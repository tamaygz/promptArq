using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptArqApp
{
    /// <summary>
    /// A non-interactive panel for displaying long text content next to the command palette.
    /// Automatically sizes based on content and available viewport space.
    /// </summary>
    public class TextDisplayPanel : Form
    {
        private readonly RichTextBox _textBox;
        private readonly Panel _contentPanel;
        private const int MaxWidthPercent = 30; // Maximum 30% of screen width
        private const int MaxHeightPercent = 80; // Maximum 80% of screen height
        private const int MinWidth = 350;
        private const int MinHeight = 200;
        private const int Padding = 15;
        private const int MarginBetweenForms = 15; // Space between this panel and command palette

        public TextDisplayPanel()
        {
            // Form settings - match CommandPaletteForm style
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(30, 30, 30);
            Opacity = 0.97;
            TopMost = true;
            ShowInTaskbar = false;
            Enabled = false; // Make non-interactive

            // Add rounded corners effect
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));

            // Content panel with padding
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(Padding)
            };

            // Text display - using RichTextBox for better text rendering and scrolling
            _textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                TabStop = false,
                Cursor = Cursors.Default
            };

            _contentPanel.Controls.Add(_textBox);
            Controls.Add(_contentPanel);

            // Prevent form from getting focus
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        /// <summary>
        /// Shows the text display panel to the left of the specified form.
        /// </summary>
        /// <param name="text">The text content to display</param>
        /// <param name="referenceForm">The form to position next to (e.g., CommandPaletteForm)</param>
        public void ShowText(string text, Form referenceForm)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            _textBox.Text = text;

            // Calculate optimal size based on content and screen
            var screen = Screen.FromControl(referenceForm);
            var workingArea = screen.WorkingArea;

            // Calculate maximum dimensions based on screen size
            int maxWidth = (int)(workingArea.Width * MaxWidthPercent / 100.0);
            int maxHeight = (int)(workingArea.Height * MaxHeightPercent / 100.0);

            // Measure text to determine required size
            var requiredSize = MeasureTextSize(text, maxWidth - (Padding * 2));

            // Add padding to required size
            int width = Math.Max(MinWidth, Math.Min(maxWidth, requiredSize.Width + (Padding * 2)));
            int height = Math.Max(MinHeight, Math.Min(maxHeight, requiredSize.Height + (Padding * 2)));

            // Set size
            Size = new Size(width, height);

            // Update rounded corners for new size
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));

            // Position to the left of the reference form
            int x = referenceForm.Left - Width - MarginBetweenForms;
            int y = referenceForm.Top;

            // Ensure it stays within screen bounds
            if (x < workingArea.Left)
            {
                // If not enough space on left, position on right side
                x = referenceForm.Right + MarginBetweenForms;
                
                // If still out of bounds, just place at screen edge
                if (x + Width > workingArea.Right)
                {
                    x = workingArea.Left + 10;
                }
            }

            // Adjust Y position if needed to fit on screen
            if (y + height > workingArea.Bottom)
            {
                y = workingArea.Bottom - height - 10;
            }
            if (y < workingArea.Top)
            {
                y = workingArea.Top + 10;
            }

            Location = new Point(x, y);

            // Show the panel
            Show();
            
            // Ensure reference form stays on top
            referenceForm.BringToFront();
        }

        /// <summary>
        /// Measures the size required to display the text.
        /// </summary>
        private Size MeasureTextSize(string text, int maxWidth)
        {
            using (var graphics = CreateGraphics())
            {
                var font = new Font("Segoe UI", 10F, FontStyle.Regular);
                var layoutSize = new SizeF(maxWidth, float.MaxValue);
                var measuredSize = graphics.MeasureString(text, font, layoutSize);
                
                // Add some buffer for scrollbar
                return new Size(
                    (int)Math.Ceiling(measuredSize.Width) + 30, // Buffer for scrollbar
                    (int)Math.Ceiling(measuredSize.Height) + 20 // Extra padding
                );
            }
        }

        /// <summary>
        /// Hides the text display panel.
        /// </summary>
        public new void Hide()
        {
            _textBox.Clear();
            base.Hide();
        }

        /// <summary>
        /// Updates the text content without changing position or size.
        /// </summary>
        public void UpdateText(string text)
        {
            _textBox.Text = text ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets whether scrollbars are visible.
        /// </summary>
        public RichTextBoxScrollBars ScrollBarsVisibility
        {
            get => _textBox.ScrollBars;
            set => _textBox.ScrollBars = value;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_NOACTIVATE prevents the form from being activated when shown
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }
    }
}
