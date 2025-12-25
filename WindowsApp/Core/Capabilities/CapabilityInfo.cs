using System;
using System.Collections.Generic;

namespace PromptArqApp.Core.Capabilities;

/// <summary>
/// Represents a discoverable capability that can be shown in the command palette
/// </summary>
public class CapabilityInfo
{
    /// <summary>
    /// Unique identifier for this capability
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Display name shown in the command palette
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Description/hint shown under the name
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Category for grouping
    /// </summary>
    public CapabilityCategory Category { get; init; }
    
    /// <summary>
    /// Icon identifier (emoji, font icon, or path)
    /// </summary>
    public string? Icon { get; init; }
    
    /// <summary>
    /// Keywords for search matching
    /// </summary>
    public string[] Keywords { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Workflow ID to launch when this capability is selected
    /// </summary>
    public string? WorkflowId { get; init; }
    
    /// <summary>
    /// Action to execute when selected (alternative to workflow)
    /// </summary>
    public string? ActionId { get; init; }
    
    /// <summary>
    /// Whether this capability is currently available
    /// </summary>
    public bool IsEnabled { get; init; } = true;
    
    /// <summary>
    /// Priority for sorting (higher = shown first)
    /// </summary>
    public int Priority { get; init; } = 0;
    
    /// <summary>
    /// Metadata for custom data
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
