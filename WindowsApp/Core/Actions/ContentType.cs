namespace PromptArqApp.Core.Actions;

/// <summary>
/// Content types for universal actions
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Plain text content
    /// </summary>
    Text,
    
    /// <summary>
    /// File path or file reference
    /// </summary>
    File,
    
    /// <summary>
    /// URL or web link
    /// </summary>
    Url,
    
    /// <summary>
    /// Email address
    /// </summary>
    Email,
    
    /// <summary>
    /// Phone number
    /// </summary>
    PhoneNumber,
    
    /// <summary>
    /// Image data or reference
    /// </summary>
    Image,
    
    /// <summary>
    /// JSON data
    /// </summary>
    Json,
    
    /// <summary>
    /// Custom or unknown type
    /// </summary>
    Custom
}
