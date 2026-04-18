using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Interfaces;
using FinancialApp.DAL.EF.Repositories;
using FinancialApp.DAL.ADO.Services;
using FinancialApp.BLL.Services;

namespace FinancialApp.UI
{
    internal static class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // --- Global Error Handling ---
            // Never show raw exception messages to users. Friendly dialogs for the user,
            // full stack trace logged by Serilog to file.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                Log.Error(e.Exception, "Unhandled UI thread exception");
                MessageBox.Show(
                    "An unexpected error occurred. The error has been logged.\nPlease restart the application.",
                    "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Fatal unhandled domain exception");
                }
            };

            // 1. Build Configuration from appsettings.json — never hardcode credentials
            // BaseDirectory ensures this works both from 'dotnet run' and from Visual Studio
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            // 2. Configure Serilog for file-based logging from day one
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("=== Financial Analytics System Starting ===");

                // 3. Build the Host for Dependency Injection
                var host = Host.CreateDefaultBuilder()
                    .UseSerilog()
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services, configuration);
                    })
                    .Build();

                ServiceProvider = host.Services;

                // 4. Ensure database schema is created and up to date
                using (var dbContext = ServiceProvider.GetRequiredService<FinancialDbContext>())
                {
                    dbContext.Database.Migrate();
                }

                // 5. Ensure admin user exists (database seeding)
                var authService = ServiceProvider.GetRequiredService<AuthenticationService>();
                authService.EnsureAdminExistsAsync().GetAwaiter().GetResult();

                // 6. Run the Application — LoginForm is the entry point
                var loginForm = ServiceProvider.GetRequiredService<LoginForm>();
                Application.Run(loginForm);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly due to a fatal error.");
                MessageBox.Show(
                    "A fatal error prevented the application from starting.\nPlease check the log files.",
                    "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.Information("=== Financial Analytics System Shutting Down ===");
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // --- Data Access Layer: Entity Framework ---
            // IMPORTANT: Transient lifetime for WinForms. Unlike ASP.NET where Scoped = per-request,
            // WinForms has no request scope. Scoped would create ONE DbContext for the entire app lifetime,
            // causing stale entity cache, memory leaks, and SaveChanges() conflicts.
            services.AddDbContext<FinancialDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")),
                ServiceLifetime.Transient);

            // Generic Repository pattern — BLL depends on IRepository<T>, not concrete EF classes
            services.AddTransient(typeof(IRepository<>), typeof(GenericRepository<>));

            // --- Data Access Layer: ADO.NET Services ---
            // ADO.NET used for: transfers (SqlTransaction), analytics (stored procs), history (DataAdapter)
            services.AddTransient<AdoTransferService>();
            services.AddTransient<AdoAnalyticsService>();
            services.AddTransient<AdoTransactionHistoryService>();

            // --- ML Fraud Detection: HttpClient + Flask bridge ---
            services.AddHttpClient<FraudDetectionService>();

            // --- Business Logic Layer ---
            // All BLL services — forms call ONLY these, never DAL directly
            services.AddTransient<AuditService>();
            services.AddTransient<AuthenticationService>();
            services.AddTransient<AccountService>();
            services.AddTransient<TransactionService>();
            services.AddTransient<AlertService>();
            services.AddTransient<ExportService>();

            // --- UI Forms ---
            // LoginForm is the DI entry point. MainForm is created manually in LoginForm
            // because it requires a runtime User parameter that can't be injected.
            services.AddTransient<LoginForm>();
        }
    }
}