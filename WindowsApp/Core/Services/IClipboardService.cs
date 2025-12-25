using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PromptArqApp.Core.Services;

/// <summary>
/// Represents a clipboard history entry
/// </summary>
public class ClipboardEntry
{
    /// <summary>
    /// Unique identifier for this entry
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// The clipboard content
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// Type of content (text, image, file, etc.)
    /// </summary>
    public string ContentType { get; init; } = "text";
    
    /// <summary>
    /// Timestamp when this entry was created
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Source application that created this entry (if known)
    /// </summary>
    public string? Source { get; init; }
    
    /// <summary>
    /// Whether this entry is pinned (won't be auto-deleted)
    /// </summary>
    public bool IsPinned { get; init; }
    
    /// <summary>
    /// Tags for organizing clipboard entries
    /// </summary>
    public string[] Tags { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Service for managing clipboard operations with history
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Get the current clipboard text content
    /// </summary>
    /// <returns>Current clipboard text</returns>
    string? GetText();
    
    /// <summary>
    /// Set the clipboard text content
    /// </summary>
    /// <param name="text">Text to set</param>
    void SetText(string text);
    
    /// <summary>
    /// Get clipboard history entries
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to return (0 = all)</param>
    /// <returns>List of clipboard entries</returns>
    Task<List<ClipboardEntry>> GetHistoryAsync(int maxEntries = 100);
    
    /// <summary>
    /// Get a specific clipboard entry by ID
    /// </summary>
    /// <param name="id">Entry ID</param>
    /// <returns>Entry if found, null otherwise</returns>
    Task<ClipboardEntry?> GetEntryAsync(string id);
    
    /// <summary>
    /// Search clipboard history
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="maxResults">Maximum number of results</param>
    /// <returns>Matching entries</returns>
    Task<List<ClipboardEntry>> SearchHistoryAsync(string query, int maxResults = 20);
    
    /// <summary>
    /// Pin a clipboard entry (prevents auto-deletion)
    /// </summary>
    /// <param name="id">Entry ID to pin</param>
    /// <returns>True if pinned successfully</returns>
    Task<bool> PinEntryAsync(string id);
    
    /// <summary>
    /// Unpin a clipboard entry
    /// </summary>
    /// <param name="id">Entry ID to unpin</param>
    /// <returns>True if unpinned successfully</returns>
    Task<bool> UnpinEntryAsync(string id);
    
    /// <summary>
    /// Delete a clipboard entry from history
    /// </summary>
    /// <param name="id">Entry ID to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteEntryAsync(string id);
    
    /// <summary>
    /// Clear all clipboard history
    /// </summary>
    /// <param name="includePinned">Whether to also clear pinned entries</param>
    /// <returns>Number of entries cleared</returns>
    Task<int> ClearHistoryAsync(bool includePinned = false);
    
    /// <summary>
    /// Enable clipboard monitoring
    /// </summary>
    void EnableMonitoring();
    
    /// <summary>
    /// Disable clipboard monitoring
    /// </summary>
    void DisableMonitoring();
    
    /// <summary>
    /// Check if clipboard monitoring is enabled
    /// </summary>
    bool IsMonitoringEnabled { get; }
    
    /// <summary>
    /// Event raised when clipboard content changes
    /// </summary>
    event EventHandler<ClipboardEntry>? ClipboardChanged;
}
