using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PromptArqApp.Core.Services;

namespace PromptArqApp.Services;

/// <summary>
/// Implementation of window management operations.
/// </summary>
public class WindowService : IWindowService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    IntPtr IWindowService.GetForegroundWindow()
    {
        return GetForegroundWindow();
    }

    public string GetWindowTitle(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return string.Empty;

        StringBuilder windowTitle = new StringBuilder(256);
        GetWindowText(windowHandle, windowTitle, 256);
        return windowTitle.ToString();
    }

    bool IWindowService.SetForegroundWindow(IntPtr windowHandle)
    {
        return SetForegroundWindow(windowHandle);
    }

    public bool IsPromptArqWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
            return false;

        string title = GetWindowTitle(windowHandle);
        return title.Contains("PromptArq", StringComparison.OrdinalIgnoreCase) ||
         title.Contains("PromptArq Settings", StringComparison.OrdinalIgnoreCase) ||
         title.Contains("PromptArqToast", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("Command Palette", StringComparison.OrdinalIgnoreCase) ||
                title.Contains("CommandPaletteForm", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SwitchToPreviousWindowAsync(int delayMs = 200)
    {
        SendKeys.SendWait("%{TAB}");
        await Task.Delay(delayMs);
    }

    public async Task<IntPtr> EnsurePromptArqWindowNotFocusedAsync()
    {
        IntPtr foregroundWindow = GetForegroundWindow();

        if (foregroundWindow != IntPtr.Zero && IsPromptArqWindow(foregroundWindow))
        {
            await SwitchToPreviousWindowAsync();
            return foregroundWindow;
        }

        return IntPtr.Zero;
    }

    public async Task RestorePromptArqFocusAsync(IntPtr windowHandle, int delayMs = 100)
    {
        if (windowHandle != IntPtr.Zero)
        {
            await Task.Delay(delayMs);
            SetForegroundWindow(windowHandle);
        }
    }
}
