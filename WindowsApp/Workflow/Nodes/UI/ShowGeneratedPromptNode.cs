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
    public class ShowGeneratedPromptNode : UINodeBase, INodeTextProvider
    {
        public override string Name => "Show Generated Prompt";
        public override NodeUIType UIType => NodeUIType.TextDisplay;
        public override string HintText => "Generated prompt preview  |  Select action";

        public ShowGeneratedPromptNode(IServiceProvider services) : base(services)
        {
        }

        public string GetTextContent(WorkflowContext context)
        {
            var content = context.GetOrDefault<string>("generatedPrompt", "No generated prompt available");
            System.Diagnostics.Debug.WriteLine($"[ShowGeneratedPromptNode] GetTextContent - returning {content?.Length ?? 0} characters");
            return content;
        }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            var generatedPrompt = context.GetOrDefault<string>("generatedPrompt", "");
            System.Diagnostics.Debug.WriteLine($"[ShowGeneratedPromptNode] ExecuteAsync - generatedPrompt length: {generatedPrompt?.Length ?? 0}");
            
            if (string.IsNullOrWhiteSpace(generatedPrompt))
            {
                System.Diagnostics.Debug.WriteLine("[ShowGeneratedPromptNode] No generated prompt available");
                return Task.FromResult(WorkflowResult.CreateError(context, "No generated prompt available"));
            }

            // If an action was selected, store it and map to branch key
            if (context.Has("selectedItem"))
            {
                System.Diagnostics.Debug.WriteLine("[ShowGeneratedPromptNode] selectedItem found in context");
                var selectedItem = context.Get<object>("selectedItem");
                if (selectedItem is PromptAction action)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShowGeneratedPromptNode] Action selected: {action.Name}, Type: {action.Type}");
                    context.Set("selectedAction", action);
                    
                    // Map action type to branch key for ConditionalNode
                    string branchKey = action.Type switch
                    {
                        PromptActionType.ExecuteOneTimePrompt => "Execute",
                        PromptActionType.Copy => "Copy",
                        PromptActionType.EditGeneratedPrompt => "Edit",
                        _ => ""
                    };
                    System.Diagnostics.Debug.WriteLine($"[ShowGeneratedPromptNode] Mapped to branch key: {branchKey}");
                    context.Set("promptAction", branchKey);
                    
                    return Task.FromResult(WorkflowResult.CreateSuccess(context));
                }
            }

            System.Diagnostics.Debug.WriteLine("[ShowGeneratedPromptNode] No action selected, returning success without advancing");
            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        public override IEnumerable<object> GetItems(WorkflowContext context)
        {
            // Show available actions (preview is shown in TextDisplayPanel)
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
