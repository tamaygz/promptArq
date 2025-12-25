namespace PromptArqApp.Core.Capabilities;

/// <summary>
/// Categories for organizing capabilities in the command palette
/// </summary>
public enum CapabilityCategory
{
    /// <summary>
    /// Prompt-related capabilities (search, execute, manage prompts)
    /// </summary>
    Prompts,
    
    /// <summary>
    /// Clipboard operations (copy, paste, history)
    /// </summary>
    Clipboard,
    
    /// <summary>
    /// System commands (shutdown, lock, etc.)
    /// </summary>
    System,
    
    /// <summary>
    /// File operations (open, search, manage files)
    /// </summary>
    Files,
    
    /// <summary>
    /// Window management operations
    /// </summary>
    Windows,
    
    /// <summary>
    /// Calculator and computations
    /// </summary>
    Calculator,
    
    /// <summary>
    /// Text snippets and expansion
    /// </summary>
    Snippets,
    
    /// <summary>
    /// Web-related actions (search, open URLs)
    /// </summary>
    Web,
    
    /// <summary>
    /// Application settings and configuration
    /// </summary>
    Settings,
    
    /// <summary>
    /// Custom or third-party capabilities
    /// </summary>
    Custom
}
