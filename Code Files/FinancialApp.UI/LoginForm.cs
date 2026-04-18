using System;
using System.Drawing;
using System.Windows.Forms;
using FinancialApp.BLL.Services;

namespace FinancialApp.UI
{
    public partial class LoginForm : Form
    {
        private readonly AuthenticationService _authService;
        private readonly IServiceProvider _serviceProvider;
        private Label lblStatus = null!;

        public LoginForm(AuthenticationService authService, IServiceProvider serviceProvider)
        {
            _authService = authService;
            _serviceProvider = serviceProvider;
            
            this.Text = "AI Financial System - Secure Access";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            BuildResponsiveUI();
        }

        private void BuildResponsiveUI()
        {
            this.BackColor = Theme.BgPrimary;

            // Inner Card - fills the entire window now with a padding
            var pnlLoginCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Padding = new Padding(40)
            };
            
            // Add slight border to card
            pnlLoginCard.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, pnlLoginCard.ClientRectangle, Theme.Border, ButtonBorderStyle.Solid);
            };

            // Layout inside card
            var cardLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8
            };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Row 0: Title
            var lblTitle = new Label
            {
                Text = "AI Financial System",
                Font = new Font("Century Gothic", 16F, FontStyle.Bold | FontStyle.Italic),
                ForeColor = Theme.TextWhite,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 5)
            };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 1: Subtitle
            var lblSubtitle = new Label
            {
                Text = "ENTERPRISE FRAUD DETECTION",
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 2: Username Label
            var lblUser = new Label { Text = "USER IDENTIFIER", Font = Theme.BodyBold, ForeColor = Theme.TextSilver, Dock = DockStyle.Bottom, AutoSize = true, Margin = new Padding(0,0,0,5) };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 3: Username Input
            txtUsername = new TextBox
            {
                Font = Theme.InputFont,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 4: Password Label
            var lblPass = new Label { Text = "ACCESS KEY", Font = Theme.BodyBold, ForeColor = Theme.TextSilver, Dock = DockStyle.Bottom, AutoSize = true, Margin = new Padding(0,0,0,5) };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 5: Password Input
            txtPassword = new TextBox
            {
                Font = Theme.InputFont,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                PasswordChar = '•',
                Margin = new Padding(0, 0, 0, 25)
            };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 6: Button
            btnLogin = Theme.CreatePrimaryButton("AUTHENTICATE");
            btnLogin.Dock = DockStyle.Top;
            btnLogin.Height = 55;
            btnLogin.Click += BtnLogin_Click;
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 7: Status
            lblStatus = new Label
            {
                Text = "Connection Secure (256-bit AES)",
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Add to card layout
            cardLayout.Controls.Add(lblTitle, 0, 0);
            cardLayout.Controls.Add(lblSubtitle, 0, 1);
            cardLayout.Controls.Add(lblUser, 0, 2);
            cardLayout.Controls.Add(txtUsername, 0, 3);
            cardLayout.Controls.Add(lblPass, 0, 4);
            cardLayout.Controls.Add(txtPassword, 0, 5);
            cardLayout.Controls.Add(btnLogin, 0, 6);
            cardLayout.Controls.Add(lblStatus, 0, 7);

            pnlLoginCard.Controls.Add(cardLayout);
            this.Controls.Add(pnlLoginCard);
            
            this.AcceptButton = btnLogin;
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = "Authenticating payload...";
            lblStatus.ForeColor = Theme.TextSilver;
            btnLogin.Enabled = false;

            var user = await _authService.AuthenticateAsync(txtUsername.Text, txtPassword.Text);

            if (user != null)
            {
                lblStatus.Text = "Authentication successful. Initializing data...";
                lblStatus.ForeColor = Theme.AccentGreen;

                // Ensure demo accounts + transactions exist so charts are populated
                var accountService = (AccountService)_serviceProvider.GetService(typeof(AccountService))!;
                await accountService.EnsureAccountExistsAsync(user.UserId);

                lblStatus.Text = "Booting System...";
                
                var mainForm = new MainForm(_serviceProvider, user);
                mainForm.FormClosed += (s, args) => this.Close();
                this.Hide();
                mainForm.Show();
            }
            else
            {
                lblStatus.Text = "Authentication failed. Invalid credentials.";
                lblStatus.ForeColor = Theme.AccentRed;
                btnLogin.Enabled = true;
            }
        }
    }
}