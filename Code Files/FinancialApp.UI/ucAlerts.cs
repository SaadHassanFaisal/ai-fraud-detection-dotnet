using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using FinancialApp.BLL.Services;

namespace FinancialApp.UI
{
    public partial class ucAlerts : UserControl
    {
        private readonly AlertService _alertService;

        private DataGridView dgvAlerts = null!;
        private Button btnMarkRead = null!, btnMarkAllRead = null!, btnRefresh = null!;
        private Label lblStatus = null!, lblAlertSummary = null!;
        private Label lblDetailTitle = null!, lblDetailConf = null!, lblDetailTime = null!, lblDetailTx = null!;

        public ucAlerts(AlertService alertService)
        {
            _alertService = alertService;
            BuildResponsiveUI();
            LoadAlerts();
        }

        private void BuildResponsiveUI()
        {
            this.BackColor = Theme.BgPrimary;
            this.Padding = new Padding(15);
            this.Dock = DockStyle.Fill;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Data Grid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Detail Panel

            // ── Header ──
            lblAlertSummary = new Label
            {
                Text = "SYSTEM ALERTS",
                Font = Theme.HeadingMd,
                ForeColor = Theme.TextWhite,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0,0,0,10),
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblAlertSummary, 0, 0);

            // ── Actions ──
            var controlLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 4, Margin = new Padding(0) };
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            btnMarkRead = Theme.CreateSecondaryButton("MARK AS READ");
            btnMarkRead.Width = 160; btnMarkRead.Height = 35;
            btnMarkRead.Click += async (s, e) => await MarkSelectedAsRead();

            btnMarkAllRead = Theme.CreateSecondaryButton("CLEAR ALL");
            btnMarkAllRead.Width = 160; btnMarkAllRead.Height = 35;
            btnMarkAllRead.Click += async (s, e) => await MarkAllAsRead();

            btnRefresh = Theme.CreatePrimaryButton("REFRESH DATA");
            btnRefresh.Width = 160; btnRefresh.Height = 35;
            btnRefresh.Click += (s, e) => LoadAlerts();

            lblStatus = new Label
            {
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Fill,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight
            };

            controlLayout.Controls.Add(btnMarkRead, 0, 0);
            controlLayout.Controls.Add(btnMarkAllRead, 1, 0);
            controlLayout.Controls.Add(btnRefresh, 2, 0);
            controlLayout.Controls.Add(lblStatus, 3, 0);
            
            mainLayout.Controls.Add(controlLayout, 0, 1);

            // ── Grid ──
            dgvAlerts = new DataGridView { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 10) };
            Theme.StyleGrid(dgvAlerts);
            dgvAlerts.SelectionChanged += DgvAlerts_SelectionChanged;
            mainLayout.Controls.Add(dgvAlerts, 0, 2);

            // ── Detail Card ──
            var pnlDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                MinimumSize = new Size(0, 100) // Ensure it never collapses completely
            };
            pnlDetail.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlDetail.ClientRectangle, Theme.Border, ButtonBorderStyle.Solid);

            var detailLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 2, Padding = new Padding(15) };
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            lblDetailTitle = new Label { Text = "Select an alert to view payload details", Font = Theme.BodyBold, ForeColor = Theme.TextWhite, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
            lblDetailConf = new Label { Text = "[]", Font = Theme.BodyBold, ForeColor = Theme.AccentRed, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomRight };
            lblDetailTime = new Label { Text = "...", Font = Theme.Caption, ForeColor = Theme.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
            lblDetailTx = new Label { Text = "...", Font = Theme.Caption, ForeColor = Theme.TextMuted, Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopRight };

            detailLayout.Controls.Add(lblDetailTitle, 0, 0);
            detailLayout.Controls.Add(lblDetailConf, 1, 0);
            detailLayout.Controls.Add(lblDetailTime, 0, 1);
            detailLayout.Controls.Add(lblDetailTx, 1, 1);

            var stripe = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Theme.AccentRed };
            pnlDetail.Controls.Add(detailLayout);
            pnlDetail.Controls.Add(stripe);

            mainLayout.Controls.Add(pnlDetail, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private async void LoadAlerts()
        {
            try
            {
                DataTable data = await _alertService.GetAlertsDataTableAsync();
                dgvAlerts.DataSource = data;

                int unread = await _alertService.GetUnreadAlertCountAsync();
                int total = data.Rows.Count;

                lblAlertSummary.Text = $"SYSTEM ALERTS — {unread} unread of {total} total";
                lblAlertSummary.ForeColor = unread > 0 ? Theme.AccentRed : Theme.TextWhite;

                if (dgvAlerts.Columns.Count > 0)
                {
                    if (dgvAlerts.Columns["Confidence"] is DataGridViewColumn c1) c1.DefaultCellStyle.Format = "P2";
                    if (dgvAlerts.Columns["CreatedAt"] is DataGridViewColumn c2) c2.DefaultCellStyle.Format = "g";
                    if (dgvAlerts.Columns["TransactionAmount"] is DataGridViewColumn c3) c3.DefaultCellStyle.Format = "C2";
                }
                lblStatus.Text = $"Loaded {total} integrity alerts";
            }
            catch (Exception ex)
            {
                lblAlertSummary.Text = "SYSTEM ALERTS — Offline";
                lblStatus.Text = "Connection lost";
                Console.WriteLine($"Alert load: {ex.Message}");
            }
        }

        private void DgvAlerts_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvAlerts.SelectedRows.Count == 0) return;
            var row = dgvAlerts.SelectedRows[0];
            try
            {
                string id = row.Cells["AlertId"]?.Value?.ToString() ?? "?";
                string conf = row.Cells["Confidence"]?.Value != null ? Convert.ToDecimal(row.Cells["Confidence"].Value).ToString("P2") : "?";
                string time = row.Cells["CreatedAt"]?.Value?.ToString() ?? "?";
                string amt = row.Cells["TransactionAmount"]?.Value != null ? Convert.ToDecimal(row.Cells["TransactionAmount"].Value).ToString("C") : "?";
                string type = row.Cells["TransactionType"]?.Value?.ToString() ?? "?";
                bool read = row.Cells["IsRead"]?.Value != null && Convert.ToBoolean(row.Cells["IsRead"].Value);

                lblDetailTitle.Text = $"Alert Array #{id} — {(read ? "READ" : "UNREAD")}";
                lblDetailTitle.ForeColor = read ? Theme.TextWhite : Theme.AccentRed;
                lblDetailConf.Text = $"AI Confidence Level: {conf}";
                lblDetailTime.Text = $"Detected at: {time}";
                lblDetailTx.Text = $"Linked Tx: {type} | Volume: {amt}";
            }
            catch { lblDetailTitle.Text = "Details unavailable"; }
        }

        private async Task MarkSelectedAsRead()
        {
            if (dgvAlerts.SelectedRows.Count == 0) { lblStatus.Text = "Select an alert first"; return; }
            try
            {
                int id = Convert.ToInt32(dgvAlerts.SelectedRows[0].Cells["AlertId"].Value);
                if (await _alertService.MarkAsReadAsync(id))
                {
                    lblStatus.Text = $"Alert Array #{id} neutralized";
                    LoadAlerts();
                }
            }
            catch (Exception ex) { lblStatus.Text = "Failed"; Console.WriteLine(ex.Message); }
        }

        private async Task MarkAllAsRead()
        {
            try
            {
                await _alertService.MarkAllAsReadAsync();
                lblStatus.Text = "All alerts neutralized";
                LoadAlerts();
            }
            catch (Exception ex) { lblStatus.Text = "Failed"; Console.WriteLine(ex.Message); }
        }
    }
}
