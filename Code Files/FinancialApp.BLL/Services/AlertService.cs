using FinancialApp.DAL.ADO.Services;
using FinancialApp.DAL.EF.Context;
using FinancialApp.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApp.BLL.Services
{
    /// <summary>
    /// BLL Alert Service. Manages the complete fraud alert lifecycle:
    /// retrieval, mark-as-read, and dashboard alert count.
    /// Uses ADO.NET stored procedures for high-performance bulk reads,
    /// and EF Core for individual alert updates (simple single-entity modification).
    /// </summary>
    public class AlertService
    {
        private readonly FinancialDbContext _context;
        private readonly AdoAnalyticsService _analyticsService;

        public AlertService(FinancialDbContext context, AdoAnalyticsService analyticsService)
        {
            _context = context;
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Returns all fraud alerts with associated transaction details.
        /// EF Core is appropriate here for navigating the Alert → Transaction relationship
        /// using Include() for eager loading.
        /// </summary>
        public async Task<List<Alert>> GetAllAlertsAsync()
        {
            return await _context.Alerts
                .Include(a => a.Transaction)
                .OrderByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Returns only unread alerts for notification badges.
        /// </summary>
        public async Task<int> GetUnreadAlertCountAsync()
        {
            return await _context.Alerts
                .Where(a => !a.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Marks a specific alert as read. Simple single-entity update — EF Core handles this cleanly.
        /// </summary>
        public async Task<bool> MarkAsReadAsync(int alertId)
        {
            var alert = await _context.Alerts.FindAsync(alertId);
            if (alert == null) return false;

            alert.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Marks all unread alerts as read (batch operation).
        /// </summary>
        public async Task MarkAllAsReadAsync()
        {
            var unreadAlerts = await _context.Alerts
                .Where(a => !a.IsRead)
                .ToListAsync();

            foreach (var alert in unreadAlerts)
            {
                alert.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns alerts as a DataTable for DataGridView binding.
        /// Uses ADO.NET for this pattern — DataAdapter + DataTable is the standard
        /// enterprise approach for binding grids in financial applications.
        /// </summary>
        public async Task<DataTable> GetAlertsDataTableAsync()
        {
            return await _analyticsService.GetFraudAlertsDataTableAsync();
        }
    }
}
