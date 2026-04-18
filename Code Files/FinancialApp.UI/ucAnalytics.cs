using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FinancialApp.BLL.Services;
using FinancialApp.DAL.ADO.Services;

namespace FinancialApp.UI
{
    public partial class ucAnalytics : UserControl
    {
        private readonly AdoAnalyticsService _analyticsService;

        private Chart chartCategorySpend = null!;
        private Chart chartRollingAverages = null!;
        private Label lblStatus = null!;

        public ucAnalytics(AdoAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
            BuildResponsiveUI();
            _ = LoadAnalyticsDataAsync();
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Charts
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Status

            // ── Header ──
            var lblTitle = new Label
            {
                Text = "VISUAL ANALYTICS",
                Font = Theme.HeadingMd,
                ForeColor = Theme.TextWhite,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0,0,0,10),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            // ── Charts Layout (50/50 Horizontal Split) ──
            var chartLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2
            };
            chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            chartLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Chart 1 Panel (Doughnut)
            var pnlChart1 = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(0, 0, 7, 0), Padding = new Padding(10) };
            pnlChart1.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlChart1.ClientRectangle, Theme.Border, ButtonBorderStyle.Solid);
            
            var lblChart1Title = new Label { Text = "EXPENSE BY CATEGORY", Font = Theme.BodyBold, ForeColor = Theme.TextWhite, Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft };
            chartCategorySpend = CreateChart();
            pnlChart1.Controls.AddRange(new Control[] { chartCategorySpend, lblChart1Title });
            chartLayout.Controls.Add(pnlChart1, 0, 0);

            // Chart 2 Panel (Spline)
            var pnlChart2 = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(7, 0, 0, 0), Padding = new Padding(10) };
            pnlChart2.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlChart2.ClientRectangle, Theme.Border, ButtonBorderStyle.Solid);

            var lblChart2Title = new Label { Text = "30-DAY ROLLING SPEND", Font = Theme.BodyBold, ForeColor = Theme.TextWhite, Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft };
            chartRollingAverages = CreateChart();
            pnlChart2.Controls.AddRange(new Control[] { chartRollingAverages, lblChart2Title });
            chartLayout.Controls.Add(pnlChart2, 1, 0);

            mainLayout.Controls.Add(chartLayout, 0, 1);

            // ── Status ──
            lblStatus = new Label
            {
                Text = "Loading analytics streams...",
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(0,10,0,0),
                TextAlign = ContentAlignment.MiddleRight
            };
            mainLayout.Controls.Add(lblStatus, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private Chart CreateChart()
        {
            var chart = new Chart 
            { 
                Dock = DockStyle.Fill, 
                BackColor = Theme.BgCard,
                MinimumSize = new Size(0, 100) // Prevent Height=0 layout crash
            };
            var area = new ChartArea("MainArea") { BackColor = Theme.BgCard };
            area.AxisX.MajorGrid.LineColor = Theme.GridLine;
            area.AxisY.MajorGrid.LineColor = Theme.GridLine;
            area.AxisX.LabelStyle.ForeColor = Theme.TextSilver;
            area.AxisY.LabelStyle.ForeColor = Theme.TextSilver;
            area.AxisX.LineColor = Theme.Border;
            area.AxisY.LineColor = Theme.Border;
            chart.ChartAreas.Add(area);

            var legend = new Legend("Legend1") { BackColor = Theme.BgCard, ForeColor = Theme.TextSilver, Font = Theme.Body, Docking = Docking.Bottom, Alignment = StringAlignment.Center };
            chart.Legends.Add(legend);

            return chart;
        }

        private async Task LoadAnalyticsDataAsync()
        {
            try
            {
                // 1. Load Category Spend
                DataTable catData = await _analyticsService.GetCategoryBreakdownAsync();
                var sPie = new Series("CategorySpend") { ChartType = SeriesChartType.Doughnut };
                sPie.Font = Theme.BodyBold;
                sPie["PieLabelStyle"] = "Outside";
                
                int colorIndex = 0;
                foreach (DataRow row in catData.Rows)
                {
                    double val = Convert.ToDouble(row["TotalAmount"]);
                    int pIdx = sPie.Points.AddXY(row["Category"].ToString(), val);
                    sPie.Points[pIdx].Color = Theme.ChartColors[colorIndex % Theme.ChartColors.Length];
                    colorIndex++;
                }
                chartCategorySpend.Series.Add(sPie);

                // 2. Load Rolling Spend
                DataTable rollData = await _analyticsService.GetMonthlySpendingAsync();
                var sLine = new Series("Monthly Spend") 
                { 
                    ChartType = SeriesChartType.SplineArea, 
                    BorderWidth = 3, 
                    Color = Theme.AccentBlue 
                };
                sLine.BackGradientStyle = GradientStyle.TopBottom;
                sLine.BackSecondaryColor = Theme.BgCard;

                foreach (DataRow row in rollData.Rows)
                {
                    if (row["Month"] != DBNull.Value && row["TotalSpent"] != DBNull.Value)
                    {
                        sLine.Points.AddXY(row["Month"].ToString(), Convert.ToDouble(row["TotalSpent"]));
                    }
                }
                chartRollingAverages.Series.Add(sLine);

                lblStatus.Text = "Analytics synchronized seamlessly.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Failed to synchronize analytics.";
                Console.WriteLine($"Analytics err: {ex.Message}");
            }
        }
    }
}
