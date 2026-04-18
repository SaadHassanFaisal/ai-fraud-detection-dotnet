using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialApp.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create sp_GetAccountSummary for complex aggregation
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetAccountSummary
                    @UserId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    -- ADO.NET for analytics queries: EF generated SQL for complex aggregations creates N+1 issues
                    SELECT 
                        u.UserId,
                        u.Username,
                        COUNT(a.AccountId) AS TotalAccounts,
                        ISNULL(SUM(a.Balance), 0) AS TotalPortfolioValue
                    FROM Users u
                    LEFT JOIN Accounts a ON u.UserId = a.UserId
                    WHERE u.UserId = @UserId
                    GROUP BY u.UserId, u.Username;
                END
            ");

            // 2. Create sp_GetFraudAlerts
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetFraudAlerts
                AS
                BEGIN
                    SET NOCOUNT ON;
                    -- High-performance direct read for fraud detection
                    SELECT 
                        AlertId,
                        Confidence
                    FROM Alerts
                    WHERE Confidence >= 0.75
                    ORDER BY Confidence DESC;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cleanly drop the procedures if we ever rollback this migration
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAccountSummary;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetFraudAlerts;");
        }
    }
}