using System;
using System.Threading.Tasks;

namespace PromptArqApp.Core.Services;

/// <summary>
/// Service for window management operations.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Gets the handle of the currently focused window.
    /// </summary>
    /// <returns>The window handle.</returns>
    IntPtr GetForegroundWindow();

    /// <summary>
    /// Gets the title of a window by its handle.
    /// </summary>
    /// <param name="windowHandle">The window handle.</param>
    /// <returns>The window title.</returns>
    string GetWindowTitle(IntPtr windowHandle);

    /// <summary>
    /// Sets the specified window as the foreground window.
    /// </summary>
    /// <param name="windowHandle">The window handle.</param>
    /// <returns>True if successful, false otherwise.</returns>
    bool SetForegroundWindow(IntPtr windowHandle);

    /// <summary>
    /// Checks if the specified window is a PromptArq window.
    /// </summary>
    /// <param name="windowHandle">The window handle.</param>
    /// <returns>True if the window is a PromptArq window, false otherwise.</returns>
    bool IsPromptArqWindow(IntPtr windowHandle);

    /// <summary>
    /// Switches focus to the previous window using Alt+Tab.
    /// </summary>
    /// <param name="delayMs">Delay in milliseconds after switching.</param>
    /// <returns>Task representing the operation.</returns>
    Task SwitchToPreviousWindowAsync(int delayMs = 200);

    /// <summary>
    /// Ensures that no PromptArq window has focus. If one does, switches to the previous window.
    /// </summary>
    /// <returns>The PromptArq window handle if it had focus, otherwise IntPtr.Zero.</returns>
    Task<IntPtr> EnsurePromptArqWindowNotFocusedAsync();

    /// <summary>
    /// Restores focus to a PromptArq window if it was previously saved.
    /// </summary>
    /// <param name="windowHandle">The window handle to restore.</param>
    /// <param name="delayMs">Delay in milliseconds before restoring focus.</param>
    /// <returns>Task representing the operation.</returns>
    Task RestorePromptArqFocusAsync(IntPtr windowHandle, int delayMs = 100);

    /// <summary>
    /// Gets the last focused window handle before PromptArq was activated.
    /// </summary>
    IntPtr LastFocusWindowHandle { get; }

    /// <summary>
    /// Refreshes and stores the currently focused window as the last focus window.
    /// Should be called before showing PromptArq dialogs to remember which window to return to.
    /// </summary>
    void RefreshLastFocus();

    /// <summary>
    /// Sets the foreground window to the stored last focus window.
    /// </summary>
    /// <returns>True if successful and a last focus window was stored, false otherwise.</returns>
    bool SetForegroundLastFocus();
}
