using System.Collections.Generic;
using System.Linq;

namespace PromptArqApp.Core.Capabilities;

/// <summary>
/// Interface for components that provide capabilities to the command palette
/// </summary>
public interface ICapabilityProvider
{
    /// <summary>
    /// Name of this capability provider
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Get all capabilities provided by this provider
    /// </summary>
    /// <returns>Collection of capabilities</returns>
    IEnumerable<CapabilityInfo> GetCapabilities();
    
    /// <summary>
    /// Check if this provider can handle the given query
    /// </summary>
    /// <param name="query">Search query</param>
    /// <returns>True if this provider should be considered for the query</returns>
    bool CanHandle(string query) => true;
    
    /// <summary>
    /// Get dynamic capabilities based on a query (optional)
    /// </summary>
    /// <param name="query">Search query</param>
    /// <returns>Query-specific capabilities</returns>
    IEnumerable<CapabilityInfo> GetDynamicCapabilities(string query)
    {
        return Enumerable.Empty<CapabilityInfo>();
    }
}
