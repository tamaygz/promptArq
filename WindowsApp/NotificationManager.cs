using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromptArqApp
{
    /// <summary>
    /// Manages toast notifications across all forms.
    /// Consolidates notification logic with customizable positioning and styling.
    /// </summary>
    public static class NotificationManager
    {
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

            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = options.BackColor ?? Color.FromArgb(45, 45, 45),
                ForeColor = options.ForeColor ?? Color.White,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new Size(options.Width, options.Height),
                Opacity = options.Opacity
            };

            var label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = options.Font ?? new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = options.ForeColor ?? Color.White,
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
                Position = ToastPosition.BottomRight,
                BackColor = Color.FromArgb(45, 45, 45)
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
                BackColor = Color.FromArgb(50, 50, 50),
                BottomMargin = 50
            });
        }
    }
}
