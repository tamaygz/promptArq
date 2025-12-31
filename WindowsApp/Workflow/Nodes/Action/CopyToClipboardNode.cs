using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Core.Services;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that copies text to the system clipboard using IClipboardService.
    /// </summary>
    public class CopyToClipboardNode : ActionNodeBase
    {
        private readonly IClipboardService? _clipboardService;
        public override string Name => "Copy to Clipboard";

        private string _contentKey = "content";

        public CopyToClipboardNode(IServiceProvider services) : base(services)
        {
            _clipboardService = services.GetService<IClipboardService>();
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

                // Use IClipboardService if available, fallback to direct Clipboard
                if (_clipboardService != null)
                {
                    _clipboardService.SetText(content);
                }
                else
                {
                    // Fallback for backward compatibility
                    System.Windows.Forms.Clipboard.SetText(content);
                }
                
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
