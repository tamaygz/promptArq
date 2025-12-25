using System;
using System.Collections.Generic;
using PromptArqApp.Core.Capabilities;

namespace PromptArqApp.Capabilities;

/// <summary>
/// Provides system command capabilities for the command palette
/// </summary>
public class SystemCapabilitiesProvider : ICapabilityProvider
{
    public string Name => "System";

    public IEnumerable<CapabilityInfo> GetCapabilities()
    {
        return new[]
        {
            new CapabilityInfo
            {
                Id = "system-shutdown",
                Name = "Shutdown",
                Description = "Shutdown the computer",
                Category = CapabilityCategory.System,
                Icon = "⏻",
                Keywords = new[] { "shutdown", "power", "off", "turn off" },
                ActionId = "system-shutdown",
                Priority = 60
            },
            new CapabilityInfo
            {
                Id = "system-restart",
                Name = "Restart",
                Description = "Restart the computer",
                Category = CapabilityCategory.System,
                Icon = "🔄",
                Keywords = new[] { "restart", "reboot", "reset" },
                ActionId = "system-restart",
                Priority = 60
            },
            new CapabilityInfo
            {
                Id = "system-lock",
                Name = "Lock Computer",
                Description = "Lock the computer screen",
                Category = CapabilityCategory.System,
                Icon = "🔒",
                Keywords = new[] { "lock", "screen", "secure" },
                ActionId = "system-lock",
                Priority = 70
            },
            new CapabilityInfo
            {
                Id = "system-sleep",
                Name = "Sleep",
                Description = "Put the computer to sleep",
                Category = CapabilityCategory.System,
                Icon = "💤",
                Keywords = new[] { "sleep", "suspend", "standby" },
                ActionId = "system-sleep",
                Priority = 65
            },
            new CapabilityInfo
            {
                Id = "system-logout",
                Name = "Logout",
                Description = "Log out of the current user session",
                Category = CapabilityCategory.System,
                Icon = "🚪",
                Keywords = new[] { "logout", "sign out", "log out" },
                ActionId = "system-logout",
                Priority = 60
            },
            new CapabilityInfo
            {
                Id = "open-task-manager",
                Name = "Task Manager",
                Description = "Open Windows Task Manager",
                Category = CapabilityCategory.System,
                Icon = "📊",
                Keywords = new[] { "task", "manager", "processes", "performance" },
                ActionId = "open-task-manager",
                Priority = 55
            },
            new CapabilityInfo
            {
                Id = "open-control-panel",
                Name = "Control Panel",
                Description = "Open Windows Control Panel",
                Category = CapabilityCategory.System,
                Icon = "🎛️",
                Keywords = new[] { "control", "panel", "settings", "configure" },
                ActionId = "open-control-panel",
                Priority = 50
            },
            new CapabilityInfo
            {
                Id = "open-settings",
                Name = "Windows Settings",
                Description = "Open Windows Settings",
                Category = CapabilityCategory.System,
                Icon = "⚙️",
                Keywords = new[] { "settings", "preferences", "configure", "options" },
                ActionId = "open-settings",
                Priority = 50
            },
            new CapabilityInfo
            {
                Id = "empty-recycle-bin",
                Name = "Empty Recycle Bin",
                Description = "Empty the Windows Recycle Bin",
                Category = CapabilityCategory.System,
                Icon = "🗑️",
                Keywords = new[] { "recycle", "bin", "empty", "delete", "trash" },
                ActionId = "empty-recycle-bin",
                Priority = 45
            }
        };
    }

    public bool CanHandle(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var lowerQuery = query.ToLowerInvariant();
        return lowerQuery.Contains("shut") ||
               lowerQuery.Contains("restart") ||
               lowerQuery.Contains("lock") ||
               lowerQuery.Contains("sleep") ||
               lowerQuery.Contains("logout") ||
               lowerQuery.Contains("task") ||
               lowerQuery.Contains("control") ||
               lowerQuery.Contains("settings") ||
               lowerQuery.Contains("system");
    }
}
