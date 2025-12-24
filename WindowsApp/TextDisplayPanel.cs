using System;
using System.Drawing;
using System.Windows.Forms;
using PromptArqApp.Theming;

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
        private const int ContentPadding = 15;
        private const int MarginBetweenForms = 15; // Space between this panel and command palette

        public TextDisplayPanel()
        {
            // Form settings
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            Enabled = false; // Make non-interactive

            // Apply dark theme using WindowStyleManager
            WindowStyleManager.ApplyDarkTheme(this);

            // Register with ThemeManager
            ThemeManager.Instance.RegisterForm(this);
            ThemeManager.Instance.ApplyThemeToForm(this);

            // Subscribe to theme changes
            EventHandler<ThemeChangedEventArgs> themeChangedHandler = (s, e) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => ThemeManager.Instance.ApplyThemeToForm(this)));
                }
                else
                {
                    ThemeManager.Instance.ApplyThemeToForm(this);
                }
                Invalidate(true);
            };
            ThemeManager.Instance.ThemeChanged += themeChangedHandler;
            
            // Cleanup on closing
            FormClosing += (s, e) =>
            {
                ThemeManager.Instance.ThemeChanged -= themeChangedHandler;
            };

            // Content panel with padding - add border like CommandPalette
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = WindowStyleManager.DarkBackgroundColor,
                Padding = new System.Windows.Forms.Padding(ContentPadding + 1) // +1 for border
            };

            // Add Paint event to draw border
            _contentPanel.Paint += ContentPanel_Paint;

            // Text display - using RichTextBox for better text rendering and scrolling
            _textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                TabStop = false,
                Cursor = Cursors.Default,
                Margin = new System.Windows.Forms.Padding(0)
            };
            
            // Apply dark theme to RichTextBox using WindowStyleManager
            WindowStyleManager.ApplyDarkThemeToRichTextBox(_textBox);

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

        private void ContentPanel_Paint(object? sender, PaintEventArgs e)
        {
            // Ensure background is filled with dark color
            using (var brush = new SolidBrush(WindowStyleManager.DarkBackgroundColor))
            {
                e.Graphics.FillRectangle(brush, _contentPanel.ClientRectangle);
            }
            
            // Draw a subtle border around the panel (similar to CommandPalette)
            using (var pen = new Pen(Color.FromArgb(60, 60, 60), 1))
            {
                var rect = _contentPanel.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

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

            // Set text and ensure colors are applied
            _textBox.Text = text;
            WindowStyleManager.ApplyTextColorsToRichTextBox(_textBox);

            // Calculate optimal size based on content and screen
            var screen = Screen.FromControl(referenceForm);
            var workingArea = screen.WorkingArea;

            // Calculate maximum dimensions based on screen size
            int maxWidth = (int)(workingArea.Width * MaxWidthPercent / 100.0);
            int maxHeight = (int)(workingArea.Height * MaxHeightPercent / 100.0);

            // Measure text to determine required size
            var requiredSize = MeasureTextSize(text, maxWidth - (ContentPadding * 2));

            // Add padding to required size
            int width = Math.Max(MinWidth, Math.Min(maxWidth, requiredSize.Width + (ContentPadding * 2)));
            int height = Math.Max(MinHeight, Math.Min(maxHeight, requiredSize.Height + (ContentPadding * 2)));

            // Set size
            Size = new Size(width, height);

            // Update rounded corners for new size
            WindowStyleManager.ApplyRoundedCorners(this, WindowStyleManager.DefaultCornerRadius);

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
            WindowStyleManager.ApplyTextColorsToRichTextBox(_textBox);
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Fill the entire form background with dark color
            using (var brush = new SolidBrush(WindowStyleManager.DarkBackgroundColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw a subtle rounded border around the entire form
            using (var pen = new Pen(Color.FromArgb(70, 70, 70), 2))
            {
                var rect = new Rectangle(1, 1, Width - 2, Height - 2);
                using (var path = GetRoundedRectPath(rect, WindowStyleManager.DefaultCornerRadius))
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }
    }
}
