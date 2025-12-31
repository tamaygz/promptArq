using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PromptArqApp.Core.Services;

namespace PromptArqApp.Services;

/// <summary>
/// Basic clipboard service implementation
/// Future: Add SQLite-based history persistence
/// </summary>
public class ClipboardService : IClipboardService
{
    private readonly List<ClipboardEntry> _history = new();
    private readonly object _lock = new();
    private bool _isMonitoring = false;
    
    public bool IsMonitoringEnabled => _isMonitoring;
    
    public event EventHandler<ClipboardEntry>? ClipboardChanged;

    public string? GetText()
    {
        try
        {
            return Clipboard.GetText();
        }
        catch
        {
            return null;
        }
    }

    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Clipboard.SetText(text);
        
        // Add to history
        var entry = new ClipboardEntry
        {
            Id = Guid.NewGuid().ToString(),
            Content = text,
            ContentType = "text",
            Timestamp = DateTime.Now,
            Source = "PromptArq"
        };
        
        lock (_lock)
        {
            _history.Insert(0, entry);
            
            // Keep only last 1000 entries
            while (_history.Count > 1000)
            {
                var toRemove = _history.LastOrDefault(e => !e.IsPinned);
                if (toRemove != null)
                {
                    _history.Remove(toRemove);
                }
                else
                {
                    break;
                }
            }
        }
        
        ClipboardChanged?.Invoke(this, entry);
    }

    public Task<List<ClipboardEntry>> GetHistoryAsync(int maxEntries = 100)
    {
        lock (_lock)
        {
            var count = maxEntries > 0 ? Math.Min(maxEntries, _history.Count) : _history.Count;
            return Task.FromResult(_history.Take(count).ToList());
        }
    }

    public Task<ClipboardEntry?> GetEntryAsync(string id)
    {
        lock (_lock)
        {
            return Task.FromResult(_history.FirstOrDefault(e => e.Id == id));
        }
    }

    public Task<List<ClipboardEntry>> SearchHistoryAsync(string query, int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetHistoryAsync(maxResults);

        lock (_lock)
        {
            var results = _history
                .Where(e => e.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults)
                .ToList();
            
            return Task.FromResult(results);
        }
    }

    public Task<bool> PinEntryAsync(string id)
    {
        lock (_lock)
        {
            var entry = _history.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                var index = _history.IndexOf(entry);
                _history[index] = new ClipboardEntry
                {
                    Id = entry.Id,
                    Content = entry.Content,
                    ContentType = entry.ContentType,
                    Timestamp = entry.Timestamp,
                    Source = entry.Source,
                    IsPinned = true,
                    Tags = entry.Tags
                };
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    public Task<bool> UnpinEntryAsync(string id)
    {
        lock (_lock)
        {
            var entry = _history.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                var index = _history.IndexOf(entry);
                _history[index] = new ClipboardEntry
                {
                    Id = entry.Id,
                    Content = entry.Content,
                    ContentType = entry.ContentType,
                    Timestamp = entry.Timestamp,
                    Source = entry.Source,
                    IsPinned = false,
                    Tags = entry.Tags
                };
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    public Task<bool> DeleteEntryAsync(string id)
    {
        lock (_lock)
        {
            var entry = _history.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                _history.Remove(entry);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    public Task<int> ClearHistoryAsync(bool includePinned = false)
    {
        lock (_lock)
        {
            var toRemove = includePinned 
                ? _history.ToList() 
                : _history.Where(e => !e.IsPinned).ToList();
            
            foreach (var entry in toRemove)
            {
                _history.Remove(entry);
            }
            
            return Task.FromResult(toRemove.Count);
        }
    }

    public void EnableMonitoring()
    {
        _isMonitoring = true;
        // TODO: Implement Windows clipboard monitoring
    }

    public void DisableMonitoring()
    {
        _isMonitoring = false;
        // TODO: Implement Windows clipboard monitoring
    }
}
