using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace FinancialApp.DAL.ADO.Services
{
    /// <summary>
    /// ADO.NET Analytics Service. Executes stored procedures and complex aggregation queries
    /// that would generate suboptimal SQL through EF's LINQ-to-SQL translation.
    /// ADO.NET gives precise control over the SQL, uses precompiled stored procedures,
    /// and leverages SqlDataAdapter for efficient DataGridView binding patterns.
    /// </summary>
    public class AdoAnalyticsService
    {
        private readonly string _connectionString;

        public AdoAnalyticsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");
        }

        /// <summary>
        /// Calls sp_GetAccountSummary stored procedure for portfolio-level aggregation.
        /// ADO.NET used here — stored procedures are precompiled and DBA-maintainable.
        /// EF's generated SQL for this type of cross-table aggregation creates N+1 query issues.
        /// </summary>
        public async Task<(int TotalAccounts, decimal TotalPortfolioValue)> GetAccountSummaryAsync(int userId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand("sp_GetAccountSummary", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserId", userId);

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int totalAccounts = reader.GetInt32(reader.GetOrdinal("TotalAccounts"));
                            decimal totalValue = reader.GetDecimal(reader.GetOrdinal("TotalPortfolioValue"));
                            return (totalAccounts, totalValue);
                        }
                        return (0, 0m);
                    }
                }
            }
        }

        /// <summary>
        /// Calls sp_GetFraudAlerts stored procedure for high-risk alert retrieval.
        /// ADO.NET used here — speed-critical alert path, stored procedure is precompiled.
        /// </summary>
        public async Task<List<(int AlertId, decimal Confidence)>> GetHighRiskFraudAlertsAsync()
        {
            var alerts = new List<(int AlertId, decimal Confidence)>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                using (SqlCommand command = new SqlCommand("sp_GetFraudAlerts", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int id = reader.GetInt32(reader.GetOrdinal("AlertId"));
                            decimal confidence = reader.GetDecimal(reader.GetOrdinal("Confidence"));
                            alerts.Add((id, confidence));
                        }
                    }
                }
            }
            return alerts;
        }

        /// <summary>
        /// Returns full transaction history as a DataTable for DataGridView binding.
        /// ADO.NET DataAdapter + DataTable is the standard enterprise pattern for grid binding —
        /// significantly more performant than converting EF entity lists to display models.
        /// </summary>
        public DataTable GetTransactionHistory()
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT TxId, AccountId, Type, Amount, Timestamp, IsFlag FROM Transactions ORDER BY Timestamp DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Analytics Service - Transaction History Error: {ex.Message}");
            }

            return dt;
        }

        /// <summary>
        /// Returns transaction history filtered by userId via JOIN to Accounts table.
        /// Ensures data isolation — each user only sees their own transactions.
        /// ADO.NET DataAdapter + DataTable for efficient DataGridView binding.
        /// </summary>
        public DataTable GetTransactionHistoryByUser(int userId)
        {
            DataTable dt = new DataTable();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    // Parameterized query with JOIN to Accounts for user filtering
                    string query = @"
                        SELECT t.TxId, t.AccountId, t.Type, t.Amount, t.Timestamp, t.IsFlag,
                               ISNULL(c.Name, '—') AS Category
                        FROM Transactions t
                        INNER JOIN Accounts a ON t.AccountId = a.AccountId
                        LEFT JOIN Categories c ON t.CategoryId = c.CategoryId
                        WHERE a.UserId = @UserId
                        ORDER BY t.Timestamp DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Analytics Service - User Transaction History Error: {ex.Message}");
            }

            return dt;
        }

        /// <summary>
        /// Returns monthly spending aggregation for line chart visualization.
        /// ADO.NET used here — complex GROUP BY + date aggregation generates inefficient EF SQL.
        /// This query provides precise control over the date grouping and sum logic.
        /// </summary>
        public async Task<DataTable> GetMonthlySpendingAsync()
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                // Complex aggregation query — EF LINQ would generate suboptimal SQL with multiple subqueries
                string query = @"
                    SELECT 
                        FORMAT(Timestamp, 'MMM yyyy') AS [Month],
                        MONTH(Timestamp) AS MonthNum,
                        YEAR(Timestamp) AS YearNum,
                        SUM(Amount) AS TotalSpent
                    FROM Transactions
                    WHERE Type IN ('Withdrawal', 'Transfer')
                    GROUP BY FORMAT(Timestamp, 'MMM yyyy'), MONTH(Timestamp), YEAR(Timestamp)
                    ORDER BY YearNum DESC, MonthNum DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        await conn.OpenAsync();
                        await Task.Run(() => adapter.Fill(dt));
                    }
                }
            }

            return dt;
        }

        /// <summary>
        /// Returns spending by category for pie chart visualization.
        /// ADO.NET used here — the JOIN + GROUP BY + aggregation would cause EF to generate
        /// multiple round-trips instead of a single optimized query.
        /// </summary>
        public async Task<DataTable> GetCategoryBreakdownAsync()
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        ISNULL(c.Name, 'Uncategorized') AS Category,
                        SUM(t.Amount) AS TotalAmount,
                        COUNT(t.TxId) AS TransactionCount
                    FROM Transactions t
                    LEFT JOIN Categories c ON t.CategoryId = c.CategoryId
                    GROUP BY c.Name
                    ORDER BY TotalAmount DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        await conn.OpenAsync();
                        await Task.Run(() => adapter.Fill(dt));
                    }
                }
            }

            return dt;
        }

        /// <summary>
        /// Returns fraud alerts as a DataTable for DataGridView binding in the Alerts management UI.
        /// Includes associated transaction details via JOIN for complete alert context.
        /// </summary>
        public async Task<DataTable> GetFraudAlertsDataTableAsync()
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT 
                        a.AlertId,
                        a.TxId AS TransactionId,
                        a.Confidence,
                        a.CreatedAt,
                        a.IsRead,
                        ISNULL(t.Amount, 0) AS TransactionAmount,
                        ISNULL(t.Type, 'N/A') AS TransactionType
                    FROM Alerts a
                    LEFT JOIN Transactions t ON a.TxId = t.TxId
                    ORDER BY a.CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        await conn.OpenAsync();
                        await Task.Run(() => adapter.Fill(dt));
                    }
                }
            }

            return dt;
        }
    }
}