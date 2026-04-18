using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinancialApp.BLL.Services;
using FinancialApp.DAL.ADO.Services;
using FinancialApp.DAL.EF.Context;
using FinancialApp.DAL.EF.Repositories;
using FinancialApp.Models.Interfaces;

namespace FinancialApp.Tests
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=========================================");
            Console.WriteLine(" FINANALYTICS - INTEGRATION TEST RUNNER ");
            Console.WriteLine("=========================================");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(@"d:\AntiGravity Projects\Intelligent Financial Analytics & Fraud Detection Desktop System\Code Files\FinancialApp.UI\bin\Debug\net9.0-windows")
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var services = new ServiceCollection();

            // 1. Setup EF
            services.AddDbContext<FinancialDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")),
                ServiceLifetime.Transient);

            services.AddTransient(typeof(IRepository<>), typeof(GenericRepository<>));

            services.AddSingleton<IConfiguration>(configuration);
            services.AddHttpClient();

            // 2. Setup ADO.NET Services
            services.AddTransient<AdoTransferService>();
            services.AddTransient<AdoAnalyticsService>();
            services.AddTransient<AdoTransactionHistoryService>();
            services.AddTransient<FraudDetectionService>();

            // 3. Setup BLL Services
            services.AddTransient<AuditService>();
            services.AddTransient<AuthenticationService>();
            services.AddTransient<AccountService>();
            services.AddTransient<TransactionService>();
            services.AddTransient<AlertService>();

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                Console.WriteLine("[TEST 1] Authenticating Admin User...");
                var authService = serviceProvider.GetRequiredService<AuthenticationService>();
                await authService.EnsureAdminExistsAsync();
                
                var user = await authService.AuthenticateAsync("admin", "admin123");
                if (user == null) throw new Exception("Authentication failed!");
                Console.WriteLine("✓ Authentication Successful");

                Console.WriteLine("\n[TEST 2] Verifying Accounts & Initial Balances...");
                var accountService = serviceProvider.GetRequiredService<AccountService>();
                await accountService.EnsureAccountExistsAsync(user.UserId);
                
                var accounts = await accountService.GetUserAccountsAsync(user.UserId);
                var checking = accounts.First(a => a.AccountType == "Checking");
                var savings = accounts.First(a => a.AccountType == "Savings");
                Console.WriteLine($"✓ Checking Balance: {checking.Balance:C}");
                Console.WriteLine($"✓ Savings Balance: {savings.Balance:C}");

                Console.WriteLine("\n[TEST 3] Executing Atomic ADO.NET Transfer...");
                var transactionService = serviceProvider.GetRequiredService<TransactionService>();
                decimal transferAmount = 1000m;
                var result = await transactionService.ExecuteTransferAsync(user.UserId, transferAmount, savings.AccountId);
                
                if (!result.Success) throw new Exception($"Transfer failed: {result.Message}");
                Console.WriteLine($"✓ Transfer Output: {result.Message}");

                Console.WriteLine("\n[TEST 4] Verifying Post-Transfer Entity Balances (Cache Reload Check)...");
                accounts = await accountService.GetUserAccountsAsync(user.UserId);
                var checkingAfter = accounts.First(a => a.AccountType == "Checking");
                var savingsAfter = accounts.First(a => a.AccountType == "Savings");
                
                if (checkingAfter.Balance != checking.Balance - transferAmount)
                    throw new Exception($"Checking balance did not update correctly! Expected {checking.Balance - transferAmount}, got {checkingAfter.Balance}");
                    
                if (savingsAfter.Balance != savings.Balance + transferAmount)
                    throw new Exception($"Savings balance did not update correctly! Expected {savings.Balance + transferAmount}, got {savingsAfter.Balance}");
                    
                Console.WriteLine($"✓ Checking Updated to: {checkingAfter.Balance:C}");
                Console.WriteLine($"✓ Savings Updated to: {savingsAfter.Balance:C}");

                Console.WriteLine("\n[TEST 5] Verifying ADO.NET Transaction History...");
                var historyTable = transactionService.GetTransactionHistory(user.UserId);
                if (historyTable.Rows.Count == 0) throw new Exception("No transaction history found!");
                
                var latestTx = historyTable.Rows[0];
                if (Convert.ToDecimal(latestTx["Amount"]) != transferAmount || latestTx["Type"].ToString() != "Transfer")
                {
                    throw new Exception("Latest transaction does not match the executed transfer!");
                }
                Console.WriteLine("✓ Transaction correctly logged in ADO DataTable history");

                Console.WriteLine("\n=========================================");
                Console.WriteLine(" ALL INTEGRATION TESTS PASSED SUCCESSFULLY ");
                Console.WriteLine("=========================================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Test Failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[INNER ERROR] {ex.InnerException.Message}");
                }
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
