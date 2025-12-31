using System;
using System.Collections.Generic;
using System.Linq;

namespace PromptArqApp.Core.Capabilities;

/// <summary>
/// Central registry for managing capability providers and capability discovery
/// </summary>
public class CapabilityRegistry
{
    private readonly List<ICapabilityProvider> _providers = new();
    private readonly object _lock = new();
    private List<CapabilityInfo>? _cachedCapabilities;

    /// <summary>
    /// Register a capability provider
    /// </summary>
    /// <param name="provider">Provider to register</param>
    public void RegisterProvider(ICapabilityProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        lock (_lock)
        {
            if (!_providers.Contains(provider))
            {
                _providers.Add(provider);
                _cachedCapabilities = null; // Invalidate cache
            }
        }
    }

    /// <summary>
    /// Register multiple capability providers
    /// </summary>
    /// <param name="providers">Providers to register</param>
    public void RegisterProviders(IEnumerable<ICapabilityProvider> providers)
    {
        foreach (var provider in providers)
        {
            RegisterProvider(provider);
        }
    }

    /// <summary>
    /// Unregister a capability provider
    /// </summary>
    /// <param name="provider">Provider to unregister</param>
    public void UnregisterProvider(ICapabilityProvider provider)
    {
        if (provider == null)
            return;

        lock (_lock)
        {
            if (_providers.Remove(provider))
            {
                _cachedCapabilities = null; // Invalidate cache
            }
        }
    }

    /// <summary>
    /// Get all registered capability providers
    /// </summary>
    /// <returns>Collection of providers</returns>
    public IReadOnlyList<ICapabilityProvider> GetProviders()
    {
        lock (_lock)
        {
            return _providers.ToList();
        }
    }

    /// <summary>
    /// Get all available capabilities from all providers
    /// </summary>
    /// <param name="useCache">Whether to use cached capabilities (default: true)</param>
    /// <returns>Collection of all capabilities</returns>
    public IEnumerable<CapabilityInfo> GetAllCapabilities(bool useCache = true)
    {
        lock (_lock)
        {
            if (useCache && _cachedCapabilities != null)
            {
                return _cachedCapabilities;
            }

            var capabilities = _providers
                .SelectMany(p =>
                {
                    try
                    {
                        return p.GetCapabilities();
                    }
                    catch
                    {
                        // Log error but don't fail entire discovery
                        return Enumerable.Empty<CapabilityInfo>();
                    }
                })
                .Where(c => c.IsEnabled)
                .OrderByDescending(c => c.Priority)
                .ThenBy(c => c.Name)
                .ToList();

            if (useCache)
            {
                _cachedCapabilities = capabilities;
            }

            return capabilities;
        }
    }

    /// <summary>
    /// Search capabilities by query string
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="maxResults">Maximum number of results to return (0 = unlimited)</param>
    /// <returns>Matching capabilities</returns>
    public IEnumerable<CapabilityInfo> SearchCapabilities(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAllCapabilities().Take(maxResults > 0 ? maxResults : int.MaxValue);
        }

        var lowerQuery = query.ToLowerInvariant();
        var results = new List<(CapabilityInfo capability, int score)>();

        lock (_lock)
        {
            // Get static capabilities
            foreach (var capability in GetAllCapabilities())
            {
                var score = CalculateMatchScore(capability, lowerQuery);
                if (score > 0)
                {
                    results.Add((capability, score));
                }
            }

            // Get dynamic capabilities from providers that can handle this query
            foreach (var provider in _providers.Where(p => p.CanHandle(query)))
            {
                try
                {
                    foreach (var capability in provider.GetDynamicCapabilities(query))
                    {
                        if (capability.IsEnabled)
                        {
                            var score = CalculateMatchScore(capability, lowerQuery);
                            results.Add((capability, score));
                        }
                    }
                }
                catch
                {
                    // Log error but continue with other providers
                }
            }
        }

        return results
            .OrderByDescending(r => r.score)
            .ThenByDescending(r => r.capability.Priority)
            .ThenBy(r => r.capability.Name)
            .Select(r => r.capability)
            .Take(maxResults > 0 ? maxResults : int.MaxValue);
    }

    /// <summary>
    /// Get capabilities by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <returns>Capabilities in the specified category</returns>
    public IEnumerable<CapabilityInfo> GetCapabilitiesByCategory(CapabilityCategory category)
    {
        return GetAllCapabilities().Where(c => c.Category == category);
    }

    /// <summary>
    /// Find a capability by ID
    /// </summary>
    /// <param name="id">Capability ID</param>
    /// <returns>Capability if found, null otherwise</returns>
    public CapabilityInfo? FindCapability(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return GetAllCapabilities().FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clear the capability cache
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _cachedCapabilities = null;
        }
    }

    /// <summary>
    /// Calculate match score between a capability and search query
    /// </summary>
    /// <param name="capability">Capability to score</param>
    /// <param name="lowerQuery">Lowercase search query</param>
    /// <returns>Match score (0 = no match, higher = better match)</returns>
    private int CalculateMatchScore(CapabilityInfo capability, string lowerQuery)
    {
        int score = 0;

        var lowerName = capability.Name.ToLowerInvariant();
        var lowerDescription = capability.Description?.ToLowerInvariant() ?? string.Empty;

        // Exact name match
        if (lowerName == lowerQuery)
        {
            score += 1000;
        }
        // Name starts with query
        else if (lowerName.StartsWith(lowerQuery))
        {
            score += 500;
        }
        // Name contains query
        else if (lowerName.Contains(lowerQuery))
        {
            score += 250;
        }

        // Description contains query
        if (lowerDescription.Contains(lowerQuery))
        {
            score += 100;
        }

        // Keyword matches
        foreach (var keyword in capability.Keywords)
        {
            var lowerKeyword = keyword.ToLowerInvariant();
            if (lowerKeyword == lowerQuery)
            {
                score += 300;
            }
            else if (lowerKeyword.StartsWith(lowerQuery))
            {
                score += 150;
            }
            else if (lowerKeyword.Contains(lowerQuery))
            {
                score += 75;
            }
        }

        // Fuzzy matching bonus for close matches
        if (score == 0 && IsFuzzyMatch(lowerName, lowerQuery))
        {
            score += 50;
        }

        return score;
    }

    /// <summary>
    /// Check if there's a fuzzy match between two strings
    /// </summary>
    /// <param name="text">Text to check</param>
    /// <param name="query">Query to match</param>
    /// <returns>True if fuzzy match found</returns>
    private bool IsFuzzyMatch(string text, string query)
    {
        if (string.IsNullOrEmpty(query))
            return false;

        int queryIndex = 0;
        for (int i = 0; i < text.Length && queryIndex < query.Length; i++)
        {
            if (text[i] == query[queryIndex])
            {
                queryIndex++;
            }
        }

        return queryIndex == query.Length;
    }
}
