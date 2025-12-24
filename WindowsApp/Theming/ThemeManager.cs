using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Serilog;

namespace PromptArqApp.Theming
{
    /// <summary>
    /// Singleton service that manages themes across the application.
    /// Handles theme loading, form registration, and theme change notifications.
    /// </summary>
    public sealed class ThemeManager : IDisposable
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext<ThemeManager>();
        private static readonly object _lock = new object();
        private static ThemeManager? _instance;

        private Theme _currentTheme;
        private readonly List<WeakReference<Form>> _registeredForms = new List<WeakReference<Form>>();
        private readonly string _themesDirectory;
        private bool _disposed;

        /// <summary>
        /// Event fired when the current theme changes
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Gets the singleton instance of the ThemeManager
        /// </summary>
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ThemeManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets the currently active theme
        /// </summary>
        public Theme CurrentTheme
        {
            get
            {
                lock (_lock)
                {
                    return _currentTheme;
                }
            }
        }

        private ThemeManager()
        {
            // Initialize themes directory
            _themesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PromptArq",
                "Themes"
            );

            Logger.Information("Initializing ThemeManager with themes directory: {ThemesDirectory}", _themesDirectory);

            // Create themes directory if it doesn't exist
            if (!Directory.Exists(_themesDirectory))
            {
                Directory.CreateDirectory(_themesDirectory);
                Logger.Information("Created themes directory");
            }

            // Extract default themes from embedded resources if needed
            ExtractDefaultThemes();

            // Load default theme
            _currentTheme = ThemeLoader.GetFallbackTheme();
            LoadTheme("DarkBlue");
        }

        /// <summary>
        /// Initializes the ThemeManager. Call this before creating any forms.
        /// </summary>
        public static void Initialize()
        {
            Logger.Information("ThemeManager.Initialize() called");
            var _ = Instance; // Trigger singleton creation
        }

        /// <summary>
        /// Gets a list of available theme names
        /// </summary>
        public List<string> GetAvailableThemes()
        {
            lock (_lock)
            {
                var themeFiles = ThemeLoader.GetThemeFiles(_themesDirectory);
                return themeFiles
                    .Select(f => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))) // Remove .theme.json
                    .ToList();
            }
        }

        /// <summary>
        /// Loads a theme by name
        /// </summary>
        /// <param name="themeName">Name of the theme (without extension)</param>
        /// <returns>True if theme loaded successfully</returns>
        public bool LoadTheme(string themeName)
        {
            lock (_lock)
            {
                try
                {
                    Logger.Information("Loading theme: {ThemeName}", themeName);

                    string themeFile = Path.Combine(_themesDirectory, $"{themeName}.theme.json");

                    if (!File.Exists(themeFile))
                    {
                        Logger.Warning("Theme file not found: {ThemeFile}", themeFile);
                        return false;
                    }

                    var newTheme = ThemeLoader.LoadFromFile(themeFile);

                    // Update current theme
                    var oldTheme = _currentTheme;
                    _currentTheme = newTheme;

                    Logger.Information("Theme loaded successfully: {ThemeName}", newTheme.Name);

                    // Fire theme changed event
                    OnThemeChanged(new ThemeChangedEventArgs(oldTheme, newTheme));

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error loading theme: {ThemeName}", themeName);
                    return false;
                }
            }
        }

        /// <summary>
        /// Registers a form to receive theme updates
        /// </summary>
        /// <param name="form">Form to register</param>
        public void RegisterForm(Form form)
        {
            if (form == null)
                return;

            lock (_lock)
            {
                // Clean up dead references
                _registeredForms.RemoveAll(wr => !wr.TryGetTarget(out _));

                // Check if form is already registered
                if (_registeredForms.Any(wr => wr.TryGetTarget(out var f) && f == form))
                {
                    Logger.Debug("Form already registered: {FormName}", form.Name);
                    return;
                }

                _registeredForms.Add(new WeakReference<Form>(form));
                Logger.Debug("Form registered: {FormName}, Total forms: {Count}", form.Name, _registeredForms.Count);
            }
        }

        /// <summary>
        /// Unregisters a form from receiving theme updates
        /// </summary>
        /// <param name="form">Form to unregister</param>
        public void UnregisterForm(Form form)
        {
            if (form == null)
                return;

            lock (_lock)
            {
                _registeredForms.RemoveAll(wr => wr.TryGetTarget(out var f) && f == form);
                Logger.Debug("Form unregistered: {FormName}", form.Name);
            }
        }

        /// <summary>
        /// Applies the current theme to a form
        /// </summary>
        /// <param name="form">Form to apply theme to</param>
        public void ApplyThemeToForm(Form form)
        {
            if (form == null)
                return;

            lock (_lock)
            {
                ThemeApplicator.ApplyToForm(form, _currentTheme);
            }
        }

        /// <summary>
        /// Refreshes all registered forms with the current theme
        /// </summary>
        public void RefreshAllForms()
        {
            lock (_lock)
            {
                Logger.Information("Refreshing all forms with current theme");

                // Clean up dead references and get live forms
                _registeredForms.RemoveAll(wr => !wr.TryGetTarget(out _));

                var liveForms = _registeredForms
                    .Where(wr => wr.TryGetTarget(out _))
                    .Select(wr => { wr.TryGetTarget(out var f); return f; })
                    .Where(f => f != null)
                    .ToList();

                foreach (var form in liveForms)
                {
                    try
                    {
                        if (form!.InvokeRequired)
                        {
                            form.Invoke(new Action(() => ApplyThemeToForm(form)));
                        }
                        else
                        {
                            ApplyThemeToForm(form);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex, "Error refreshing form: {FormName}", form?.Name ?? "Unknown");
                    }
                }

                Logger.Information("Refreshed {Count} forms", liveForms.Count);
            }
        }

        /// <summary>
        /// Gets a color from the current theme by name
        /// </summary>
        public Color GetColor(string colorName)
        {
            lock (_lock)
            {
                var colors = _currentTheme.Colors;
                var color = colorName switch
                {
                    "Background" => colors.Background,
                    "ControlBackground" => colors.ControlBackground,
                    "InputBackground" => colors.InputBackground,
                    "HeaderBackground" => colors.HeaderBackground,
                    "Foreground" => colors.Foreground,
                    "SecondaryForeground" => colors.SecondaryForeground,
                    "Accent" => colors.Accent,
                    "Border" => colors.Border,
                    "Selection" => colors.Selection,
                    _ => colors.Background
                };

                return ColorTranslator.FromHtml(color);
            }
        }

        /// <summary>
        /// Gets a font from the current theme by name
        /// </summary>
        public Font GetFont(string fontName)
        {
            lock (_lock)
            {
                var fonts = _currentTheme.Fonts;
                var fontDef = fontName switch
                {
                    "Heading" => fonts.Heading,
                    "SearchBox" => fonts.SearchBox,
                    _ => fonts.Default
                };

                return fontDef.ToFont();
            }
        }

        /// <summary>
        /// Extracts default theme files from embedded resources
        /// </summary>
        private void ExtractDefaultThemes()
        {
            try
            {
                var assembly = typeof(ThemeManager).Assembly;
                var resourceNames = assembly.GetManifestResourceNames()
                    .Where(r => r.EndsWith(".theme.json"))
                    .ToArray();

                Logger.Debug("Found {Count} embedded theme resources", resourceNames.Length);

                foreach (var resourceName in resourceNames)
                {
                    // Extract filename from resource name (e.g., "PromptArqApp.Themes.DarkBlue.theme.json" -> "DarkBlue.theme.json")
                    var parts = resourceName.Split('.');
                    var fileName = string.Join(".", parts.Skip(Math.Max(0, parts.Length - 3)));
                    var targetPath = Path.Combine(_themesDirectory, fileName);

                    // Only extract if file doesn't exist (don't overwrite user modifications)
                    if (!File.Exists(targetPath))
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                using (var fileStream = File.Create(targetPath))
                                {
                                    stream.CopyTo(fileStream);
                                }
                                Logger.Information("Extracted default theme: {FileName}", fileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error extracting default themes");
            }
        }

        private void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(this, e);
            RefreshAllForms();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                _registeredForms.Clear();
                _disposed = true;
                Logger.Information("ThemeManager disposed");
            }
        }
    }

    /// <summary>
    /// Event args for theme change notifications
    /// </summary>
    public class ThemeChangedEventArgs : EventArgs
    {
        public Theme OldTheme { get; }
        public Theme NewTheme { get; }

        public ThemeChangedEventArgs(Theme oldTheme, Theme newTheme)
        {
            OldTheme = oldTheme;
            NewTheme = newTheme;
        }
    }
}
