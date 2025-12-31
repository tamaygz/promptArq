using System;
using System.Diagnostics;
using System.Threading.Tasks;
using PromptArqApp.Core.Actions;

namespace PromptArqApp.Actions;

/// <summary>
/// Action to open URLs in the default browser
/// </summary>
public class OpenUrlAction : IUniversalAction
{
    public string Id => "open-url";
    public string Name => "Open URL";
    public string Description => "Open a URL in the default web browser";
    public string? Icon => "🌐";
    public ContentType[] SupportedContentTypes => new[] { ContentType.Url };

    public bool CanHandle(string content, ContentType contentType)
    {
        if (contentType != ContentType.Url)
            return false;

        // Basic URL validation
        return Uri.TryCreate(content, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public async Task<ActionResult> ExecuteAsync(ActionContext context)
    {
        try
        {
            if (!CanHandle(context.Content, context.ContentType))
            {
                return ActionResult.Failed("Invalid URL format");
            }

            await Task.Run(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = context.Content,
                    UseShellExecute = true
                };
                Process.Start(psi);
            });

            return ActionResult.Successful($"Opened URL: {context.Content}");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Failed to open URL: {ex.Message}");
        }
    }
}
