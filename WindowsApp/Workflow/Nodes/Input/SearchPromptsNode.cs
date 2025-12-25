using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Input
{
    /// <summary>
    /// Node that displays a searchable list of prompts for the user to select from.
    /// Supports filtering by title, description, content, project, category, and tags.
    /// </summary>
    public class SearchPromptsNode : InputNodeBase
    {
        public override string Name => "Search Prompts";
        public override NodeUIType UIType => NodeUIType.ItemList;
        public override string HintText => "Type to search prompts... Press ESC to close";

        private List<PromptInfo> _allPrompts = new();
        private string _searchQuery = "";

        public SearchPromptsNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            // Configuration can include filters, sorting, etc.
            base.Configure(config);
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Get prompts from context or service
            _allPrompts = context.GetOrDefault<List<PromptInfo>>("allPrompts", new List<PromptInfo>());
            
            // Get search query from UI (will be set by CommandPaletteForm)
            _searchQuery = context.GetOrDefault<string>("searchQuery", "");

            // If a prompt was selected, store it in context
            if (context.Has("selectedItem"))
            {
                var selectedItem = context.Get<object>("selectedItem");
                
                // Check if it's the One-Time Prompt action
                if (selectedItem is PromptAction action && action.Type == PromptActionType.CoAuthorOneTimePrompt)
                {
                    context.Set("switchToWorkflow", "one-time-prompt");
                    return Task.FromResult(WorkflowResult.CreateSuccess(context));
                }
                else if (selectedItem is PromptInfo promptInfo)
                {
                    context.Set("selectedPrompt", promptInfo);
                    return Task.FromResult(WorkflowResult.CreateSuccess(context));
                }
            }

            // Otherwise, just return success to show the UI
            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            _allPrompts = context.GetOrDefault<List<PromptInfo>>("allPrompts", new List<PromptInfo>());
            _searchQuery = context.GetOrDefault<string>("searchQuery", "");

            if (string.IsNullOrEmpty(_searchQuery))
            {
                // Show recent prompts first if enabled
                var settings = context.Services.GetService(typeof(AppSettings)) as AppSettings;
                var history = context.Services.GetService(typeof(PromptHistory)) as PromptHistory;

                if (settings?.ShowLastUsedPrompts == true && history != null)
                {
                    var recentPrompts = history.GetRecentPrompts();
                    var recentIds = new HashSet<string>(recentPrompts.Select(p => p.PromptId));

                    // Add "Co-Author One Time Prompt" as first item
                    yield return new PromptAction
                    {
                        Type = PromptActionType.CoAuthorOneTimePrompt,
                        Name = "Co-Author One Time Prompt",
                        Description = "Execute a prompt with AI system guidance",
                        Icon = "✨",
                        IsEnabled = true
                    };

                    // Add recent prompts
                    foreach (var recentEntry in recentPrompts)
                    {
                        var prompt = _allPrompts.FirstOrDefault(p => p.Id == recentEntry.PromptId);
                        if (prompt != null)
                        {
                            yield return prompt;
                        }
                    }

                    // Fill remaining space with other prompts
                    foreach (var prompt in _allPrompts.Where(p => !recentIds.Contains(p.Id)).Take(50 - recentPrompts.Count))
                    {
                        yield return prompt;
                    }
                }
                else
                {
                    // Show first 50 prompts
                    foreach (var prompt in _allPrompts.Take(50))
                    {
                        yield return prompt;
                    }
                }
            }
            else
            {
                // Filter prompts based on search query
                var query = _searchQuery.ToLowerInvariant();
                var filtered = _allPrompts
                    .Where(p =>
                        p.Title.ToLowerInvariant().Contains(query) ||
                        p.Description.ToLowerInvariant().Contains(query) ||
                        p.Content.ToLowerInvariant().Contains(query) ||
                        p.ProjectName.ToLowerInvariant().Contains(query) ||
                        p.CategoryName.ToLowerInvariant().Contains(query) ||
                        p.Tags.Any(t => t.ToLowerInvariant().Contains(query))
                    )
                    .Take(50);

                foreach (var prompt in filtered)
                {
                    yield return prompt;
                }
            }
        }

        public override string GetDisplayText(object item)
        {
            if (item is PromptInfo prompt)
                return prompt.Title;
            if (item is PromptAction action)
                return action.Name;
            return item?.ToString() ?? "";
        }

        public override string GetSecondaryText(object item)
        {
            if (item is PromptInfo prompt)
                return prompt.Description;
            if (item is PromptAction action)
                return action.Description;
            return "";
        }

        public override string GetIcon(object item)
        {
            if (item is PromptInfo prompt)
                return prompt.ProjectName?.Substring(0, Math.Min(3, prompt.ProjectName.Length)).ToUpper() ?? "?";
            if (item is PromptAction action)
                return action.Icon;
            return "";
        }

        public override Color? GetItemColor(object item)
        {
            // Could return different colors based on prompt type, project, etc.
            return null;
        }
    }
}
