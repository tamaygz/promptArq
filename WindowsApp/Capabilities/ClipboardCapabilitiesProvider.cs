using System;
using System.Collections.Generic;
using PromptArqApp.Core.Capabilities;

namespace PromptArqApp.Capabilities;

/// <summary>
/// Provides clipboard-related capabilities for the command palette
/// </summary>
public class ClipboardCapabilitiesProvider : ICapabilityProvider
{
    public string Name => "Clipboard";

    public IEnumerable<CapabilityInfo> GetCapabilities()
    {
        return new[]
        {
            new CapabilityInfo
            {
                Id = "clipboard-history",
                Name = "Clipboard History",
                Description = "View and select from clipboard history",
                Category = CapabilityCategory.Clipboard,
                Icon = "📋",
                Keywords = new[] { "clipboard", "history", "copy", "paste", "clip" },
                ActionId = "show-clipboard-history",
                Priority = 75
            },
            new CapabilityInfo
            {
                Id = "clear-clipboard",
                Name = "Clear Clipboard",
                Description = "Clear the current clipboard content",
                Category = CapabilityCategory.Clipboard,
                Icon = "🗑️",
                Keywords = new[] { "clear", "clipboard", "empty", "delete" },
                ActionId = "clear-clipboard",
                Priority = 50
            },
            new CapabilityInfo
            {
                Id = "clipboard-manager",
                Name = "Clipboard Manager",
                Description = "Manage clipboard history and settings",
                Category = CapabilityCategory.Clipboard,
                Icon = "⚙️",
                Keywords = new[] { "clipboard", "manager", "settings", "configure" },
                ActionId = "open-clipboard-manager",
                Priority = 40
            }
        };
    }

    public bool CanHandle(string query)
    {
        // Basic check for clipboard-related queries
        if (string.IsNullOrWhiteSpace(query))
            return false;

        var lowerQuery = query.ToLowerInvariant();
        return lowerQuery.Contains("clip") ||
               lowerQuery.Contains("copy") ||
               lowerQuery.Contains("paste") ||
               lowerQuery.Contains("history");
    }
}
