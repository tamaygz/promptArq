using System;
using System.Drawing;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    /// <summary>
    /// Manages toast notifications across all forms.
    /// Consolidates notification logic with customizable positioning and styling.
    /// </summary>
    public static class NotificationManager
    {
        /// <summary>
        /// Custom form that doesn't steal focus when shown
        /// </summary>
        private class ToastForm : Form
        {
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOOLWINDOW = 0x00000080;

            protected override bool ShowWithoutActivation
            {
                get { return true; }
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams baseParams = base.CreateParams;
                    baseParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                    return baseParams;
                }
            }
        }
        /// <summary>
        /// Toast notification positioning options
        /// </summary>
        public enum ToastPosition
        {
            BottomRight,
            BottomCenter,
            TopRight,
            TopCenter,
            Custom
        }

        /// <summary>
        /// Configuration options for toast notifications
        /// </summary>
        public class ToastOptions
        {
            public ToastPosition Position { get; set; } = ToastPosition.BottomRight;
            public Point? CustomPosition { get; set; }
            public Color? BackColor { get; set; }
            public Color? ForeColor { get; set; }
            public double Opacity { get; set; } = 0.95;
            public int Width { get; set; } = 300;
            public int Height { get; set; } = 60;
            public int CornerRadius { get; set; } = 10;
            public Font? Font { get; set; }
            public int BottomMargin { get; set; } = 20;
            public int SideMargin { get; set; } = 20;
        }

        /// <summary>
        /// Shows a temporary toast notification with auto-dismiss
        /// </summary>
        /// <param name="message">Message to display</param>
        /// <param name="durationMs">Duration in milliseconds before auto-close</param>
        /// <param name="options">Optional styling and positioning configuration</param>
        public static void ShowToast(string message, int durationMs, ToastOptions? options = null)
        {
            options ??= new ToastOptions();

            var toast = new ToastForm
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new Size(options.Width, options.Height),
                Opacity = options.Opacity
            };
            
            // Apply theme colors if not explicitly overridden
            var theme = ThemeManager.Instance.CurrentTheme;
            if (options.BackColor == null)
            {
                toast.BackColor = ThemeApplicator.ParseColor(theme.Colors.ControlBackground);
            }
            else
            {
                toast.BackColor = options.BackColor.Value;
            }

            var label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = options.Font ?? new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = options.ForeColor ?? ThemeApplicator.ParseColor(theme.Colors.Foreground),
                Padding = new Padding(10)
            };

            toast.Controls.Add(label);

            // Calculate position
            var screen = options.Position == ToastPosition.Custom && options.CustomPosition.HasValue
                ? Screen.FromPoint(options.CustomPosition.Value)
                : Screen.FromPoint(Cursor.Position);

            Point location = options.Position switch
            {
                ToastPosition.BottomRight => new Point(
                    screen.WorkingArea.Right - options.Width - options.SideMargin,
                    screen.WorkingArea.Bottom - options.Height - options.BottomMargin
                ),
                ToastPosition.BottomCenter => new Point(
                    screen.WorkingArea.Left + (screen.WorkingArea.Width - options.Width) / 2,
                    screen.WorkingArea.Bottom - options.Height - options.BottomMargin
                ),
                ToastPosition.TopRight => new Point(
                    screen.WorkingArea.Right - options.Width - options.SideMargin,
                    screen.WorkingArea.Top + options.BottomMargin
                ),
                ToastPosition.TopCenter => new Point(
                    screen.WorkingArea.Left + (screen.WorkingArea.Width - options.Width) / 2,
                    screen.WorkingArea.Top + options.BottomMargin
                ),
                ToastPosition.Custom => options.CustomPosition ?? new Point(
                    screen.WorkingArea.Right - options.Width - options.SideMargin,
                    screen.WorkingArea.Bottom - options.Height - options.BottomMargin
                ),
                _ => new Point(
                    screen.WorkingArea.Right - options.Width - options.SideMargin,
                    screen.WorkingArea.Bottom - options.Height - options.BottomMargin
                )
            };

            toast.Location = location;

            // Apply rounded corners
            if (options.CornerRadius > 0)
            {
                WindowStyleManager.ApplyRoundedCorners(toast, options.CornerRadius);
            }

            toast.Show();

            // Auto-close after duration
            var timer = new System.Windows.Forms.Timer { Interval = durationMs };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                toast.Close();
                toast.Dispose();
            };
            timer.Start();
        }

        /// <summary>
        /// Shows a toast at bottom-right (default MainForm style)
        /// </summary>
        public static void ShowToastBottomRight(string message, int durationMs)
        {
            ShowToast(message, durationMs, new ToastOptions
            {
                Position = ToastPosition.BottomRight
            });
        }

        /// <summary>
        /// Shows a toast at bottom-center (CommandPalette style)
        /// </summary>
        public static void ShowToastBottomCenter(string message, int durationMs)
        {
            ShowToast(message, durationMs, new ToastOptions
            {
                Position = ToastPosition.BottomCenter,
                BottomMargin = 50
            });
        }
    }
}
