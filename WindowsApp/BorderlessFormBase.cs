using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    /// <summary>
    /// Base class for borderless forms with custom window chrome (dragging, resizing, and border painting).
    /// </summary>
    public class BorderlessFormBase : Form
    {
        private const int ResizeBorderWidth = 8;

        // DWM API for disabling window rendering
        private const int DWMWA_NCRENDERING_POLICY = 2;
        private const int DWMNCRP_DISABLED = 1;
        
        // Windows styles to remove
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_SYSMENU = 0x00080000;
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        private const int GWL_STYLE = -16;

        protected BorderlessFormBase()
        {
            // Enable double buffering to prevent flicker
            SetStyle(ControlStyles.ResizeRedraw | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            UpdateStyles();
            
            // Subscribe to theme changes to repaint border and reapply rounded corners
            ThemeManager.Instance.ThemeChanged += (s, e) => 
            {
                Invalidate();
                ApplyThemeRoundedCorners();
            };
        }
        
        private void ApplyThemeRoundedCorners()
        {
            if (IsHandleCreated && FormBorderStyle == FormBorderStyle.None)
            {
                var theme = ThemeManager.Instance.CurrentTheme;
                if (theme?.Window?.CornerRadius > 0)
                {
                    ThemeApplicator.ApplyRoundedCorners(this, theme.Window.CornerRadius);
                }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            
            // Disable DWM non-client area rendering to prevent blue gradient border
            if (FormBorderStyle == FormBorderStyle.None)
            {
                try
                {
                    // Remove window styles that cause DWM to render borders
                    int style = GetWindowLong(Handle, GWL_STYLE);
                    style &= ~(WS_CAPTION | WS_THICKFRAME | WS_SYSMENU);
                    SetWindowLong(Handle, GWL_STYLE, style);
                    
                    // Disable DWM rendering policy
                    int policy = DWMNCRP_DISABLED;
                    DwmSetWindowAttribute(Handle, DWMWA_NCRENDERING_POLICY, ref policy, sizeof(int));
                }
                catch
                {
                    // DWM not available or call failed
                }
            }
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Apply rounded corners after form is shown
            // Use BeginInvoke to ensure it runs after all initialization is complete
            BeginInvoke(new Action(() => ApplyThemeRoundedCorners()));
        }

        /// <summary>
        /// Draws the border using the current theme's border color.
        /// Can be overridden to customize border appearance.
        /// </summary>
        protected virtual void DrawBorder(Graphics graphics)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var borderColor = ThemeApplicator.ParseColor(theme.Colors.Border);
            using (var pen = new Pen(borderColor, 2))
            {
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
        
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw border AFTER all children have painted
            // Border width must match the WebView2 margin (8px) to fill entire area
            var theme = ThemeManager.Instance.CurrentTheme;
            if (theme?.Colors?.Border != null)
            {
                var borderColor = ThemeApplicator.ParseColor(theme.Colors.Border);
                
                // Use ControlPaint with 8px border to match WebView2 margin
                // This prevents form background from showing through
                Rectangle borderRect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                ControlPaint.DrawBorder(e.Graphics, borderRect, 
                    borderColor, 8, ButtonBorderStyle.Solid,
                    borderColor, 8, ButtonBorderStyle.Solid,
                    borderColor, 8, ButtonBorderStyle.Solid,
                    borderColor, 8, ButtonBorderStyle.Solid);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int WM_NCPAINT = 0x0085;
            const int WM_NCCALCSIZE = 0x0083;
            const int WM_NCACTIVATE = 0x0086;
            const int WM_SETTEXT = 0x000C;
            const int WM_SETICON = 0x0080;

            switch (m.Msg)
            {
                case WM_NCCALCSIZE:
                    // Return 0 to indicate no non-client area
                    // This prevents Windows from drawing any border
                    if (m.WParam != IntPtr.Zero)
                    {
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;
                
                case WM_NCACTIVATE:
                    // Prevent Windows from drawing the default active/inactive border
                    // Return 1 to allow the message to proceed but don't let Windows paint
                    m.Result = (IntPtr)1;
                    return;

                case WM_NCPAINT:
                    // Don't call base - completely override NC painting
                    PaintBorder();
                    m.Result = (IntPtr)1;
                    return;
                
                case WM_SETTEXT:
                case WM_SETICON:
                    // Prevent default processing which can trigger NC area redraw
                    DefWndProc(ref m);
                    // Invalidate to repaint our custom border
                    PaintBorder();
                    return;

                case WM_NCHITTEST:
                    HandleHitTest(ref m);
                    return;
            }

            base.WndProc(ref m);
        }

        private void PaintBorder()
        {
            // Get window DC for border painting
            var hwnd = Handle;
            var hdc = GetWindowDC(hwnd);
            
            try
            {
                using (var g = Graphics.FromHdc(hdc))
                {
                    // Get border color from theme
                    var theme = ThemeManager.Instance.CurrentTheme;
                    Color borderColor;
                    
                    if (theme?.Colors?.Border != null)
                    {
                        // Use ThemeApplicator to parse color correctly
                        borderColor = ThemeApplicator.ParseColor(theme.Colors.Border);
                    }
                    else
                    {
                        // Fallback to Dracula theme border color
                        borderColor = Color.FromArgb(98, 114, 164); // #6272A4
                    }
                    
                    // Set high-quality rendering
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                    
                    using (var pen = new Pen(borderColor, 2))
                    {
                        // Draw border with 2px width - draw at 0,0 to cover full window edge
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
            finally
            {
                ReleaseDC(hwnd, hdc);
            }
        }

        private void HandleHitTest(ref Message m)
        {
            // Extract screen coordinates from lParam
            int x = unchecked((short)(long)m.LParam);
            int y = unchecked((short)((long)m.LParam >> 16));
            Point screenPoint = new Point(x, y);
            Point clientPoint = PointToClient(screenPoint);

            // Check if in resize border area (using client coordinates)
            bool left = clientPoint.X < ResizeBorderWidth;
            bool right = clientPoint.X > ClientSize.Width - ResizeBorderWidth;
            bool top = clientPoint.Y < ResizeBorderWidth;
            bool bottom = clientPoint.Y > ClientSize.Height - ResizeBorderWidth;

            // Return appropriate hit test code
            if (top && left)
                m.Result = (IntPtr)13; // HTTOPLEFT
            else if (top && right)
                m.Result = (IntPtr)14; // HTTOPRIGHT
            else if (bottom && left)
                m.Result = (IntPtr)16; // HTBOTTOMLEFT
            else if (bottom && right)
                m.Result = (IntPtr)17; // HTBOTTOMRIGHT
            else if (top)
                m.Result = (IntPtr)12; // HTTOP
            else if (bottom)
                m.Result = (IntPtr)15; // HTBOTTOM
            else if (left)
                m.Result = (IntPtr)10; // HTLEFT
            else if (right)
                m.Result = (IntPtr)11; // HTRIGHT
            else if (IsInDraggableArea(clientPoint))
                m.Result = (IntPtr)2; // HTCAPTION - draggable
            else
                m.Result = (IntPtr)1; // HTCLIENT - normal client area
        }

        /// <summary>
        /// Determines if a point is in a draggable area.
        /// Override this to customize which areas can drag the window.
        /// Default: disabled to avoid conflict with resize borders.
        /// </summary>
        protected virtual bool IsInDraggableArea(Point clientPoint)
        {
            // Disabled by default - forms can override to enable dragging from specific areas
            return false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Don't add CS_DROPSHADOW as it conflicts with custom border painting
                // and causes DWM to draw a blue gradient border
                return cp;
            }
        }
    }
}
