using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.UI
{
    /// <summary>
    /// Node that displays the generated prompt preview with available actions.
    /// </summary>
    public class ShowGeneratedPromptNode : UINodeBase
    {
        public override string Name => "Show Generated Prompt";
        public override NodeUIType UIType => NodeUIType.ItemList;
        public override string HintText => "Preview generated prompt  |  Select action";

        public ShowGeneratedPromptNode(IServiceProvider services) : base(services)
        {
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            var generatedPrompt = context.GetOrDefault<string>("generatedPrompt", "");
            
            if (string.IsNullOrWhiteSpace(generatedPrompt))
            {
                return Task.FromResult(WorkflowResult.CreateError(context, "No generated prompt available"));
            }

            // If an action was selected, store it
            if (context.Has("selectedItem"))
            {
                var selectedItem = context.Get<object>("selectedItem");
                if (selectedItem is PromptAction action)
                {
                    context.Set("selectedAction", action);
                    return Task.FromResult(WorkflowResult.CreateSuccess(context));
                }
                else if (selectedItem is string text && !text.StartsWith("───"))
                {
                    // User selected the preview text, treat as "Edit" action
                    context.Set("selectedAction", new PromptAction
                    {
                        Type = PromptActionType.EditGeneratedPrompt,
                        Name = "Edit Prompt"
                    });
                    return Task.FromResult(WorkflowResult.CreateSuccess(context));
                }
            }

            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            var generatedPrompt = context.GetOrDefault<string>("generatedPrompt", "");

            // Show preview of generated prompt
            yield return "─────── Generated Prompt Preview ───────";
            
            // Show first few lines as preview
            var lines = generatedPrompt.Split('\n');
            var previewLines = lines.Length > 5 ? 5 : lines.Length;
            for (int i = 0; i < previewLines; i++)
            {
                yield return lines[i];
            }
            if (lines.Length > 5)
            {
                yield return $"... ({lines.Length - 5} more lines)";
            }

            yield return "─────── Actions ───────";

            // Show available actions
            yield return new PromptAction
            {
                Type = PromptActionType.ExecuteOneTimePrompt,
                Name = "Execute Prompt",
                Description = "Execute through LLM",
                Icon = "▶️",
                IsEnabled = true
            };

            yield return new PromptAction
            {
                Type = PromptActionType.EditGeneratedPrompt,
                Name = "Edit Prompt",
                Description = "Modify the generated prompt",
                Icon = "✏️",
                IsEnabled = true
            };

            yield return new PromptAction
            {
                Type = PromptActionType.Copy,
                Name = "Copy to Clipboard",
                Description = "Copy without executing",
                Icon = "📎",
                IsEnabled = true
            };
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
