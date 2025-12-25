using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that copies text to the system clipboard.
    /// </summary>
    public class CopyToClipboardNode : ActionNodeBase
    {
        public override string Name => "Copy to Clipboard";

        private string _contentKey = "content";

        public CopyToClipboardNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("contentKey", out var key))
            {
                _contentKey = key.ToString() ?? "content";
            }
        }

        protected override Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            try
            {
                // Determine what content to copy
                string content;
                
                // First check if there's filled content (from placeholder workflow)
                if (context.Has("filledContent"))
                {
                    content = context.Get<string>("filledContent");
                }
                // Otherwise check for content from selected prompt
                else if (context.Has("selectedPrompt"))
                {
                    var prompt = context.Get<PromptInfo>("selectedPrompt");
                    content = prompt.Content;
                }
                // Otherwise use the specified content key
                else if (context.Has(_contentKey))
                {
                    content = context.Get<string>(_contentKey);
                }
                else
                {
                    return Task.FromResult(WorkflowResult.CreateError(context, "No content to copy"));
                }

                if (string.IsNullOrEmpty(content))
                {
                    return Task.FromResult(WorkflowResult.CreateError(context, "Content is empty"));
                }

                // Copy to clipboard
                Clipboard.SetText(content);
                
                // Store action for notification
                context.Set("lastAction", "copied");
                
                return Task.FromResult(WorkflowResult.CreateSuccess(context));
            }
            catch (Exception ex)
            {
                return Task.FromResult(WorkflowResult.CreateError(context, $"Error copying to clipboard: {ex.Message}"));
            }
        }
    }
}
