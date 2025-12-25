using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace PromptArqApp
{
    public class PlaceholderValueEntry
    {
        public string Value { get; set; } = "";
        public DateTime LastUsed { get; set; }
    }

    public class PromptUsageEntry
    {
        public string PromptId { get; set; } = "";
        public string PromptTitle { get; set; } = "";
        public DateTime LastUsed { get; set; }
        public int UseCount { get; set; }
    }

    public class PromptHistory
    {
        public List<PromptUsageEntry> RecentPrompts { get; set; } = new List<PromptUsageEntry>();
        public Dictionary<string, List<PlaceholderValueEntry>> PlaceholderValues { get; set; } = new Dictionary<string, List<PlaceholderValueEntry>>();

        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PromptArq",
            "history.json"
        );

        private static readonly int MaxRecentPrompts = 20;
        private static readonly int MaxPlaceholderValues = 10;

        public static PromptHistory Load()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    string json = File.ReadAllText(HistoryPath);
                    return JsonConvert.DeserializeObject<PromptHistory>(json) ?? new PromptHistory();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading history: {ex.Message}");
            }
            return new PromptHistory();
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(HistoryPath) ?? "";
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(HistoryPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving history: {ex.Message}");
            }
        }

        public void RecordPromptUsage(string promptId, string promptTitle)
        {
            var existing = RecentPrompts.FirstOrDefault(p => p.PromptId == promptId);
            if (existing != null)
            {
                existing.LastUsed = DateTime.Now;
                existing.UseCount++;
            }
            else
            {
                RecentPrompts.Add(new PromptUsageEntry
                {
                    PromptId = promptId,
                    PromptTitle = promptTitle,
                    LastUsed = DateTime.Now,
                    UseCount = 1
                });
            }

            // Keep only the most recent prompts, sorted by last used
            RecentPrompts = RecentPrompts
                .OrderByDescending(p => p.LastUsed)
                .Take(MaxRecentPrompts)
                .ToList();

            Save();
        }

        public void RecordPlaceholderValue(string placeholderName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (!PlaceholderValues.ContainsKey(placeholderName))
            {
                PlaceholderValues[placeholderName] = new List<PlaceholderValueEntry>();
            }

            var values = PlaceholderValues[placeholderName];
            var existing = values.FirstOrDefault(v => v.Value == value);
            
            if (existing != null)
            {
                existing.LastUsed = DateTime.Now;
            }
            else
            {
                values.Add(new PlaceholderValueEntry
                {
                    Value = value,
                    LastUsed = DateTime.Now
                });
            }

            // Keep only the most recent values, sorted by last used
            PlaceholderValues[placeholderName] = values
                .OrderByDescending(v => v.LastUsed)
                .Take(MaxPlaceholderValues)
                .ToList();

            Save();
        }

        public List<string> GetPlaceholderValueSuggestions(string placeholderName, string excludeValue = "")
        {
            if (!PlaceholderValues.ContainsKey(placeholderName))
                return new List<string>();

            return PlaceholderValues[placeholderName]
                .Where(v => v.Value != excludeValue) // Exclude the specified value
                .OrderByDescending(v => v.LastUsed)
                .Select(v => v.Value)
                .ToList();
        }

        public List<PromptUsageEntry> GetRecentPrompts()
        {
            return RecentPrompts
                .OrderByDescending(p => p.LastUsed)
                .ToList();
        }

        /// <summary>
        /// Clears all prompt history and placeholder values
        /// </summary>
        public void Clear()
        {
            RecentPrompts.Clear();
            PlaceholderValues.Clear();
            Save();
        }
    }
}
