using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace FinancialApp.DAL.ADO.Services
{
    public class AdoTransferService
    {
        private readonly string _connectionString;

        // Dependency Injection for the connection string
        public AdoTransferService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        public async Task<bool> TransferFundsAsync(int fromAccountId, int toAccountId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Transfer amount must be strictly positive.");
            if (fromAccountId == toAccountId) throw new ArgumentException("Source and destination accounts cannot be the same.");

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // ADO.NET used here for explicit transaction control — EF does not expose SqlTransaction directly
                using (SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Debit the Source Account (Strict Parameterization prevents SQL Injection)
                        string debitQuery = "UPDATE Accounts SET Balance = Balance - @Amount WHERE AccountId = @FromId AND Balance >= @Amount";
                        using (SqlCommand debitCmd = new SqlCommand(debitQuery, connection, transaction))
                        {
                            debitCmd.Parameters.AddWithValue("@Amount", amount);
                            debitCmd.Parameters.AddWithValue("@FromId", fromAccountId);

                            int rowsAffected = await debitCmd.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                throw new InvalidOperationException("Transfer failed: Insufficient funds or invalid source account.");
                            }
                        }

                        // 2. Credit the Destination Account (Strict Parameterization)
                        string creditQuery = "UPDATE Accounts SET Balance = Balance + @Amount WHERE AccountId = @ToId";
                        using (SqlCommand creditCmd = new SqlCommand(creditQuery, connection, transaction))
                        {
                            creditCmd.Parameters.AddWithValue("@Amount", amount);
                            creditCmd.Parameters.AddWithValue("@ToId", toAccountId);

                            int rowsAffected = await creditCmd.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                throw new InvalidOperationException("Transfer failed: Invalid destination account.");
                            }
                        }

                        // 3. Commit the transaction ONLY if both queries succeed
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        // 4. Rollback the entire transaction if ANY error occurs (ACID Compliance)
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}