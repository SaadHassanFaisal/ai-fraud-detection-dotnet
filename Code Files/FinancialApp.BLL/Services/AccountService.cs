using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApp.BLL.Services
{
    /// <summary>
    /// BLL Account Service. Provides dashboard KPI data and account management.
    /// Seeds demo data on first login so charts and grids are populated for demo purposes.
    /// </summary>
    public class AccountService
    {
        private readonly FinancialDbContext _context;

        public AccountService(FinancialDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Aggregates KPI data for the dashboard.
        /// </summary>
        public async Task<(decimal TotalBalance, decimal MonthlySpend, int AlertCount)> GetDashboardSummaryAsync(int userId)
        {
            decimal totalBalance = await _context.Accounts
                .Where(a => a.UserId == userId)
                .SumAsync(a => (decimal?)a.Balance) ?? 0m;

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            decimal monthlySpend = await _context.Transactions
                .Where(t => t.Account.UserId == userId
                    && t.Timestamp >= startOfMonth
                    && (t.Type == "Withdrawal" || t.Type == "Transfer"))
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            int alertCount = await _context.Alerts
                .Where(a => !a.IsRead)
                .CountAsync();

            return (totalBalance, monthlySpend, alertCount);
        }

        /// <summary>
        /// Returns all accounts owned by a specific user.
        /// </summary>
        public async Task<List<Account>> GetUserAccountsAsync(int userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Ensures accounts + demo data exist for a user on first login.
        /// Creates TWO accounts (Checking + Savings) so transfers work,
        /// and seeds ~20 transactions spanning 6 months so charts are populated.
        /// </summary>
        public async Task EnsureAccountExistsAsync(int userId)
        {
            bool hasAccount = await _context.Accounts.AnyAsync(a => a.UserId == userId);
            if (!hasAccount)
            {
                // ── Create TWO accounts so transfer feature works ──
                var checking = new Account
                {
                    UserId = userId,
                    AccountType = "Checking",
                    Balance = 12450.00m
                };

                var savings = new Account
                {
                    UserId = userId,
                    AccountType = "Savings",
                    Balance = 34200.00m
                };

                _context.Accounts.Add(checking);
                _context.Accounts.Add(savings);
                await _context.SaveChangesAsync();

                // ── Seed demo transactions so charts are populated ──
                await SeedDemoTransactionsAsync(checking.AccountId, savings.AccountId);
            }

            // Always ensure demo alerts exist for portfolio presentation, even if accounts were seeded previously
            bool hasAlerts = await _context.Alerts.AnyAsync();
            if (!hasAlerts)
            {
                await SeedDemoAlertsAsync();
            }
        }

        private async Task SeedDemoTransactionsAsync(int checkingId, int savingsId)
        {
            // Fetch category IDs
            var categories = await _context.Categories.ToListAsync();
            int? incomeId = categories.FirstOrDefault(c => c.Name == "Income")?.CategoryId;
            int? foodId = categories.FirstOrDefault(c => c.Name == "Food")?.CategoryId;
            int? transferId = categories.FirstOrDefault(c => c.Name == "Transfer")?.CategoryId;
            int? utilitiesId = categories.FirstOrDefault(c => c.Name == "Utilities")?.CategoryId;
            int? entertainmentId = categories.FirstOrDefault(c => c.Name == "Entertainment")?.CategoryId;

            var now = DateTime.UtcNow;
            var transactions = new List<Transaction>
            {
                // ── Month 1 (6 months ago) ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5200.00m, Timestamp = now.AddMonths(-6).AddDays(1), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 340.50m, Timestamp = now.AddMonths(-6).AddDays(5), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = utilitiesId, Type = "Withdrawal", Amount = 185.00m, Timestamp = now.AddMonths(-6).AddDays(12), IsFlag = false },

                // ── Month 2 (5 months ago) ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5200.00m, Timestamp = now.AddMonths(-5).AddDays(1), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 425.75m, Timestamp = now.AddMonths(-5).AddDays(7), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = entertainmentId, Type = "Withdrawal", Amount = 89.99m, Timestamp = now.AddMonths(-5).AddDays(14), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = transferId, Type = "Transfer", Amount = 2000.00m, Timestamp = now.AddMonths(-5).AddDays(20), IsFlag = false },

                // ── Month 3 (4 months ago) ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5200.00m, Timestamp = now.AddMonths(-4).AddDays(1), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = utilitiesId, Type = "Withdrawal", Amount = 210.00m, Timestamp = now.AddMonths(-4).AddDays(8), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 512.30m, Timestamp = now.AddMonths(-4).AddDays(15), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = entertainmentId, Type = "Withdrawal", Amount = 149.99m, Timestamp = now.AddMonths(-4).AddDays(22), IsFlag = false },

                // ── Month 4 (3 months ago) ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5500.00m, Timestamp = now.AddMonths(-3).AddDays(1), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 387.90m, Timestamp = now.AddMonths(-3).AddDays(6), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = utilitiesId, Type = "Withdrawal", Amount = 195.00m, Timestamp = now.AddMonths(-3).AddDays(10), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = transferId, Type = "Transfer", Amount = 1500.00m, Timestamp = now.AddMonths(-3).AddDays(18), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = entertainmentId, Type = "Withdrawal", Amount = 64.99m, Timestamp = now.AddMonths(-3).AddDays(25), IsFlag = false },

                // ── Month 5 (2 months ago) ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5500.00m, Timestamp = now.AddMonths(-2).AddDays(1), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 298.45m, Timestamp = now.AddMonths(-2).AddDays(4), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = utilitiesId, Type = "Withdrawal", Amount = 220.00m, Timestamp = now.AddMonths(-2).AddDays(11), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = entertainmentId, Type = "Withdrawal", Amount = 199.99m, Timestamp = now.AddMonths(-2).AddDays(19), IsFlag = true },

                // ── Month 6 (1 month ago) ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5500.00m, Timestamp = now.AddMonths(-1).AddDays(1), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 445.60m, Timestamp = now.AddMonths(-1).AddDays(9), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = utilitiesId, Type = "Withdrawal", Amount = 205.00m, Timestamp = now.AddMonths(-1).AddDays(14), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = transferId, Type = "Transfer", Amount = 3000.00m, Timestamp = now.AddMonths(-1).AddDays(21), IsFlag = false },

                // ── Current month ──
                new Transaction { AccountId = checkingId, CategoryId = incomeId, Type = "Deposit", Amount = 5200.00m, Timestamp = now.AddDays(-10), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = foodId, Type = "Withdrawal", Amount = 178.25m, Timestamp = now.AddDays(-5), IsFlag = false },
                new Transaction { AccountId = checkingId, CategoryId = utilitiesId, Type = "Withdrawal", Amount = 190.00m, Timestamp = now.AddDays(-2), IsFlag = false },
            };

            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();
        }

        private async Task SeedDemoAlertsAsync()
        {
            // Get some transaction IDs to link alerts to
            var recentTxIds = await _context.Transactions
                .OrderByDescending(t => t.Timestamp)
                .Take(5)
                .Select(t => t.TxId)
                .ToListAsync();

            if (recentTxIds.Count >= 3)
            {
                var alerts = new List<Alert>
                {
                    new Alert { TxId = recentTxIds[0], Confidence = 0.9250m, CreatedAt = DateTime.UtcNow.AddDays(-3), IsRead = false },
                    new Alert { TxId = recentTxIds[1], Confidence = 0.8100m, CreatedAt = DateTime.UtcNow.AddDays(-7), IsRead = false },
                    new Alert { TxId = recentTxIds[2], Confidence = 0.7650m, CreatedAt = DateTime.UtcNow.AddDays(-14), IsRead = true },
                };

                _context.Alerts.AddRange(alerts);
                await _context.SaveChangesAsync();
            }
        }
    }
}
