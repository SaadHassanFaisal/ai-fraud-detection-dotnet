using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using FinancialApp.BLL.Services;
using FinancialApp.Models.Entities;

namespace FinancialApp.UI
{
    public partial class ucTransactions : UserControl
    {
        private readonly TransactionService _transactionService;
        private readonly AccountService _accountService;
        private readonly ExportService _exportService;
        private readonly User _currentUser;

        private TabControl tabActions = null!;
        private TextBox txtDepositAmount = null!, txtWithdrawAmount = null!;
        private TextBox txtTransferAmount = null!, txtTargetAccount = null!;
        private Button btnDeposit = null!, btnWithdraw = null!, btnTransfer = null!, btnExport = null!;
        private DataGridView dgvHistory = null!;
        private Label lblStatus = null!;
        private Label lblHint = null!;

        public ucTransactions(TransactionService transactionService, AccountService accountService, ExportService exportService, User currentUser)
        {
            _transactionService = transactionService;
            _accountService = accountService;
            _exportService = exportService;
            _currentUser = currentUser;
            BuildResponsiveUI();
            LoadTransactionHistory();
            _ = LoadAccountsAsync();
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 280F)); // Tab Control
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Status label
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Grid Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // DataGridView Grid

            // ═══ TAB CONTROL ═══
            tabActions = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = Theme.BodyBold,
                ItemSize = new Size(120, 35) // Make tabs chunkier and more modern
            };

            // ── Deposit ──
            var tabDeposit = new TabPage("  Deposit  ") { BackColor = Theme.BgCard, Padding = new Padding(20) };
            var depLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            depLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            depLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            depLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblDepAmt = MakeLabel("AMOUNT TO DEPOSIT");
            lblDepAmt.AutoSize = true; lblDepAmt.Margin = new Padding(0,0,0,10);
            txtDepositAmount = MakeInput();
            txtDepositAmount.PlaceholderText = "Enter amount (e.g. 500.00)";
            txtDepositAmount.Margin = new Padding(0,0,0,20);
            btnDeposit = Theme.CreatePrimaryButton("EXECUTE DEPOSIT");
            btnDeposit.Dock = DockStyle.Top;
            btnDeposit.Height = 50;  btnDeposit.Margin = new Padding(0);
            btnDeposit.Click += async (s, e) => await ExecuteDeposit();

            depLayout.Controls.Add(lblDepAmt, 0, 0);
            depLayout.Controls.Add(txtDepositAmount, 0, 1);
            depLayout.Controls.Add(btnDeposit, 0, 2);
            tabDeposit.Controls.Add(depLayout);

            // ── Withdraw ──
            var tabWithdraw = new TabPage("  Withdraw  ") { BackColor = Theme.BgCard, Padding = new Padding(20) };
            var wdLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            wdLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            wdLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            wdLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblWdAmt = MakeLabel("AMOUNT TO WITHDRAW");
            lblWdAmt.AutoSize = true; lblWdAmt.Margin = new Padding(0,0,0,10);
            txtWithdrawAmount = MakeInput();
            txtWithdrawAmount.PlaceholderText = "Enter amount (e.g. 200.00)";
            txtWithdrawAmount.Margin = new Padding(0,0,0,20);
            btnWithdraw = Theme.CreatePrimaryButton("EXECUTE WITHDRAWAL");
            btnWithdraw.Dock = DockStyle.Top;
            btnWithdraw.Height = 50; btnWithdraw.Margin = new Padding(0);
            btnWithdraw.Click += async (s, e) => await ExecuteWithdraw();

            wdLayout.Controls.Add(lblWdAmt, 0, 0);
            wdLayout.Controls.Add(txtWithdrawAmount, 0, 1);
            wdLayout.Controls.Add(btnWithdraw, 0, 2);
            tabWithdraw.Controls.Add(wdLayout);

            // ── Transfer ──
            var tabTransfer = new TabPage("  Transfer  ") { BackColor = Theme.BgCard, Padding = new Padding(20) };
            var trLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 2 };
            // Ensure proper split
            trLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            trLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var pnlTrLeft = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
            pnlTrLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlTrLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlTrLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            pnlTrLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var lblTrAmt = MakeLabel("AMOUNT"); lblTrAmt.AutoSize = true; lblTrAmt.Margin = new Padding(0,0,0,10);
            txtTransferAmount = MakeInput(); txtTransferAmount.Width = 300; txtTransferAmount.Margin = new Padding(0,0,0,20);
            txtTransferAmount.PlaceholderText = "Enter amount";
            
            var lblTrTgt = MakeLabel("TARGET ACCOUNT ID"); lblTrTgt.AutoSize = true; lblTrTgt.Margin = new Padding(0,0,0,10);
            txtTargetAccount = MakeInput(); txtTargetAccount.Width = 300; txtTargetAccount.Margin = new Padding(0,0,0,20);
            txtTargetAccount.PlaceholderText = "Enter target account ID";
            
            pnlTrLeft.Controls.Add(lblTrAmt, 0, 0);
            pnlTrLeft.Controls.Add(txtTransferAmount, 0, 1);
            pnlTrLeft.Controls.Add(lblTrTgt, 0, 2);
            pnlTrLeft.Controls.Add(txtTargetAccount, 0, 3);

            var pnlTrRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 0, 0) };
            btnTransfer = Theme.CreatePrimaryButton("EXECUTE SECURE TRANSFER");
            btnTransfer.Dock = DockStyle.Top;
            btnTransfer.Height = 50;
            btnTransfer.Click += async (s, e) => await ExecuteTransfer();

            lblHint = new Label
            {
                Text = "Loading your accounts...",
                Font = Theme.Caption,
                ForeColor = Theme.TextMuted,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 10),
                TextAlign = ContentAlignment.TopCenter
            };
            pnlTrRight.Controls.AddRange(new Control[] { lblHint, btnTransfer });

            trLayout.Controls.Add(pnlTrLeft, 0, 0);
            trLayout.Controls.Add(pnlTrRight, 1, 0);
            tabTransfer.Controls.Add(trLayout);

            tabActions.TabPages.AddRange(new[] { tabDeposit, tabWithdraw, tabTransfer });
            mainLayout.Controls.Add(tabActions, 0, 0);

            // ═══ STATUS ═══
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.BodySm,
                ForeColor = Theme.TextSilver,
                TextAlign = ContentAlignment.BottomLeft,
                AutoSize = true,
                Margin = new Padding(0,10,0,5),
                Text = $"Logged in as: {_currentUser.Username}"
            };
            mainLayout.Controls.Add(lblStatus, 0, 1);

            // ═══ HISTORY HEADER + EXPORT ═══
            var gridHeaderLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                Margin = new Padding(0)
            };
            gridHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            gridHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));

            var lblGridTitle = new Label
            {
                Text = "SYSTEM TRANSACTION LOGS",
                Font = Theme.BodyBold,
                ForeColor = Theme.TextWhite,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                AutoSize = true,
                Padding = new Padding(0, 5, 0, 10)
            };

            // Blueprint requirement: "Export transaction report to Excel (ClosedXML)"
            btnExport = Theme.CreateSecondaryButton("EXPORT TO EXCEL");
            btnExport.Dock = DockStyle.Fill;
            btnExport.Height = 35;
            btnExport.Click += async (s, e) => await ExportToExcel();

            gridHeaderLayout.Controls.Add(lblGridTitle, 0, 0);
            gridHeaderLayout.Controls.Add(btnExport, 1, 0);
            mainLayout.Controls.Add(gridHeaderLayout, 0, 2);

            dgvHistory = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleGrid(dgvHistory);
            mainLayout.Controls.Add(dgvHistory, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private Label MakeLabel(string text)
        {
            return new Label { Text = text, Font = Theme.CaptionBold, ForeColor = Theme.TextSilver, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
        }

        private TextBox MakeInput()
        {
            return new TextBox
            {
                Dock = DockStyle.Top,
                Font = Theme.InputFont,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void LoadTransactionHistory()
        {
            try
            {
                DataTable history = _transactionService.GetTransactionHistory(_currentUser.UserId);
                dgvHistory.DataSource = history;

                if (dgvHistory.Columns.Count > 0)
                {
                    if (dgvHistory.Columns["Amount"] is DataGridViewColumn c1) c1.DefaultCellStyle.Format = "C2";
                    if (dgvHistory.Columns["Timestamp"] is DataGridViewColumn c2) c2.DefaultCellStyle.Format = "g";
                }
            }
            catch (Exception ex) { Console.WriteLine($"History load error: {ex.Message}"); }
        }

        private async Task LoadAccountsAsync()
        {
            try
            {
                var accounts = await _accountService.GetUserAccountsAsync(_currentUser.UserId);
                string text = "Your Registered Accs:\n";
                foreach(var acc in accounts)
                {
                    text += $"[{acc.AccountType}] ID: {acc.AccountId} - {acc.Balance:C}\n";
                }
                lblHint.Text = text.TrimEnd();
            }
            catch { lblHint.Text = "Failed to load accounts."; }
        }

        private async Task ExecuteDeposit()
        {
            if (!decimal.TryParse(txtDepositAmount.Text, out decimal amount)) { ShowStatus("Err: Valid amount required.", true); return; }
            btnDeposit.Enabled = false;
            ShowStatus("Executing deposit sequence...", false);
            try
            {
                var r = await _transactionService.ExecuteDepositAsync(_currentUser.UserId, amount);
                ShowStatus(r.Success ? r.Message : r.Message, !r.Success);
                if (r.Success) { txtDepositAmount.Clear(); LoadTransactionHistory(); _ = LoadAccountsAsync(); }
            }
            catch (Exception ex) { ShowStatus($"Error: {ex.Message}", true); }
            finally { btnDeposit.Enabled = true; }
        }

        private async Task ExecuteWithdraw()
        {
            if (!decimal.TryParse(txtWithdrawAmount.Text, out decimal amount)) { ShowStatus("Err: Valid amount required.", true); return; }
            btnWithdraw.Enabled = false;
            ShowStatus("Executing withdraw sequence...", false);
            try
            {
                var r = await _transactionService.ExecuteWithdrawalAsync(_currentUser.UserId, amount);
                ShowStatus(r.Message, !r.Success);
                if (r.Success) { txtWithdrawAmount.Clear(); LoadTransactionHistory(); _ = LoadAccountsAsync(); }
            }
            catch (Exception ex) { ShowStatus($"Error: {ex.Message}", true); }
            finally { btnWithdraw.Enabled = true; }
        }

        private async Task ExecuteTransfer()
        {
            if (!decimal.TryParse(txtTransferAmount.Text, out decimal amount)) { ShowStatus("Err: Valid amount required.", true); return; }
            if (!int.TryParse(txtTargetAccount.Text, out int targetId)) { ShowStatus("Err: Valid target ID required.", true); return; }

            btnTransfer.Enabled = false;
            ShowStatus("Executing transfer and initializing AI Fraud Model analysis...", false);
            try
            {
                var r = await _transactionService.ExecuteTransferAsync(_currentUser.UserId, amount, targetId);
                if (r.IsFraud)
                {
                    ShowStatus($"BLOCKED — AI Confidence: {(r.Confidence * 100):0.00}%", true);
                    MessageBox.Show(r.Message, "System Integrity Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (r.Success) { ShowStatus("Transfer authorized and completed.", false); txtTransferAmount.Clear(); txtTargetAccount.Clear(); LoadTransactionHistory(); _ = LoadAccountsAsync(); }
                else { ShowStatus(r.Message, true); }
            }
            catch (Exception ex) { ShowStatus($"Network Error: {ex.Message}", true); }
            finally { btnTransfer.Enabled = true; }
        }

        private void ShowStatus(string msg, bool isError)
        {
            lblStatus.Text = $">> {msg}";
            lblStatus.ForeColor = isError ? Theme.AccentRed : Theme.AccentGreen;
        }

        private async Task ExportToExcel()
        {
            btnExport.Enabled = false;
            ShowStatus("Generating Excel file via ClosedXML...", false);
            try
            {
                // Use the environment's desktop for the export destination
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string path = await _exportService.ExportTransactionsToExcelAsync(_currentUser.UserId, dir);
                ShowStatus($"Export successful! Saved to: {path}", false);
                
                // Ask user if they want to open it immediately
                if (MessageBox.Show("Export complete. Would you like to open the report?", "Export Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Export Failed: {ex.Message}", true);
            }
            finally
            {
                btnExport.Enabled = true;
            }
        }
    }
}