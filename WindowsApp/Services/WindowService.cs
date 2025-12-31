using System;
using System.Diagnostics;
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private IntPtr _lastFocusWindowHandle = IntPtr.Zero;
    private readonly int _currentProcessId;

    public WindowService()
    {
        _currentProcessId = Process.GetCurrentProcess().Id;
    }

    public IntPtr LastFocusWindowHandle => _lastFocusWindowHandle;

    IntPtr IWindowService.GetForegroundWindow()
    {
        return GetForegroundWindow();
    }

    public void RefreshLastFocus()
    {
        IntPtr currentWindow = GetForegroundWindow();
        
        // Only store the window if it's not a PromptArq window
        if (currentWindow != IntPtr.Zero && !IsPromptArqWindow(currentWindow))
        {
            _lastFocusWindowHandle = currentWindow;
        }
    }

    public bool SetForegroundLastFocus()
    {
        if (_lastFocusWindowHandle != IntPtr.Zero)
        {
            return SetForegroundWindow(_lastFocusWindowHandle);
        }
        return false;
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

        // Primary method: Check if window belongs to current process
        uint windowProcessId;
        GetWindowThreadProcessId(windowHandle, out windowProcessId);

        return (windowProcessId == _currentProcessId);

        // if (windowProcessId == _currentProcessId)
        //     return true;

        // // Fallback: Check window title (for edge cases or debugging)
        // string title = GetWindowTitle(windowHandle);
        // return title.Contains("PromptArq", StringComparison.OrdinalIgnoreCase) ||
        //        title.Contains("CommandPalette", StringComparison.OrdinalIgnoreCase) ||
        //        title.Contains("Command Palette", StringComparison.OrdinalIgnoreCase);
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
