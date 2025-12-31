using System;
using System.Collections.Generic;
using System.Linq;
using PromptArqApp.Core.Capabilities;

namespace PromptArqApp.Capabilities;

/// <summary>
/// Provides prompt-related capabilities for the command palette
/// </summary>
public class PromptCapabilitiesProvider : ICapabilityProvider
{
    private readonly Func<List<PromptInfo>>? _getPrompts;

    public string Name => "Prompts";

    public PromptCapabilitiesProvider(Func<List<PromptInfo>>? getPrompts = null)
    {
        _getPrompts = getPrompts;
    }

    public IEnumerable<CapabilityInfo> GetCapabilities()
    {
        return new[]
        {
            new CapabilityInfo
            {
                Id = "search-prompts",
                Name = "Search Prompts",
                Description = "Search and execute your saved prompts",
                Category = CapabilityCategory.Prompts,
                Icon = "📝",
                Keywords = new[] { "prompt", "search", "find", "execute" },
                WorkflowId = "quick-copy",
                Priority = 100
            },
            new CapabilityInfo
            {
                Id = "one-time-prompt",
                Name = "Co-Author One Time Prompt",
                Description = "Create and execute a one-time prompt with system and user input",
                Category = CapabilityCategory.Prompts,
                Icon = "✨",
                Keywords = new[] { "one", "time", "single", "co-author", "generate" },
                WorkflowId = "one-time-prompt",
                Priority = 90
            },
            new CapabilityInfo
            {
                Id = "fill-placeholders",
                Name = "Fill Placeholders",
                Description = "Search prompts and fill in placeholder values",
                Category = CapabilityCategory.Prompts,
                Icon = "📋",
                Keywords = new[] { "fill", "placeholder", "variable", "template" },
                WorkflowId = "fill-placeholders",
                Priority = 85
            },
            new CapabilityInfo
            {
                Id = "quick-paste",
                Name = "Quick Paste",
                Description = "Search and quickly paste prompt output to active window",
                Category = CapabilityCategory.Prompts,
                Icon = "📎",
                Keywords = new[] { "paste", "insert", "quick" },
                WorkflowId = "quick-paste",
                Priority = 80
            }
        };
    }

    public bool CanHandle(string query)
    {
        // Can provide dynamic prompt results if prompts are available
        return _getPrompts != null &&
               !string.IsNullOrWhiteSpace(query) &&
               query.Length >= 2; // Require at least 2 characters
    }

    public IEnumerable<CapabilityInfo> GetDynamicCapabilities(string query)
    {
        if (_getPrompts == null || string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<CapabilityInfo>();
        }

        var prompts = _getPrompts();
        var lowerQuery = query.ToLowerInvariant();

        return prompts
            .Where(p =>
                p.Title.ToLowerInvariant().Contains(lowerQuery) ||
                p.Description?.ToLowerInvariant().Contains(lowerQuery) == true ||
                p.Content?.ToLowerInvariant().Contains(lowerQuery) == true)
            .Take(10) // Limit dynamic results
            .Select(p => new CapabilityInfo
            {
                Id = $"prompt-{p.Id}",
                Name = p.Title,
                Description = p.Description ?? "Execute this prompt",
                Category = CapabilityCategory.Prompts,
                Icon = "📄",
                Keywords = new[] { "prompt", p.Title.ToLowerInvariant() },
                WorkflowId = "quick-copy",
                Priority = 70,
                Metadata = new Dictionary<string, object>
                {
                    ["PromptId"] = p.Id,
                    ["PromptInfo"] = p
                }
            });
    }
}
