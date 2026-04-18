using System.Drawing;
using System.Windows.Forms;

namespace FinancialApp.UI
{
    /// <summary>
    /// Ultra-Premium Deep Purple + Stark White Aesthetic (AEGIS INTELLIGENCE).
    /// Focuses on deep monochromatic space, extreme contrast, and expert-grade minimalism.
    /// </summary>
    public static class Theme
    {
        // ─── Backgrounds (Deepest Purple to Elevated Purple) ───
        public static readonly Color BgPrimary  = Color.FromArgb(13, 9, 20);     // Pitch Dark Void Purple (App Background)
        public static readonly Color BgSidebar  = Color.FromArgb(18, 12, 28);    // Sidebar slightly decoupled
        public static readonly Color BgCard     = Color.FromArgb(23, 18, 33);    // Panels / Cards
        public static readonly Color BgInput    = Color.FromArgb(29, 23, 42);    // Text fields
        public static readonly Color BgHover    = Color.FromArgb(39, 31, 56);    // Hover states

        // ─── Accents & Interactive (Stark White & High Contrast) ───
        public static readonly Color AccentPrimary = Color.FromArgb(255, 255, 255); // Stark White for primary actions
        public static readonly Color AccentBlue    = Color.FromArgb(139, 92, 246);  // Vibrant Purple (Replaces Blue)
        public static readonly Color AccentGreen   = Color.FromArgb(52, 211, 153);  // Vibrant Success Green
        public static readonly Color AccentRed     = Color.FromArgb(248, 113, 113); // Warning / Fraud Alert Red

        // ─── Typography ───
        public static readonly Color TextWhite  = Color.FromArgb(250, 250, 250); // Primary Headers
        public static readonly Color TextSilver = Color.FromArgb(200, 205, 215); // Standard Body
        public static readonly Color TextMuted  = Color.FromArgb(110, 120, 135); // Hints, Labels
        public static readonly Color TextInverse= Color.FromArgb(13, 9, 20);     // Text on White buttons (Deep Purple)

        // ─── Borders ───
        public static readonly Color Border     = Color.FromArgb(46, 39, 63);    // Subtle purple-slate lines
        public static readonly Color GridLine   = Color.FromArgb(37, 31, 51);    // Chart / Grid lines

        // ─── Fonts ───
        public static readonly Font HeadingLg   = new Font("Segoe UI", 22F, FontStyle.Bold);
        public static readonly Font HeadingMd   = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font BodyLg      = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font Body        = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font BodyBold    = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font BodySm      = new Font("Segoe UI", 9F, FontStyle.Regular);
        public static readonly Font Caption     = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font CaptionBold = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        public static readonly Font KpiValue    = new Font("Segoe UI", 26F, FontStyle.Bold);
        public static readonly Font InputFont   = new Font("Segoe UI", 11F, FontStyle.Regular);

        // ─── Chart Colors (Monochromatic Purple + Sharp Accents) ───
        public static readonly Color[] ChartColors = new[]
        {
            Color.FromArgb(250, 250, 250), // Stark White
            Color.FromArgb(139, 92, 246),  // Vibrant Purple
            Color.FromArgb(167, 139, 250), // Light Purple
            Color.FromArgb(196, 181, 253), // Pale Purple
            Color.FromArgb(109, 40, 217)   // Deep Purple
        };

        public static void StyleGrid(DataGridView grid)
        {
            grid.BackgroundColor = BgCard;
            grid.GridColor = Border;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Font = Body;
            grid.EnableHeadersVisualStyles = false;

            // Header - Monochromatic Dark
            grid.ColumnHeadersDefaultCellStyle.BackColor = BgSidebar;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSilver;
            grid.ColumnHeadersDefaultCellStyle.Font = BodyBold;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = BgSidebar;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            grid.ColumnHeadersHeight = 45;

            // Rows
            grid.DefaultCellStyle.BackColor = BgCard;
            grid.DefaultCellStyle.ForeColor = TextSilver;
            grid.DefaultCellStyle.SelectionBackColor = BgHover;
            grid.DefaultCellStyle.SelectionForeColor = TextWhite;
            grid.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = BgPrimary;
            grid.RowTemplate.Height = 42;
        }

        // Primary Button (White block, dark text) - Super Premium
        public static Button CreatePrimaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Font = BodyBold,
                BackColor = AccentPrimary,
                ForeColor = TextInverse,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            // Native winforms hovering for white background can be tricky, leaving default Flat styling handle it cleanly
            return btn;
        }

        // Secondary Button (Dark block, white text)
        public static Button CreateSecondaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Font = BodyBold,
                BackColor = BgInput,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Border;
            return btn;
        }
    }
}
