using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PromptArqApp
{
    /// <summary>
    /// Manages window styling including dark title bars and rounded corners.
    /// Consolidates DWM API and GDI32 API functionality for consistent styling across all forms.
    /// </summary>
    public static class WindowStyleManager
    {
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
        /// Default colors: Dark blue (RGB: 0, 51, 102 / BGR: 0x00663300)
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

                // Set caption color (default: dark blue RGB(0, 51, 102) -> BGR 0x00663300)
                int caption = captionColor ?? 0x00663300;
                DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));

                // Set border color (default: same as caption)
                int border = borderColor ?? captionColor ?? 0x00663300;
                DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));

                Debug.WriteLine("[WindowStyleManager] Dark title bar applied successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WindowStyleManager] Failed to set dark title bar: {ex.Message}");
            }
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

        #endregion
    }
}
