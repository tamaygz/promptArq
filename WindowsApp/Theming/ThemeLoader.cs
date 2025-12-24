using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

namespace PromptArqApp.Theming
{
    /// <summary>
    /// Handles loading and saving theme files with comprehensive error handling.
    /// </summary>
    public class ThemeLoader
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext<ThemeLoader>();

        /// <summary>
        /// Loads a theme from a JSON file.
        /// </summary>
        /// <param name="filePath">Full path to the theme file</param>
        /// <returns>Loaded theme or fallback theme on error</returns>
        public static Theme LoadFromFile(string filePath)
        {
            try
            {
                Logger.Debug("Loading theme from {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    Logger.Warning("Theme file not found: {FilePath}", filePath);
                    return GetFallbackTheme();
                }

                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Warning("Theme file is empty: {FilePath}", filePath);
                    return GetFallbackTheme();
                }

                var theme = JsonConvert.DeserializeObject<Theme>(json);

                if (theme == null)
                {
                    Logger.Warning("Failed to deserialize theme from {FilePath}", filePath);
                    return GetFallbackTheme();
                }

                // Validate the loaded theme
                var errors = theme.Validate();
                if (errors.Any())
                {
                    Logger.Warning("Theme validation errors in {FilePath}: {Errors}", filePath, string.Join(", ", errors));
                    // Continue with the theme but log warnings
                }

                Logger.Information("Theme loaded successfully: {ThemeName} v{Version}", theme.Name, theme.Version);
                return theme;
            }
            catch (JsonException ex)
            {
                Logger.Error(ex, "JSON parsing error when loading theme from {FilePath}", filePath);
                return GetFallbackTheme();
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error(ex, "Access denied when loading theme from {FilePath}", filePath);
                return GetFallbackTheme();
            }
            catch (IOException ex)
            {
                Logger.Error(ex, "I/O error when loading theme from {FilePath}", filePath);
                return GetFallbackTheme();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Unexpected error loading theme from {FilePath}", filePath);
                return GetFallbackTheme();
            }
        }

        /// <summary>
        /// Saves a theme to a JSON file.
        /// </summary>
        /// <param name="theme">Theme to save</param>
        /// <param name="filePath">Full path to save the theme file</param>
        public static void SaveToFile(Theme theme, string filePath)
        {
            string? tempFile = null;

            try
            {
                Logger.Debug("Saving theme to {FilePath}", filePath);

                // Validate theme before saving
                var errors = theme.Validate();
                if (errors.Any())
                {
                    throw new InvalidOperationException($"Theme validation failed: {string.Join(", ", errors)}");
                }

                string directory = Path.GetDirectoryName(filePath) ?? "";

                if (string.IsNullOrEmpty(directory))
                {
                    throw new InvalidOperationException("Could not determine theme directory");
                }

                // Ensure directory exists
                if (!Directory.Exists(directory))
                {
                    Logger.Information("Creating theme directory: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                }

                // Serialize to JSON with formatting
                string json = JsonConvert.SerializeObject(theme, Formatting.Indented);

                // Atomic write: write to temp file first, then replace
                tempFile = Path.Combine(directory, $"theme.tmp.{Guid.NewGuid():N}");
                File.WriteAllText(tempFile, json);

                // Backup existing file if it exists
                if (File.Exists(filePath))
                {
                    string backupPath = filePath + ".backup";
                    File.Copy(filePath, backupPath, overwrite: true);
                    Logger.Debug("Created backup: {BackupPath}", backupPath);
                }

                // Replace old file with new one
                File.Move(tempFile, filePath, overwrite: true);
                tempFile = null;

                Logger.Information("Theme saved successfully: {ThemeName}", theme.Name);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error saving theme to {FilePath}", filePath);
                throw;
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

        /// <summary>
        /// Gets all theme files in a directory.
        /// </summary>
        /// <param name="directory">Directory to search for theme files</param>
        /// <returns>List of theme file paths</returns>
        public static List<string> GetThemeFiles(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Logger.Debug("Theme directory does not exist: {Directory}", directory);
                    return new List<string>();
                }

                var files = Directory.GetFiles(directory, "*.theme.json")
                    .OrderBy(f => Path.GetFileName(f))
                    .ToList();

                Logger.Debug("Found {Count} theme files in {Directory}", files.Count, directory);
                return files;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting theme files from {Directory}", directory);
                return new List<string>();
            }
        }

        /// <summary>
        /// Returns a fallback theme with default values for error scenarios.
        /// </summary>
        public static Theme GetFallbackTheme()
        {
            Logger.Information("Using fallback theme");

            return new Theme
            {
                Name = "Fallback Dark",
                Version = "1.0",
                Colors = new ThemeColors
                {
                    Background = "#1E1E1E",
                    ControlBackground = "#232323",
                    InputBackground = "#323232",
                    HeaderBackground = "#282828",
                    Foreground = "#FFFFFF",
                    SecondaryForeground = "#808080",
                    Accent = "#3C7FB1",
                    Border = "#3C3C3C",
                    Selection = "#3C78B4"
                },
                Fonts = new ThemeFonts
                {
                    Default = new FontDefinition { Family = "Segoe UI", Size = 10, Style = "Regular" },
                    Heading = new FontDefinition { Family = "Segoe UI", Size = 14, Style = "Bold" },
                    SearchBox = new FontDefinition { Family = "Segoe UI", Size = 16, Style = "Regular" }
                },
                Window = new WindowProperties
                {
                    Opacity = 0.97,
                    CornerRadius = 15,
                    TitleBarColor = "#00663300",
                    BorderColor = "#00663300"
                },
                Controls = new ControlThemes
                {
                    Button = new ButtonTheme
                    {
                        Background = "#323232",
                        Foreground = "#FFFFFF",
                        HoverBackground = "#3C78B4"
                    },
                    ListBox = new ListBoxTheme
                    {
                        Background = "#232323",
                        Foreground = "#D3D3D3",
                        SelectedBackground = "#3C78B4",
                        SelectedForeground = "#FFFFFF"
                    },
                    TextBox = new TextBoxTheme
                    {
                        Background = "#323232",
                        Foreground = "#FFFFFF"
                    }
                }
            };
        }
    }
}
