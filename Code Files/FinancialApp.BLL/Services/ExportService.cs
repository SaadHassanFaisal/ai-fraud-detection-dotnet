using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApp.BLL.Services
{
    /// <summary>
    /// BLL Export Service. Generates professional Excel reports from transaction data
    /// using ClosedXML — a third-party NuGet package demonstrating package integration skills.
    /// </summary>
    public class ExportService
    {
        private readonly FinancialDbContext _context;
        private readonly AuditService _auditService;

        public ExportService(FinancialDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        /// <summary>
        /// Exports all transactions for a given user to a formatted Excel workbook.
        /// Includes styled headers, currency formatting, and conditional fraud flag highlighting.
        /// </summary>
        public async Task<string> ExportTransactionsToExcelAsync(int userId, string outputDirectory)
        {
            // Fetch transactions with navigation properties
            var transactions = await _context.Transactions
                .Include(t => t.Account)
                .Include(t => t.Category)
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .AsNoTracking()
                .ToListAsync();

            string fileName = $"TransactionReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(outputDirectory, fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Transaction Report");

                // --- Header Row Styling ---
                var headers = new[] { "Transaction ID", "Account", "Type", "Category", "Amount", "Date", "Fraud Flag" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromArgb(24, 30, 54);
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
                    cell.Style.Border.BottomBorderColor = XLColor.FromArgb(0, 126, 249);
                }

                // --- Data Rows ---
                for (int row = 0; row < transactions.Count; row++)
                {
                    var tx = transactions[row];
                    int excelRow = row + 2;

                    worksheet.Cell(excelRow, 1).Value = tx.TxId;
                    worksheet.Cell(excelRow, 2).Value = $"Account #{tx.AccountId}";
                    worksheet.Cell(excelRow, 3).Value = tx.Type;
                    worksheet.Cell(excelRow, 4).Value = tx.Category?.Name ?? "—";
                    worksheet.Cell(excelRow, 5).Value = tx.Amount;
                    worksheet.Cell(excelRow, 5).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Cell(excelRow, 6).Value = tx.Timestamp.ToString("g");
                    worksheet.Cell(excelRow, 7).Value = tx.IsFlag ? "⚠ FLAGGED" : "Clean";

                    // Highlight fraud-flagged rows in red
                    if (tx.IsFlag)
                    {
                        var range = worksheet.Range(excelRow, 1, excelRow, 7);
                        range.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 230, 230);
                        range.Style.Font.FontColor = XLColor.Red;
                    }

                    // Alternating row colors for readability
                    if (!tx.IsFlag && row % 2 == 1)
                    {
                        var range = worksheet.Range(excelRow, 1, excelRow, 7);
                        range.Style.Fill.BackgroundColor = XLColor.FromArgb(240, 242, 245);
                    }
                }

                // --- Summary Footer ---
                int footerRow = transactions.Count + 3;
                worksheet.Cell(footerRow, 1).Value = "REPORT SUMMARY";
                worksheet.Cell(footerRow, 1).Style.Font.Bold = true;
                worksheet.Cell(footerRow + 1, 1).Value = "Total Transactions:";
                worksheet.Cell(footerRow + 1, 2).Value = transactions.Count;
                worksheet.Cell(footerRow + 2, 1).Value = "Total Amount:";
                worksheet.Cell(footerRow + 2, 2).Value = transactions.Sum(t => t.Amount);
                worksheet.Cell(footerRow + 2, 2).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(footerRow + 3, 1).Value = "Flagged Transactions:";
                worksheet.Cell(footerRow + 3, 2).Value = transactions.Count(t => t.IsFlag);
                worksheet.Cell(footerRow + 4, 1).Value = "Generated At:";
                worksheet.Cell(footerRow + 4, 2).Value = DateTime.Now.ToString("G");

                // Auto-fit columns for clean presentation
                worksheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }

            // Log the export action to AuditLog
            await _auditService.LogExportAsync(userId, "Excel (.xlsx)", transactions.Count);

            return filePath;
        }
    }
}
