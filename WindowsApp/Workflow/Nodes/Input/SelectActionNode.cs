using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Input
{
    /// <summary>
    /// Node that displays available actions for a selected prompt.
    /// Actions include Fill Placeholders, Execute, Copy, Paste, Open in Editor, etc.
    /// </summary>
    public class SelectActionNode : InputNodeBase
    {
        public override string Name => "Select Action";
        public override NodeUIType UIType => NodeUIType.ItemList;
        public override string HintText => "Select an action  |  Press ESC or Backspace to go back";

        private List<PromptAction> _actions = new();

        public SelectActionNode(IServiceProvider services) : base(services)
        {
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            var selectedPrompt = context.GetOrDefault<PromptInfo>("selectedPrompt", null);
            if (selectedPrompt == null)
            {
                return Task.FromResult(WorkflowResult.CreateError(context, "No prompt selected"));
            }

            // Build actions based on prompt properties
            _actions = BuildActionsForPrompt(selectedPrompt);
            context.Set("availableActions", _actions);

            // If an action was selected, store it
            if (context.Has("selectedItem"))
            {
                var selectedItem = context.Get<object>("selectedItem");
                if (selectedItem is PromptAction action)
                {
                    context.Set("selectedAction", action);
                    return Task.FromResult(WorkflowResult.CreateSuccess(context));
                }
            }

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        private List<PromptAction> BuildActionsForPrompt(PromptInfo prompt)
        {
            var actions = new List<PromptAction>();

            // If prompt has placeholders, add "Fill Placeholders" as first option
            if (prompt.HasPlaceholders)
            {
                actions.Add(new PromptAction
                {
                    Type = PromptActionType.FillPlaceholders,
                    Name = "Fill Placeholders",
                    Description = "Fill in template variables",
                    Icon = "📝",
                    IsEnabled = true
                });
            }

            // Add execute/paste/copy actions based on execute_llm flag
            if (prompt.ExecuteLLM)
            {
                actions.Add(new PromptAction
                {
                    Type = PromptActionType.Paste,
                    Name = "Execute & Paste",
                    Description = "Execute through LLM and paste",
                    Icon = "📋",
                    IsEnabled = true
                });
                actions.Add(new PromptAction
                {
                    Type = PromptActionType.Copy,
                    Name = "Execute & Copy",
                    Description = "Execute through LLM and copy",
                    Icon = "📎",
                    IsEnabled = true
                });
            }
            else
            {
                actions.Add(new PromptAction
                {
                    Type = PromptActionType.Paste,
                    Name = "Paste",
                    Description = "Paste to current focus",
                    Icon = "📋",
                    IsEnabled = true
                });
                actions.Add(new PromptAction
                {
                    Type = PromptActionType.Copy,
                    Name = "Copy to Clipboard",
                    Description = "Copy prompt content",
                    Icon = "📎",
                    IsEnabled = true
                });
            }

            // Add open in editor action
            actions.Add(new PromptAction
            {
                Type = PromptActionType.OpenInEditor,
                Name = "Open in Editor",
                Description = "Edit this prompt",
                Icon = "✏️",
                IsEnabled = true
            });

            return actions;
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            return _actions;
        }

        public override string GetDisplayText(object item)
        {
            if (item is PromptAction action)
                return action.Name;
            return item?.ToString() ?? "";
        }

        public override string GetSecondaryText(object item)
        {
            if (item is PromptAction action)
                return action.Description;
            return "";
        }

        public override string GetIcon(object item)
        {
            if (item is PromptAction action)
                return action.Icon;
            return "";
        }

        public override Color? GetItemColor(object item)
        {
            return null;
        }
    }
}
