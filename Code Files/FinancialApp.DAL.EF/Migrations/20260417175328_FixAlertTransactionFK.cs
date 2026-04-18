using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialApp.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class FixAlertTransactionFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Transactions_TransactionTxId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_TransactionTxId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "TransactionTxId",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TxId",
                table: "Alerts",
                column: "TxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Transactions_TxId",
                table: "Alerts",
                column: "TxId",
                principalTable: "Transactions",
                principalColumn: "TxId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Transactions_TxId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_TxId",
                table: "Alerts");

            migrationBuilder.AddColumn<int>(
                name: "TransactionTxId",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TransactionTxId",
                table: "Alerts",
                column: "TransactionTxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Transactions_TransactionTxId",
                table: "Alerts",
                column: "TransactionTxId",
                principalTable: "Transactions",
                principalColumn: "TxId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
