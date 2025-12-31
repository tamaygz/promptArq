using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Core.Services;
using Serilog.Core;
using Serilog;

namespace PromptArqApp.Workflow.Nodes.Action
{
    /// <summary>
    /// Node that pastes text to the active window using SendKeys.
    /// Uses IClipboardService and IWindowService, then sends Ctrl+V.
    /// </summary>
    public class PasteToActiveWindowNode : ActionNodeBase
    {
        private readonly IClipboardService? _clipboardService;
        private readonly IWindowService? _windowService;
        public override string Name => "Paste to Active Window";

        private string _contentKey = "content";
        private int _delayMs = 300;

        public PasteToActiveWindowNode(IServiceProvider services) : base(services)
        {
            _clipboardService = services.GetService<IClipboardService>();
            _windowService = services.GetService<IWindowService>();
        }

        public override void Configure(Dictionary<string, object> config)
        {
            if (config.TryGetValue("contentKey", out var key))
            {
                _contentKey = key.ToString() ?? "content";
            }
            if (config.TryGetValue("delayMs", out var delay))
            {
                _delayMs = Convert.ToInt32(delay.ToString());
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

                // Use IClipboardService if available, fallback to direct Clipboard
                if (_clipboardService != null)
                {
                    _clipboardService.SetText(content);
                }
                else
                {
                    // Fallback for backward compatibility
                    Clipboard.SetText(content);
                }

                // Wait a bit for the form to close and focus to return to previous window
                await Task.Delay(_delayMs);

                // Use IWindowService if available to manage focus
                IntPtr promptArqWindow = IntPtr.Zero;
                
                if (_windowService != null)
                {
                    // Check if a PromptArq window currently has focus
                    IntPtr currentWindow = _windowService.GetForegroundWindow();
                    if (_windowService.IsPromptArqWindow(currentWindow))
                    {
                        promptArqWindow = currentWindow;
                        Log.Information($"[PasteToActiveWindowNode] PromptArq window has focus, switching to last focus window");
                        
                        // Switch to the stored last focus window
                        if (_windowService.SetForegroundLastFocus())
                        {
                            Log.Information($"[PasteToActiveWindowNode] Switched to last focus window: {_windowService.GetWindowTitle(_windowService.LastFocusWindowHandle)}");
                            await Task.Delay(200); // Wait for focus switch
                        }
                        else
                        {
                            Log.Warning($"[PasteToActiveWindowNode] No last focus window stored, using fallback");
                            await _windowService.SwitchToPreviousWindowAsync();
                        }
                    }
                }
                
                // Send Ctrl+V to paste
                SendKeys.SendWait("^v");
                
                // Restore PromptArq focus if it was previously focused
                if (_windowService != null && promptArqWindow != IntPtr.Zero)
                {
                    await _windowService.RestorePromptArqFocusAsync(promptArqWindow);
                }


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
