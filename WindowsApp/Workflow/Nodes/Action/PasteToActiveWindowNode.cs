using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that pastes text to the active window using SendKeys.
    /// First copies to clipboard, then sends Ctrl+V.
    /// </summary>
    public class PasteToActiveWindowNode : ActionNodeBase
    {
        public override string Name => "Paste to Active Window";

        private string _contentKey = "content";
        private int _delayMs = 300;

        public PasteToActiveWindowNode(IServiceProvider services) : base(services)
        {
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("contentKey", out var key))
            {
                _contentKey = key.ToString() ?? "content";
            }
            if (config.TryGetValue("delayMs", out var delay))
            {
                _delayMs = Convert.ToInt32(delay);
            }
        }

        protected override async Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
        {
            try
            {
                // Determine what content to paste
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
                    return WorkflowResult.CreateError(context, "No content to paste");
                }

                if (string.IsNullOrEmpty(content))
                {
                    return WorkflowResult.CreateError(context, "Content is empty");
                }

                // Copy to clipboard first
                Clipboard.SetText(content);
                
                // Wait a bit for the form to close and focus to return to previous window
                await Task.Delay(_delayMs);
                
                // Send Ctrl+V to paste
                SendKeys.SendWait("^v");
                
                // Store action for notification
                context.Set("lastAction", "pasted");
                
                return WorkflowResult.CreateSuccess(context);
            }
            catch (Exception ex)
            {
                return WorkflowResult.CreateError(context, $"Error pasting: {ex.Message}");
            }
        }
    }
}
