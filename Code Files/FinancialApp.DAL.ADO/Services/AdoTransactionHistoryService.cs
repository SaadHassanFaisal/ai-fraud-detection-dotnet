using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Threading.Tasks;

namespace FinancialApp.DAL.ADO.Services
{
    /// <summary>
    /// ADO.NET Transaction History Service. Uses SqlDataAdapter + DataTable for efficient
    /// DataGridView binding — a classic enterprise pattern used in banking and finance systems.
    /// ADO.NET is used here because DataGridView binding via DataAdapter is cleaner and more
    /// performant than converting EF entity lists to DataTables.
    /// </summary>
    public class AdoTransactionHistoryService
    {
        private readonly string _connectionString;

        public AdoTransactionHistoryService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        /// <summary>
        /// Returns transaction history for a specific account as a DataTable.
        /// Uses SqlDataAdapter for direct DataGridView binding — the standard enterprise approach.
        /// </summary>
        public async Task<DataTable> GetTransactionHistoryAsync(int accountId)
        {
            DataTable historyTable = new DataTable();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // FIXED: Column name corrected from TransactionId to TxId to match EF entity schema
                // Strict Parameterization: Zero string concatenation to prevent SQL Injection
                string query = @"
                    SELECT 
                        t.TxId,
                        t.Amount,
                        t.Timestamp,
                        t.Type,
                        c.Name AS Category
                    FROM Transactions t
                    LEFT JOIN Categories c ON t.CategoryId = c.CategoryId
                    WHERE t.AccountId = @AccountId
                    ORDER BY t.Timestamp DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AccountId", accountId);

                    // ADO.NET DataAdapter used here — binding a DataGridView directly to a DataTable 
                    // is a highly optimized enterprise pattern that avoids the memory overhead of EF Core lists.
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        await connection.OpenAsync();
                        await Task.Run(() => adapter.Fill(historyTable));
                    }
                }
            }

            return historyTable;
        }
    }
}