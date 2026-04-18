using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using FinancialApp.Models.Entities;
using FinancialApp.BLL.Services;
using FinancialApp.DAL.ADO.Services;

namespace FinancialApp.UI
{
    public partial class MainForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly User _currentUser;

        private Panel pnlSidebar = null!;
        private Panel pnlContent = null!;
        private Label lblBrand = null!;

        private readonly TransactionService _transactionService;
        private readonly AdoAnalyticsService _analyticsService;
        private readonly AlertService _alertService;
        private readonly AccountService _accountService;
        private readonly ExportService _exportService;

        public MainForm(IServiceProvider serviceProvider, User currentUser)
        {
            _serviceProvider = serviceProvider;
            _currentUser = currentUser;

            _transactionService = _serviceProvider.GetRequiredService<TransactionService>();
            _analyticsService = _serviceProvider.GetRequiredService<AdoAnalyticsService>();
            _alertService = _serviceProvider.GetRequiredService<AlertService>();
            _accountService = _serviceProvider.GetRequiredService<AccountService>();
            _exportService = _serviceProvider.GetRequiredService<ExportService>();

            this.Text = "Aegis Intelligence - Terminal";
            this.Size = new Size(1280, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1024, 768);

            BuildResponsiveUI();
            LoadView("Dashboard");
        }

        private void BuildResponsiveUI()
        {
            this.BackColor = Theme.BgPrimary;

            // ── Sidebar (Left Dock) ──
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = Theme.BgSidebar
            };

            // Sidebar Line Border
            var sidebarBorder = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border };
            pnlSidebar.Controls.Add(sidebarBorder);

            // Brand Header
            lblBrand = new Label
            {
                Text = "AI Financial System",
                Font = new Font("Century Gothic", 14F, FontStyle.Bold | FontStyle.Italic),
                ForeColor = Theme.TextWhite,
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSidebar.Controls.Add(lblBrand);

            // User Info
            var lblUser = new Label
            {
                Text = $"ID: {_currentUser.Username.ToUpper()}\nROLE: {_currentUser.Role.ToUpper()}",
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Bottom,
                AutoSize = true,
                Padding = new Padding(0, 15, 0, 15),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlSidebar.Controls.Add(lblUser);

            // Menu Buttons
            var btnAlerts = CreateMenuButton("Fraud Alerts", "Alerts");
            var btnAnalytics = CreateMenuButton("Visual Analytics", "Analytics");
            var btnTransactions = CreateMenuButton("Transfers & Logs", "Transactions");
            var btnDashboard = CreateMenuButton("Executive Dashboard", "Dashboard");

            pnlSidebar.Controls.AddRange(new Control[] { btnAlerts, btnAnalytics, btnTransactions, btnDashboard });

            // ── Content Panel (Fill Dock) ──
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgPrimary,
                Padding = new Padding(20)
            };

            this.Controls.AddRange(new Control[] { pnlContent, pnlSidebar });
        }

        private Button CreateMenuButton(string text, string viewName)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 60,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.BodyBold,
                ForeColor = Theme.TextSilver,
                BackColor = Theme.BgSidebar,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Theme.BgHover;
            btn.FlatAppearance.MouseDownBackColor = Theme.BgCard;
            btn.Click += (s, e) => { LoadView(viewName); HighlightButton(btn); };
            return btn;
        }

        private void HighlightButton(Button activeBtn)
        {
            foreach (Control ctrl in pnlSidebar.Controls)
            {
                if (ctrl is Button btn)
                {
                    btn.ForeColor = Theme.TextSilver;
                    btn.BackColor = Theme.BgSidebar;
                }
            }
            activeBtn.ForeColor = Theme.TextWhite;
            activeBtn.BackColor = Theme.BgHover;
        }

        private void LoadView(string viewName)
        {
            UserControl? view = viewName switch
            {
                "Dashboard" => new ucDashboard(_accountService, _analyticsService, _currentUser),
                "Transactions" => new ucTransactions(_transactionService, _accountService, _exportService, _currentUser),
                "Analytics" => new ucAnalytics(_analyticsService),
                "Alerts" => new ucAlerts(_alertService),
                _ => null
            };

            if (view != null)
            {
                pnlContent.Controls.Clear();
                view.Dock = DockStyle.Fill;
                pnlContent.Controls.Add(view);
            }
        }
    }
}