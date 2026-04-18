using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Entities;
using System;
using System.Threading.Tasks;

namespace FinancialApp.BLL.Services
{
    /// <summary>
    /// Centralized audit logging service. Every transaction, login attempt, fraud alert,
    /// and data export is recorded here. Non-negotiable in a financial system.
    /// Uses EF Core for simple single-row inserts — no performance concern for logging.
    /// </summary>
    public class AuditService
    {
        private readonly FinancialDbContext _context;

        public AuditService(FinancialDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int? userId, string action, string details)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogLoginAttemptAsync(string username, bool success, int? userId = null)
        {
            string result = success ? "Authenticated successfully" : "Invalid credentials";
            await LogAsync(userId, success ? "LOGIN_SUCCESS" : "LOGIN_FAILURE",
                $"Username: {username} | Result: {result} | Time: {DateTime.UtcNow:G}");
        }

        public async Task LogTransactionAsync(int userId, string transactionType, decimal amount, int accountId)
        {
            await LogAsync(userId, $"TRANSACTION_{transactionType.ToUpper()}",
                $"Amount: {amount:C} | AccountId: {accountId} | Time: {DateTime.UtcNow:G}");
        }

        public async Task LogFraudAlertAsync(int? userId, decimal confidence, decimal amount)
        {
            await LogAsync(userId, "FRAUD_ALERT_TRIGGERED",
                $"AI Confidence: {confidence:P2} | Transaction Amount: {amount:C} | Time: {DateTime.UtcNow:G}");
        }

        public async Task LogExportAsync(int userId, string exportType, int recordCount)
        {
            await LogAsync(userId, "DATA_EXPORT",
                $"Format: {exportType} | Records Exported: {recordCount} | Time: {DateTime.UtcNow:G}");
        }
    }
}
