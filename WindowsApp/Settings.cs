using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Serilog;

namespace PromptArqApp
{
    public class HotkeyConfig
    {
        public string Action { get; set; } = "";
        public string Key { get; set; } = "";
        public string Key2 { get; set; } = ""; // Optional second key for sequences like Ctrl+C+C
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
    }

    public class AppSettings
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext<AppSettings>();

        public List<HotkeyConfig> Hotkeys { get; set; } = new List<HotkeyConfig>();
        public int WindowWidth { get; set; } = 1400;
        public int WindowHeight { get; set; } = 900;
        public bool StartMinimized { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool ShowLastUsedPrompts { get; set; } = true;
        public bool ShowLastUsedPlaceholderValues { get; set; } = true;
        public string CurrentTheme { get; set; } = "DarkBlue";
        public List<string> DisabledWorkflows { get; set; } = new List<string>();

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PromptArq",
            "settings.json"
        );

        /// <summary>
        /// Loads application settings from disk. Returns default settings if file doesn't exist or on error.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                Logger.Debug("Loading settings from {SettingsPath}", SettingsPath);

                if (!File.Exists(SettingsPath))
                {
                    Logger.Information("Settings file not found, using defaults");
                    return new AppSettings();
                }

                string json = File.ReadAllText(SettingsPath);
                
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Warning("Settings file is empty, using defaults");
                    return new AppSettings();
                }

                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                
                if (settings == null)
                {
                    Logger.Warning("Failed to deserialize settings, using defaults");
                    return new AppSettings();
                }

                Logger.Information("Settings loaded successfully");
                return settings;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error(ex, "Access denied when loading settings from {SettingsPath}", SettingsPath);
                return new AppSettings();
            }
            catch (IOException ex)
            {
                Logger.Error(ex, "I/O error when loading settings from {SettingsPath}", SettingsPath);
                return new AppSettings();
            }
            catch (JsonException ex)
            {
                Logger.Error(ex, "JSON parsing error when loading settings. File may be corrupted: {SettingsPath}", SettingsPath);
                // Try to backup corrupted file
                BackupCorruptedFile(SettingsPath);
                return new AppSettings();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error loading settings from {SettingsPath}", SettingsPath);
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves application settings to disk with proper error handling and atomic write.
        /// </summary>
        public void Save()
        {
            string? tempFile = null;
            
            try
            {
                Logger.Debug("Saving settings to {SettingsPath}", SettingsPath);

                string directory = Path.GetDirectoryName(SettingsPath) ?? "";
                
                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException("Could not determine settings directory");
                }

                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Logger.Information("Creating settings directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Serialize to JSON
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);

                // Atomic write: write to temp file first, then replace
                tempFile = Path.Combine(directory, $"settings.tmp.{Guid.NewGuid():N}");
                File.WriteAllText(tempFile, json);

                // Backup existing file if it exists
                if (File.Exists(SettingsPath))
                {
                    string backupPath = SettingsPath + ".backup";
                    File.Copy(SettingsPath, backupPath, overwrite: true);
                    Logger.Debug("Created backup: {BackupPath}", backupPath);
                }

                // Replace old file with new one
                File.Move(tempFile, SettingsPath, overwrite: true);
                tempFile = null; // Clear reference since file was moved successfully

                Logger.Information("Settings saved successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error(ex, "Access denied when saving settings to {SettingsPath}", SettingsPath);
                throw new InvalidOperationException("Failed to save settings: Access denied. Check file permissions.", ex);
            }
            catch (IOException ex)
            {
                Logger.Error(ex, "I/O error when saving settings to {SettingsPath}", SettingsPath);
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                Logger.Error(ex, "JSON serialization error when saving settings");
                throw new InvalidOperationException("Failed to save settings: Serialization error", ex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error saving settings to {SettingsPath}", SettingsPath);
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
            finally
            {
                // Clean up temp file if it still exists
                if (tempFile != null && File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                        Logger.Debug("Cleaned up temporary file: {TempFile}", tempFile);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex, "Failed to delete temporary file: {TempFile}", tempFile);
                    }
                }
            }
        }

        public void SetDefaultHotkeys()
        {
            Logger.Information("Setting default hotkeys");
            Hotkeys = new List<HotkeyConfig>
            {
                new HotkeyConfig { Action = "Show/Hide Window", Key = "P", Ctrl = true, Alt = true, Shift = false, Win = false },
                new HotkeyConfig { Action = "New Prompt", Key = "N", Ctrl = true, Shift = false, Alt = true, Win = false },
                new HotkeyConfig { Action = "Settings", Key = "S", Ctrl = true, Alt = true, Shift = false, Win = false },
                new HotkeyConfig { Action = "Command Palette", Key = "K", Ctrl = true, Alt = true, Shift = false, Win = false },
                new HotkeyConfig { Action = "Quit App", Key = "Q", Ctrl = true, Alt = true, Shift = false, Win = false }
            };
        }

        /// <summary>
        /// Creates a backup of a corrupted settings file
        /// </summary>
        private static void BackupCorruptedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string backupPath = $"{filePath}.corrupted.{DateTime.Now:yyyyMMddHHmmss}";
                    File.Copy(filePath, backupPath, overwrite: true);
                    Logger.Information("Backed up corrupted settings file to {BackupPath}", backupPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to backup corrupted settings file");
            }
        }

        private HashSet<string> GetDisabledWorkflowSet()
        {
            return new HashSet<string>(DisabledWorkflows ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsWorkflowEnabled(string? workflowId)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                return false;

            var set = GetDisabledWorkflowSet();
            return !set.Contains(workflowId);
        }

        public void SetWorkflowEnabled(string workflowId, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(workflowId))
                return;

            var comparer = StringComparer.OrdinalIgnoreCase;
            var set = GetDisabledWorkflowSet();
            var isDisabled = set.Contains(workflowId);

            if (enabled)
            {
                if (isDisabled)
                {
                    DisabledWorkflows.RemoveAll(w => comparer.Equals(w, workflowId));
                }
            }
            else
            {
                if (!isDisabled)
                {
                    DisabledWorkflows.Add(workflowId);
                }
            }
        }
    }
}
