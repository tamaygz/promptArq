using System;
using System.Collections.Generic;

namespace PromptArqApp.Core.Actions;

/// <summary>
/// Context information for executing universal actions
/// </summary>
public class ActionContext
{
    /// <summary>
    /// The content to act upon
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Type of the content
    /// </summary>
    public ContentType ContentType { get; init; } = ContentType.Text;
    
    /// <summary>
    /// Source of the content (e.g., "clipboard", "prompt", "selection")
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// Additional metadata about the content
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
    
    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    public IServiceProvider? Services { get; init; }
    
    /// <summary>
    /// Whether the action should execute silently (no UI)
    /// </summary>
    public bool Silent { get; init; } = false;
}
