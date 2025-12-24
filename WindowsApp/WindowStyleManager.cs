using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    /// <summary>
    /// Manages window styling including dark title bars, rounded corners, and dark theme colors.
    /// Consolidates DWM API, GDI32 API, and common styling functionality for consistent styling across all forms.
    /// Now integrated with ThemeManager for dynamic theming support.
    /// </summary>
    public static class WindowStyleManager
    {
        #region Color Properties (Dynamic from ThemeManager)

        /// <summary>
        /// Primary background color for forms (darkest)
        /// </summary>
        public static Color DarkBackgroundColor => GetThemeColor("Background", Color.FromArgb(30, 30, 30));

        /// <summary>
        /// Secondary background color for panels and controls (slightly lighter)
        /// </summary>
        public static Color DarkControlBackgroundColor => GetThemeColor("ControlBackground", Color.FromArgb(35, 35, 35));

        /// <summary>
        /// Background color for interactive elements like search boxes
        /// </summary>
        public static Color DarkInputBackgroundColor => GetThemeColor("InputBackground", Color.FromArgb(50, 50, 50));

        /// <summary>
        /// Header/section background color
        /// </summary>
        public static Color DarkHeaderBackgroundColor => GetThemeColor("HeaderBackground", Color.FromArgb(40, 40, 40));

        /// <summary>
        /// Foreground color for text
        /// </summary>
        public static Color LightForegroundColor => GetThemeColor("Foreground", Color.White);

        /// <summary>
        /// Foreground color for secondary/hint text
        /// </summary>
        public static Color DarkForegroundColor => GetThemeColor("SecondaryForeground", Color.Gray);

        /// <summary>
        /// Gets a color from the current theme with fallback
        /// </summary>
        private static Color GetThemeColor(string colorName, Color fallback)
        {
            try
            {
                // Check if ThemeManager is initialized
                var manager = ThemeManager.Instance;
                if (manager?.CurrentTheme != null)
                {
                    return manager.GetColor(colorName);
                }
            }
            catch
            {
                // ThemeManager not initialized yet, use fallback
            }
            return fallback;
        }

        /// <summary>
        /// Default corner radius for rounded windows
        /// </summary>
        public const int DefaultCornerRadius = 15;

        /// <summary>
        /// Default form opacity
        /// </summary>
        public const double DefaultOpacity = 0.97;

        #endregion

        #region DWM API for Dark Title Bar

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        #endregion

        #region GDI32 API for Rounded Corners

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        #endregion

        #region Public API

        /// <summary>
        /// Applies dark mode title bar to a form using DWM API.
        /// Uses theme colors by default, or custom colors if specified.
        /// </summary>
        /// <param name="form">The form to style</param>
        /// <param name="captionColor">Optional caption color in BGR format (0x00BBGGRR)</param>
        /// <param name="borderColor">Optional border color in BGR format (0x00BBGGRR)</param>
        public static void ApplyDarkTitleBar(Form form, int? captionColor = null, int? borderColor = null)
        {
            if (form.Handle == IntPtr.Zero)
            {
                Debug.WriteLine("[WindowStyleManager] Cannot apply dark title bar - form handle not created yet");
                return;
            }

            try
            {
                // Enable dark mode for title bar
                int useDarkMode = 1;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

                // Extend the frame into the client area
                MARGINS margins = new MARGINS
                {
                    cxLeftWidth = 8,
                    cxRightWidth = 8,
                    cyBottomHeight = 22,
                    cyTopHeight = 22
                };
                DwmExtendFrameIntoClientArea(form.Handle, ref margins);

                // Get colors from theme if available
                int caption = captionColor ?? GetThemeTitleBarColor();
                int border = borderColor ?? captionColor ?? GetThemeBorderColor();

                // Set caption color
                DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));

                // Set border color
                DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));

                Debug.WriteLine("[WindowStyleManager] Dark title bar applied successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowStyleManager] Failed to set dark title bar: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the title bar color from the current theme, or returns default
        /// </summary>
        private static int GetThemeTitleBarColor()
        {
            try
            {
                var manager = ThemeManager.Instance;
                if (manager?.CurrentTheme?.Window != null)
                {
                    var colorHex = manager.CurrentTheme.Window.TitleBarColor;
                    // Parse hex color and convert to BGR format
                    return ParseHexColorToBGR(colorHex);
                }
            }
            catch
            {
                // ThemeManager not initialized
            }
            return 0x00663300; // Default dark blue
        }

        /// <summary>
        /// Gets the border color from the current theme, or returns default
        /// </summary>
        private static int GetThemeBorderColor()
        {
            try
            {
                var manager = ThemeManager.Instance;
                if (manager?.CurrentTheme?.Window != null)
                {
                    var colorHex = manager.CurrentTheme.Window.BorderColor;
                    return ParseHexColorToBGR(colorHex);
                }
            }
            catch
            {
                // ThemeManager not initialized
            }
            return 0x00663300; // Default dark blue
        }

        /// <summary>
        /// Parses a hex color string (e.g., "#00663300" or "#663300") and returns BGR integer
        /// </summary>
        private static int ParseHexColorToBGR(string hexColor)
        {
            try
            {
                // Remove # if present
                hexColor = hexColor.TrimStart('#');

                // If it's already in ARGB/BGR format (8 digits), parse directly
                if (hexColor.Length == 8)
                {
                    return Convert.ToInt32(hexColor, 16);
                }

                // If it's RGB format (6 digits), convert to BGR with alpha
                if (hexColor.Length == 6)
                {
                    int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                    int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                    int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);
                    // Return in BGR format with alpha channel
                    return (b << 16) | (g << 8) | r;
                }
            }
            catch
            {
                // Failed to parse
            }
            return 0x00663300; // Default
        }

        /// <summary>
        /// Applies rounded corners to a form using GDI32 region.
        /// Note: This requires FormBorderStyle.None for full effect.
        /// </summary>
        /// <param name="form">The form to style</param>
        /// <param name="radius">Corner radius in pixels</param>
        public static void ApplyRoundedCorners(Form form, int radius)
        {
            try
            {
                IntPtr hRgn = CreateRoundRectRgn(0, 0, form.Width, form.Height, radius, radius);
                form.Region = Region.FromHrgn(hRgn);
                Debug.WriteLine($"[WindowStyleManager] Rounded corners applied (radius: {radius}px)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowStyleManager] Failed to apply rounded corners: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies comprehensive window styling including dark title bar and optional rounded corners.
        /// </summary>
        /// <param name="form">The form to style</param>
        /// <param name="darkTitleBar">Whether to apply dark title bar</param>
        /// <param name="cornerRadius">Optional corner radius for rounded corners</param>
        /// <param name="captionColor">Optional caption color in BGR format</param>
        /// <param name="borderColor">Optional border color in BGR format</param>
        public static void ApplyWindowStyle(
            Form form,
            bool darkTitleBar = false,
            int? cornerRadius = null,
            int? captionColor = null,
            int? borderColor = null)
        {
            if (darkTitleBar)
            {
                ApplyDarkTitleBar(form, captionColor, borderColor);
            }

            if (cornerRadius.HasValue)
            {
                ApplyRoundedCorners(form, cornerRadius.Value);
            }
        }

        /// <summary>
        /// Applies standard dark theme styling to a form.
        /// This includes background color, opacity, and rounded corners.
        /// </summary>
        /// <param name="form">The form to style</param>
        /// <param name="applyRoundedCorners">Whether to apply rounded corners (default: true)</param>
        public static void ApplyDarkTheme(Form form, bool applyRoundedCorners = true)
        {
            form.BackColor = DarkBackgroundColor;
            form.Opacity = DefaultOpacity;

            if (applyRoundedCorners && form.FormBorderStyle == FormBorderStyle.None)
            {
                ApplyRoundedCorners(form, DefaultCornerRadius);
            }
        }

        /// <summary>
        /// Applies dark theme styling to a RichTextBox control.
        /// Handles the common issues with RichTextBox not respecting dark theme colors properly.
        /// </summary>
        /// <param name="richTextBox">The RichTextBox to style</param>
        public static void ApplyDarkThemeToRichTextBox(RichTextBox richTextBox)
        {
            richTextBox.BackColor = DarkControlBackgroundColor;
            richTextBox.ForeColor = LightForegroundColor;
            richTextBox.SelectionBackColor = DarkControlBackgroundColor;
            richTextBox.DetectUrls = false; // Disable URL detection which can add unwanted styling

            // Ensure BackColor stays consistent - RichTextBox can have issues
            richTextBox.BackColorChanged += (s, e) =>
            {
                if (richTextBox.BackColor != DarkControlBackgroundColor)
                {
                    richTextBox.BackColor = DarkControlBackgroundColor;
                }
            };
        }

        /// <summary>
        /// Applies explicit text colors to all text in a RichTextBox.
        /// Call this after setting .Text property to ensure colors are applied.
        /// </summary>
        /// <param name="richTextBox">The RichTextBox with text to style</param>
        public static void ApplyTextColorsToRichTextBox(RichTextBox richTextBox)
        {
            if (richTextBox.TextLength > 0)
            {
                int currentSelection = richTextBox.SelectionStart;
                int currentLength = richTextBox.SelectionLength;

                richTextBox.SelectAll();
                richTextBox.SelectionColor = LightForegroundColor;
                richTextBox.SelectionBackColor = DarkControlBackgroundColor;
                richTextBox.Select(currentSelection, currentLength); // Restore selection
            }
        }

        #endregion
    }
}
