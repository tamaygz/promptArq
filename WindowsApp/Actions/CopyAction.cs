using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PromptArqApp.Core.Actions;

namespace PromptArqApp.Actions;

/// <summary>
/// Action to copy content to clipboard
/// </summary>
public class CopyAction : IUniversalAction
{
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
            await Task.Run(() =>
            {
                Clipboard.SetText(context.Content);
            });

            return ActionResult.Successful($"Copied {context.Content.Length} characters to clipboard");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Failed to copy to clipboard: {ex.Message}");
        }
    }
}
