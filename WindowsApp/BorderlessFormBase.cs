using System;
using System.Drawing;
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

        protected BorderlessFormBase()
        {
            // Enable double buffering to prevent flicker
            SetStyle(ControlStyles.ResizeRedraw | 
                     ControlStyles.OptimizedDoubleBuffer | 
                     ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        /// <summary>
        /// Draws the border using the current theme's border color.
        /// Can be overridden to customize border appearance.
        /// </summary>
        protected virtual void DrawBorder(Graphics graphics)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var borderColor = ThemeApplicator.ParseColor(theme.Colors.Border);
            using (var pen = new Pen(borderColor, 1))
            {
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int WM_NCCALCSIZE = 0x0083;
            const int WM_NCPAINT = 0x0085;

            switch (m.Msg)
            {
                case WM_NCCALCSIZE:
                    // Handle non-client area calculation to reserve space for resize borders
                    if (m.WParam != IntPtr.Zero && FormBorderStyle == FormBorderStyle.None)
                    {
                        // Shrink client area to create non-client border for resizing
                        var ncParams = (NCCALCSIZE_PARAMS)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS))!;
                        
                        // Reserve space for resize borders
                        ncParams.rect0.Top += ResizeBorderWidth;
                        ncParams.rect0.Left += ResizeBorderWidth;
                        ncParams.rect0.Right -= ResizeBorderWidth;
                        ncParams.rect0.Bottom -= ResizeBorderWidth;
                        
                        System.Runtime.InteropServices.Marshal.StructureToPtr(ncParams, m.LParam, false);
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;

                case WM_NCHITTEST:
                    HandleHitTest(ref m);
                    return;

                case WM_NCPAINT:
                    // Custom paint non-client area
                    base.WndProc(ref m);
                    PaintNonClientArea();
                    return;
            }

            base.WndProc(ref m);
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS
        {
            public RECT rect0;
            public RECT rect1;
            public RECT rect2;
            public IntPtr lppos;
        }

        private void PaintNonClientArea()
        {
            // Get window DC for non-client area painting
            var hwnd = Handle;
            var hdc = GetWindowDC(hwnd);
            
            try
            {
                using (var g = Graphics.FromHdc(hdc))
                {
                    var theme = ThemeManager.Instance.CurrentTheme;
                    var borderColor = ThemeApplicator.ParseColor(theme.Colors.Border);
                    using (var pen = new Pen(borderColor, 1))
                    {
                        // Draw border in non-client area
                        var rect = new Rectangle(0, 0, Width, Height);
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                    }
                }
            }
            finally
            {
                ReleaseDC(hwnd, hdc);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private void HandleHitTest(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Result == (IntPtr)1) // HTCLIENT
            {
                var screenPoint = new Point(
                    unchecked((short)(long)m.LParam),
                    unchecked((short)((long)m.LParam >> 16))
                );
                var clientPoint = PointToClient(screenPoint);

                // Check if we're in a resize border area
                bool left = clientPoint.X < ResizeBorderWidth;
                bool right = clientPoint.X > Width - ResizeBorderWidth;
                bool top = clientPoint.Y < ResizeBorderWidth;
                bool bottom = clientPoint.Y > Height - ResizeBorderWidth;

                // Determine hit test result based on position
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
            }
        }

        /// <summary>
        /// Determines if a point is in a draggable area.
        /// Override this to customize which areas can drag the window.
        /// Default: entire client area except resize borders.
        /// </summary>
        protected virtual bool IsInDraggableArea(Point clientPoint)
        {
            // By default, the entire client area (except resize borders) is draggable
            return true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // Add drop shadow for borderless window
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}
