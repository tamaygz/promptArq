using System;
using System.Threading.Tasks;

namespace PromptArqApp.Core.Services;

/// <summary>
/// Service for system-level operations
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// Shutdown the computer
    /// </summary>
    /// <param name="force">Whether to force shutdown</param>
    /// <returns>Task representing the operation</returns>
    Task ShutdownAsync(bool force = false);
    
    /// <summary>
    /// Restart the computer
    /// </summary>
    /// <param name="force">Whether to force restart</param>
    /// <returns>Task representing the operation</returns>
    Task RestartAsync(bool force = false);
    
    /// <summary>
    /// Lock the computer
    /// </summary>
    void LockWorkstation();
    
    /// <summary>
    /// Put the computer to sleep
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task SleepAsync();
    
    /// <summary>
    /// Log out the current user
    /// </summary>
    void Logout();
    
    /// <summary>
    /// Open Task Manager
    /// </summary>
    void OpenTaskManager();
    
    /// <summary>
    /// Open Control Panel
    /// </summary>
    void OpenControlPanel();
    
    /// <summary>
    /// Open Windows Settings
    /// </summary>
    void OpenSettings();
    
    /// <summary>
    /// Empty the Recycle Bin
    /// </summary>
    /// <returns>Task representing the operation</returns>
    Task EmptyRecycleBinAsync();
    
    /// <summary>
    /// Get system information
    /// </summary>
    /// <returns>Dictionary of system info key-value pairs</returns>
    Task<System.Collections.Generic.Dictionary<string, string>> GetSystemInfoAsync();
    
    /// <summary>
    /// Check if running with administrator privileges
    /// </summary>
    /// <returns>True if running as administrator</returns>
    bool IsAdministrator();
}
