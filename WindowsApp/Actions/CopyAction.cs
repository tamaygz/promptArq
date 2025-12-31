using System;
using System.Linq;
using System.Threading.Tasks;
using PromptArqApp.Core.Actions;
using PromptArqApp.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace PromptArqApp.Actions;

/// <summary>
/// Action to copy content to clipboard using IClipboardService
/// </summary>
public class CopyAction : IUniversalAction
{
    private readonly IClipboardService? _clipboardService;

    public CopyAction(IClipboardService? clipboardService = null)
    {
        _clipboardService = clipboardService;
    }

    public string Id => "copy";
    public string Name => "Copy to Clipboard";
    public string Description => "Copy content to the system clipboard";
    public string? Icon => "📋";
    public ContentType[] SupportedContentTypes => new[] 
    { 
        ContentType.Text, 
        ContentType.Url, 
        ContentType.File,
        ContentType.Json 
    };

    public bool CanHandle(string content, ContentType contentType)
    {
        return !string.IsNullOrEmpty(content) && 
               SupportedContentTypes.Any(t => t == contentType);
    }

    public async Task<ActionResult> ExecuteAsync(ActionContext context)
    {
        try
        {
            // Try to get IClipboardService from context
            var clipboardService = _clipboardService ?? context.Services?.GetService<IClipboardService>();

            await Task.Run(() =>
            {
                if (clipboardService != null)
                {
                    clipboardService.SetText(context.Content);
                }
                else
                {
                    // Fallback to direct clipboard access
                    System.Windows.Forms.Clipboard.SetText(context.Content);
                }
            });

            return ActionResult.Successful($"Copied {context.Content.Length} characters to clipboard");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Failed to copy to clipboard: {ex.Message}");
        }
    }
}
