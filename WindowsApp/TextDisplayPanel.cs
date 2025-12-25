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

        // Message constants for WndProc
        private const int WM_SETFOCUS = 0x07;
        private const int WM_ENABLE = 0x0A;
        private const int WM_SETCURSOR = 0x20;
        private const int WM_MOUSEACTIVATE = 0x21;
        private const int MA_NOACTIVATE = 3;

        public TextDisplayPanel()
        {
            // Form settings
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            // Do NOT set Enabled = false as it prevents BackColor from working
            // Instead, we override WndProc to prevent interaction
            Name = "TextDisplayPanel";
            Text = "TextDisplayPanel";
            AccessibleName = "TextDisplayPanel";

            // Content panel with padding
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new System.Windows.Forms.Padding(ContentPadding),
            };

            // Text display - using RichTextBox for better text rendering and scrolling
            _textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                TabStop = false,
                Cursor = Cursors.Default,
                Margin = new System.Windows.Forms.Padding(0),
                DetectUrls = false, // Disable URL detection
                Visible = true, // Explicitly set visible
                Enabled = true // Explicitly enable
            };

            // Prevent focus on RichTextBox - focus something else instead
            _textBox.Enter += (s, e) =>
            {
                // Focus the content panel instead to prevent cursor in textbox
                _contentPanel.Focus();
            };

            _contentPanel.Controls.Add(_textBox);

            Controls.Add(_contentPanel);


            // Register with ThemeManager and apply theme
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
            };
            ThemeManager.Instance.ThemeChanged += themeChangedHandler;

            // Cleanup on closing
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
                else
                {
                    ThemeManager.Instance.ThemeChanged -= themeChangedHandler;
                }
            };

        }



        /// <summary>
        /// Shows the text display panel to the left of the specified form.
        /// </summary>
        /// <param name="text">The text content to display</param>
        /// <param name="referenceForm">The form to position next to (e.g., CommandPaletteForm)</param>
        public void ShowText(string text, Form referenceForm)
        {
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] ShowText called - text null: {text == null}, empty: {string.IsNullOrEmpty(text)}, length: {text?.Length ?? 0}");
            
            if (string.IsNullOrEmpty(text))
            {
                System.Diagnostics.Debug.WriteLine("[TextDisplayPanel] Text is null/empty, hiding panel");
                Hide();
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] Setting text to _textBox (first 50 chars): {text.Substring(0, Math.Min(50, text.Length))}");
            
            // Clear any existing text first
            _textBox.Clear();

            // Set the text
            _textBox.Text = text;
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] _textBox.Text set, current length: {_textBox.Text?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] _textBox state - Visible: {_textBox.Visible}, Enabled: {_textBox.Enabled}, Width: {_textBox.Width}, Height: {_textBox.Height}");
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] _textBox.Bounds: {_textBox.Bounds}, Location: {_textBox.Location}");
            
            // Force a refresh to ensure text is rendered
            _textBox.Refresh();
            System.Diagnostics.Debug.WriteLine("[TextDisplayPanel] _textBox refreshed");

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

            // Reapply theme to update rounded corners for new size
            ThemeManager.Instance.ApplyThemeToForm(this);

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

            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] About to show panel at ({x}, {y}) with size {Width}x{Height}");
            
            // Show the panel
            Show();
            
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] Show() called, Visible: {Visible}, IsHandleCreated: {IsHandleCreated}");
            
            // Make sure the panel is visible
            Visible = true;
            TopMost = true;
            
            // Force child controls to be visible
            _contentPanel.Visible = true;
            _textBox.Visible = true;
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] After forcing visibility - _contentPanel.Visible: {_contentPanel.Visible}, _textBox.Visible: {_textBox.Visible}");
            
            // Force redraw
            _textBox.Refresh();
            Refresh();
            
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] After refresh - Panel Visible: {Visible}, TopMost: {TopMost}");

            // Ensure reference form stays visible, on top, and gets focus
            referenceForm.Show(); // Explicitly show it in case it was hidden
            referenceForm.Visible = true;
            referenceForm.TopMost = true;
            referenceForm.BringToFront();
            referenceForm.Activate();
            referenceForm.Focus();
            
            System.Diagnostics.Debug.WriteLine($"[TextDisplayPanel] Reference form state: {referenceForm.Name}, Visible: {referenceForm.Visible}, TopMost: {referenceForm.TopMost}");
        }

        /// <summary>
        /// Measures the size required to display the text.
        /// </summary>
        private Size MeasureTextSize(string text, int maxWidth)
        {
            using (var graphics = CreateGraphics())
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                var font = theme.Fonts.Default.ToFont();
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

        /// <summary>
        /// Override WndProc to prevent interaction while keeping the form enabled for proper theming
        /// This approach allows BackColor to work while preventing focus and interaction
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            // Block focus, enable, and cursor messages to make form non-interactive
            // But keep it technically "enabled" so BackColor works
            if (m.Msg == WM_SETFOCUS || m.Msg == WM_ENABLE || m.Msg == WM_SETCURSOR)
            {
                return; // Ignore these messages
            }
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
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
