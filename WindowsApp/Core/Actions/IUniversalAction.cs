using System.Threading.Tasks;

namespace PromptArqApp.Core.Actions;

/// <summary>
/// Interface for universal actions that can be performed on content
/// </summary>
public interface IUniversalAction
{
    /// <summary>
    /// Unique identifier for this action
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Display name for this action
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what this action does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Icon for this action (emoji, font icon, or path)
    /// </summary>
    string? Icon { get; }
    
    /// <summary>
    /// Content types this action supports
    /// </summary>
    ContentType[] SupportedContentTypes { get; }
    
    /// <summary>
    /// Check if this action can handle the given content
    /// </summary>
    /// <param name="content">Content to check</param>
    /// <param name="contentType">Type of content</param>
    /// <returns>True if this action can handle the content</returns>
    bool CanHandle(string content, ContentType contentType);
    
    /// <summary>
    /// Execute this action
    /// </summary>
    /// <param name="context">Action context with content and metadata</param>
    /// <returns>Task representing the action execution</returns>
    Task<ActionResult> ExecuteAsync(ActionContext context);
}

/// <summary>
/// Result of executing a universal action
/// </summary>
public class ActionResult
{
    /// <summary>
    /// Whether the action succeeded
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Message describing the result
    /// </summary>
    public string? Message { get; init; }
    
    /// <summary>
    /// Output data from the action (if any)
    /// </summary>
    public object? Output { get; init; }
    
    /// <summary>
    /// Error message if action failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static ActionResult Successful(string? message = null, object? output = null)
    {
        return new ActionResult
        {
            Success = true,
            Message = message,
            Output = output
        };
    }
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    public static ActionResult Failed(string error)
    {
        return new ActionResult
        {
            Success = false,
            Error = error
        };
    }
}
