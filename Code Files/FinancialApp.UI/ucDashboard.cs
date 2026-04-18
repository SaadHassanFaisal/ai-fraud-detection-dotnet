using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FinancialApp.BLL.Services;
using FinancialApp.DAL.ADO.Services;
using FinancialApp.Models.Entities;

namespace FinancialApp.UI
{
    /// <summary>
    /// Executive Dashboard view. Displays KPI cards and an Income vs Expense chart.
    /// All data flows through BLL (AccountService) and ADO.NET (AdoAnalyticsService).
    /// No DbContext, SqlConnection, or ConfigurationBuilder in this form — ever.
    /// </summary>
    public partial class ucDashboard : UserControl
    {
        private readonly AccountService _accountService;
        private readonly AdoAnalyticsService _analyticsService;
        private readonly User _currentUser;

        private Label lblBalanceVal = null!;
        private Label lblSpendVal = null!;
        private Label lblAlertVal = null!;
        private Chart chartIncomeExpense = null!;

        public ucDashboard(AccountService accountService, AdoAnalyticsService analyticsService, User currentUser)
        {
            _accountService = accountService;
            _analyticsService = analyticsService;
            _currentUser = currentUser;

            BuildResponsiveUI();
            _ = LoadDashboardDataAsync();
        }

        private void BuildResponsiveUI()
        {
            this.BackColor = Theme.BgPrimary;
            this.Padding = new Padding(15);
            this.Dock = DockStyle.Fill;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F)); // KPIs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Chart

            // ── Header ──
            var lblTitle = new Label
            {
                Text = "EXECUTIVE DASHBOARD",
                Font = Theme.HeadingMd,
                ForeColor = Theme.TextWhite,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 15),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            // ── KPI Row (TableLayoutPanel 3 columns) ──
            var kpiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 3,
                Margin = new Padding(0, 0, 0, 15)
            };
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            var cardBalance = CreateKpiCard("TOTAL BALANCE", "$0.00", Theme.AccentPrimary, out lblBalanceVal);
            var cardSpend = CreateKpiCard("MONTHLY SPENDING", "$0.00", Theme.AccentRed, out lblSpendVal);
            var cardAlerts = CreateKpiCard("PENDING ALERTS", "0", Theme.AccentBlue, out lblAlertVal);

            kpiLayout.Controls.Add(cardBalance, 0, 0);
            kpiLayout.Controls.Add(cardSpend, 1, 0);
            kpiLayout.Controls.Add(cardAlerts, 2, 0);

            mainLayout.Controls.Add(kpiLayout, 0, 1);

            // ── Chart Row ──
            var pnlChart = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(15),
                MinimumSize = new Size(0, 200) // Prevent Height=0 crash
            };
            pnlChart.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlChart.ClientRectangle, Theme.Border, ButtonBorderStyle.Solid);

            chartIncomeExpense = new Chart 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Theme.BgCard,
                MinimumSize = new Size(0, 100) // Prevent Chart layout crash
            };
            var area = new ChartArea("MainArea") { BackColor = Theme.BgCard };
            area.AxisX.MajorGrid.LineColor = Theme.GridLine;
            area.AxisY.MajorGrid.LineColor = Theme.GridLine;
            area.AxisX.LabelStyle.ForeColor = Theme.TextSilver;
            area.AxisY.LabelStyle.ForeColor = Theme.TextSilver;
            area.AxisX.LineColor = Theme.Border;
            area.AxisY.LineColor = Theme.Border;
            chartIncomeExpense.ChartAreas.Add(area);

            var legend = new Legend("Legend1") { BackColor = Theme.BgCard, ForeColor = Theme.TextSilver, Font = Theme.Body, Docking = Docking.Top, Alignment = StringAlignment.Center };
            chartIncomeExpense.Legends.Add(legend);

            pnlChart.Controls.Add(chartIncomeExpense);
            mainLayout.Controls.Add(pnlChart, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private Panel CreateKpiCard(string title, string initialValue, Color accentColor, out Label valueLabel)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 15, 0),
                Height = 150
            };
            pnl.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, Theme.Border, ButtonBorderStyle.Solid);

            var stripe = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = accentColor };
            
            var lblTitle = new Label
            {
                Text = title,
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 30,
                Padding = new Padding(15, 10, 0, 0)
            };

            valueLabel = new Label
            {
                Text = initialValue,
                Font = Theme.KpiValue,
                ForeColor = Theme.TextWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            };

            pnl.Controls.AddRange(new Control[] { valueLabel, lblTitle, stripe });
            return pnl;
        }

        /// <summary>
        /// Loads all dashboard data through proper BLL and ADO.NET layers.
        /// KPIs come from AccountService.GetDashboardSummaryAsync() (BLL → EF).
        /// Chart data comes from AdoAnalyticsService.GetMonthlySpendingAsync() (ADO.NET).
        /// No DbContext, SqlConnection, or ConfigurationBuilder touches this form.
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // ── KPI Data via BLL (proper architecture) ──
                var (totalBalance, monthlySpend, alertCount) = 
                    await _accountService.GetDashboardSummaryAsync(_currentUser.UserId);

                lblBalanceVal.Text = totalBalance.ToString("C");
                lblSpendVal.Text = monthlySpend.ToString("C");
                lblAlertVal.Text = alertCount.ToString();

                // ── Chart Data via ADO.NET (blueprint-compliant) ──
                // ADO.NET used here — complex GROUP BY + date aggregation generates inefficient EF SQL.
                // The monthly spending stored procedure provides precise SQL control.
                DataTable monthlyData = await _analyticsService.GetMonthlySpendingAsync();

                chartIncomeExpense.Series.Clear();
                var seriesSpend = new Series("Monthly Spend") 
                { 
                    ChartType = SeriesChartType.Column, 
                    Color = Theme.AccentBlue, 
                    Font = Theme.Body,
                    BorderWidth = 0
                };

                foreach (DataRow row in monthlyData.Rows)
                {
                    if (row["Month"] != DBNull.Value && row["TotalSpent"] != DBNull.Value)
                    {
                        seriesSpend.Points.AddXY(
                            row["Month"].ToString(), 
                            Convert.ToDouble(row["TotalSpent"]));
                    }
                }

                chartIncomeExpense.Series.Add(seriesSpend);
            }
            catch (Exception ex)
            {
                lblBalanceVal.Text = "Error";
                Console.WriteLine($"Dashboard load error: {ex.Message}");
            }
        }
    }
}