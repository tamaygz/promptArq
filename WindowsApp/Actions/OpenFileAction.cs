using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PromptArqApp.Core.Actions;

namespace PromptArqApp.Actions;

/// <summary>
/// Action to open files in their default application
/// </summary>
public class OpenFileAction : IUniversalAction
{
    public string Id => "open-file";
    public string Name => "Open File";
    public string Description => "Open a file in its default application";
    public string? Icon => "📁";
    public ContentType[] SupportedContentTypes => new[] { ContentType.File };

    public bool CanHandle(string content, ContentType contentType)
    {
        if (contentType != ContentType.File)
            return false;

        // Check if it's a valid file path
        try
        {
            return File.Exists(content) || Directory.Exists(content);
        }
        catch
        {
            return false;
        }
    }

    public async Task<ActionResult> ExecuteAsync(ActionContext context)
    {
        try
        {
            if (!CanHandle(context.Content, context.ContentType))
            {
                return ActionResult.Failed("File or directory does not exist");
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

            var itemType = Directory.Exists(context.Content) ? "directory" : "file";
            return ActionResult.Successful($"Opened {itemType}: {Path.GetFileName(context.Content)}");
        }
        catch (Exception ex)
        {
            return ActionResult.Failed($"Failed to open file: {ex.Message}");
        }
    }
}
