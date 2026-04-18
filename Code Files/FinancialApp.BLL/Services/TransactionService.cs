using FinancialApp.DAL.ADO.Services;
using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading.Tasks;

namespace FinancialApp.BLL.Services
{
    /// <summary>
    /// BLL Transaction Service. Orchestrates the complete transaction lifecycle:
    /// Input validation → ML fraud check → ADO.NET atomic transfer → EF history record → Audit log.
    /// This is the core business logic layer — no form should ever execute SQL directly.
    /// </summary>
    public class TransactionService
    {
        private readonly FinancialDbContext _context;
        private readonly AdoTransferService _transferService;
        private readonly FraudDetectionService _fraudService;
        private readonly AdoAnalyticsService _analyticsService;
        private readonly AuditService _auditService;

        public TransactionService(
            FinancialDbContext context,
            AdoTransferService transferService,
            FraudDetectionService fraudService,
            AdoAnalyticsService analyticsService,
            AuditService auditService)
        {
            _context = context;
            _transferService = transferService;
            _fraudService = fraudService;
            _analyticsService = analyticsService;
            _auditService = auditService;
        }

        /// <summary>
        /// Executes a full transfer: BLL validation → ML fraud check → ADO.NET atomic debit/credit → EF audit.
        /// ADO.NET is used for the actual fund movement because EF does not expose SqlTransaction directly,
        /// and we need explicit transaction control for the atomic debit+credit operation (ACID compliance).
        /// </summary>
        public async Task<(bool Success, string Message, bool IsFraud, double Confidence)> ExecuteTransferAsync(
            int userId, decimal amount, int targetAccountId)
        {
            // --- BLL Input Validation (catch errors before they reach the database) ---
            if (amount <= 0)
                return (false, "Transfer amount must be a positive number.", false, 0);

            if (targetAccountId <= 0)
                return (false, "Please enter a valid target account ID.", false, 0);

            // Get the source account for this user
            var sourceAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (sourceAccount == null)
                return (false, "No account found for this user. Please contact an administrator.", false, 0);

            if (sourceAccount.AccountId == targetAccountId)
                return (false, "Cannot transfer to the same account.", false, 0);

            if (sourceAccount.Balance < amount)
                return (false, $"Insufficient funds. Current balance: {sourceAccount.Balance:C}", false, 0);

            // Verify the target account exists
            bool targetExists = await _context.Accounts.AnyAsync(a => a.AccountId == targetAccountId);
            if (!targetExists)
                return (false, $"Target account #{targetAccountId} does not exist.", false, 0);

            // --- ML Fraud Detection via Flask microservice ---
            // If the Flask API is down, the transaction still completes (graceful degradation)
            var fraudResult = await _fraudService.EvaluateTransactionAsync(0, amount);

            if (fraudResult.IsFraud || fraudResult.Confidence > 0.45)
            {
                // Log the fraud alert before blocking
                await _auditService.LogFraudAlertAsync(userId, (decimal)fraudResult.Confidence, amount);
                return (false,
                    $"TRANSACTION BLOCKED by AI Fraud Detection.\nConfidence of Fraud: {(fraudResult.Confidence * 100):0.00}%",
                    true, fraudResult.Confidence);
            }

            // --- Execute atomic transfer via ADO.NET ---
            // ADO.NET used here for explicit transaction control — EF does not expose SqlTransaction directly.
            // The debit + credit must execute atomically inside one SqlTransaction.
            await _transferService.TransferFundsAsync(sourceAccount.AccountId, targetAccountId, amount);

            // --- CRITICAL: Reload entity from database after ADO.NET modifies the balance ---
            // ADO.NET bypassed the EF change tracker. Without this reload, the EF context would
            // still hold the old (stale) balance, causing incorrect reads on subsequent operations.
            await _context.Entry(sourceAccount).ReloadAsync();

            // --- Record transaction in EF for history tracking ---
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Transfer");
            _context.Transactions.Add(new Transaction
            {
                AccountId = sourceAccount.AccountId,
                CategoryId = category?.CategoryId,
                Type = "Transfer",
                Amount = amount,
                Timestamp = DateTime.UtcNow,
                IsFlag = false
            });
            await _context.SaveChangesAsync();

            // --- Audit Log ---
            await _auditService.LogTransactionAsync(userId, "Transfer", amount, sourceAccount.AccountId);

            return (true, $"Transfer completed successfully. New balance: {sourceAccount.Balance:C}", false, fraudResult.Confidence);
        }

        /// <summary>
        /// Deposits funds into the user's primary account.
        /// EF Core handles the balance update — simple single-entity modification, no transaction control needed.
        /// </summary>
        public async Task<(bool Success, string Message)> ExecuteDepositAsync(int userId, decimal amount)
        {
            if (amount <= 0)
                return (false, "Deposit amount must be a positive number.");

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
                return (false, "No account found for this user.");

            account.Balance += amount;

            // Income category for deposits — semantically correct
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Income");
            _context.Transactions.Add(new Transaction
            {
                AccountId = account.AccountId,
                CategoryId = category?.CategoryId,
                Type = "Deposit",
                Amount = amount,
                Timestamp = DateTime.UtcNow,
                IsFlag = false
            });
            await _context.SaveChangesAsync();

            await _auditService.LogTransactionAsync(userId, "Deposit", amount, account.AccountId);
            return (true, $"Deposit of {amount:C} completed. New balance: {account.Balance:C}");
        }

        /// <summary>
        /// Withdraws funds from the user's primary account.
        /// BLL validates sufficient balance before executing.
        /// </summary>
        public async Task<(bool Success, string Message)> ExecuteWithdrawalAsync(int userId, decimal amount)
        {
            if (amount <= 0)
                return (false, "Withdrawal amount must be a positive number.");

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
                return (false, "No account found for this user.");

            if (account.Balance < amount)
                return (false, $"Insufficient funds. Current balance: {account.Balance:C}");

            account.Balance -= amount;

            // Use null category for withdrawals — no specific spending category at this level.
            // Users can categorize spending in the Analytics view.
            _context.Transactions.Add(new Transaction
            {
                AccountId = account.AccountId,
                CategoryId = null,
                Type = "Withdrawal",
                Amount = amount,
                Timestamp = DateTime.UtcNow,
                IsFlag = false
            });
            await _context.SaveChangesAsync();

            await _auditService.LogTransactionAsync(userId, "Withdrawal", amount, account.AccountId);
            return (true, $"Withdrawal of {amount:C} completed. New balance: {account.Balance:C}");
        }

        /// <summary>
        /// Returns transaction history for a specific user via ADO.NET DataAdapter + DataTable.
        /// ADO.NET used here — DataGridView binding is cleaner and more performant via DataAdapter
        /// than converting EF entity lists. This is a classic enterprise pattern used in banking systems.
        /// Filtered by userId to ensure data isolation between users.
        /// </summary>
        public DataTable GetTransactionHistory(int userId)
        {
            return _analyticsService.GetTransactionHistoryByUser(userId);
        }
    }
}
