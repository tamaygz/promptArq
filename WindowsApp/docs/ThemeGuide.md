# WindowsApp Theming System Guide

## Overview

The PromptArq WindowsApp features a comprehensive theming system that allows developers to easily create, apply, and switch between visual themes. This guide explains how to use the theming system effectively.

## Architecture

The theming system consists of four main components:

1. **Theme Model** (`Theme.cs`) - Data structures for theme definitions
2. **ThemeLoader** (`ThemeLoader.cs`) - File I/O operations for theme files
3. **ThemeManager** (`ThemeManager.cs`) - Singleton service managing themes
4. **ThemeApplicator** (`ThemeApplicator.cs`) - Applies themes to forms and controls

## Theme File Format

Themes are defined in JSON files with the `.theme.json` extension. The structure is as follows:

```json
{
  "name": "Dark Blue",
  "version": "1.0",
  "colors": {
    "background": "#1E1E1E",
    "controlBackground": "#232323",
    "inputBackground": "#323232",
    "headerBackground": "#282828",
    "foreground": "#FFFFFF",
    "secondaryForeground": "#808080",
    "accent": "#3C7FB1",
    "border": "#3C3C3C",
    "selection": "#3C78B4"
  },
  "fonts": {
    "default": {
      "family": "Segoe UI",
      "size": 10,
      "style": "Regular"
    },
    "heading": {
      "family": "Segoe UI",
      "size": 14,
      "style": "Bold"
    },
    "searchBox": {
      "family": "Segoe UI",
      "size": 16,
      "style": "Regular"
    }
  },
  "window": {
    "opacity": 0.97,
    "cornerRadius": 15,
    "titleBarColor": "#00663300",
    "borderColor": "#00663300"
  },
  "controls": {
    "button": {
      "background": "#323232",
      "foreground": "#FFFFFF",
      "hoverBackground": "#3C78B4"
    },
    "listBox": {
      "background": "#232323",
      "foreground": "#D3D3D3",
      "selectedBackground": "#3C78B4",
      "selectedForeground": "#FFFFFF"
    },
    "textBox": {
      "background": "#323232",
      "foreground": "#FFFFFF"
    }
  }
}
```

### Color Format

Colors must be in hexadecimal format:
- **RGB**: `#RRGGBB` (e.g., `#1E1E1E`)
- **ARGB**: `#AARRGGBB` (e.g., `#FF1E1E1E`)
- **Short RGB**: `#RGB` (e.g., `#FFF`)

### Font Styles

Supported font styles:
- `Regular`
- `Bold`
- `Italic`
- `Bold Italic` or `BoldItalic`
- `Underline`
- `Strikeout`

## Creating a New Theme

1. **Create a JSON file** in `%APPDATA%/PromptArq/Themes/` with the `.theme.json` extension
   - Example: `MyCustomTheme.theme.json`

2. **Define theme properties** using the structure shown above

3. **Test your theme** by:
   - Opening the Settings dialog
   - Selecting your new theme from the dropdown
   - Clicking Save

## Applying Themes to Forms

### Method 1: Automatic Theme Application (Recommended)

Register your form with ThemeManager in the constructor:

```csharp
using PromptArqApp.Theming;

public class MyForm : Form
{
    public MyForm()
    {
        InitializeComponent();
        
        // Register with ThemeManager and apply theme
        ThemeManager.Instance.RegisterForm(this);
        ThemeManager.Instance.ApplyThemeToForm(this);
        
        // Subscribe to theme changes
        ThemeManager.Instance.ThemeChanged += (s, e) =>
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ThemeManager.Instance.ApplyThemeToForm(this)));
            }
            else
            {
                ThemeManager.Instance.ApplyThemeToForm(this);
            }
        };
    }
}
```

### Method 2: Manual Theme Application

For one-time theme application without automatic updates:

```csharp
ThemeManager.Instance.ApplyThemeToForm(this);
```

## Working with Custom-Drawn Controls

If you have custom-drawn controls (e.g., ListBox with DrawMode.OwnerDrawFixed), update your DrawItem handler to use theme colors:

```csharp
private void MyListBox_DrawItem(object? sender, DrawItemEventArgs e)
{
    if (e.Index < 0) return;
    
    var theme = ThemeManager.Instance.CurrentTheme;
    var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
    
    // Get colors from theme
    var bgColor = isSelected 
        ? ThemeApplicator.ParseColor(theme.Controls.ListBox.SelectedBackground)
        : ThemeApplicator.ParseColor(theme.Controls.ListBox.Background);
    
    var fgColor = isSelected
        ? ThemeApplicator.ParseColor(theme.Controls.ListBox.SelectedForeground)
        : ThemeApplicator.ParseColor(theme.Controls.ListBox.Foreground);
    
    // Draw with theme colors
    using (var brush = new SolidBrush(bgColor))
    {
        e.Graphics.FillRectangle(brush, e.Bounds);
    }
    
    // Get the item from the ListBox
    var item = ((ListBox)sender!).Items[e.Index];
    
    using (var brush = new SolidBrush(fgColor))
    {
        e.Graphics.DrawString(item.ToString(), e.Font, brush, e.Bounds);
    }
}
```

## Switching Themes Programmatically

```csharp
// Load a theme by name
bool success = ThemeManager.Instance.LoadTheme("Light");
if (success)
{
    // Theme loaded and applied to all registered forms
}

// Get list of available themes
List<string> themes = ThemeManager.Instance.GetAvailableThemes();
```

## Hot-Reload Feature

The theming system supports hot-reload, allowing themes to update without restarting the application.

### Enabling Hot-Reload

```csharp
// Enable hot-reload (disabled by default in production)
ThemeManager.Instance.SetHotReload(true);
```

### How It Works

1. FileSystemWatcher monitors `%APPDATA%/PromptArq/Themes/` directory
2. When a `.theme.json` file changes, a debounce timer (500ms) starts
3. After the delay, the theme is reloaded and validated
4. If valid and matches the current theme, all forms are automatically updated
5. If invalid, the old theme is retained and an error is logged

### Best Practices

- Enable hot-reload only during development
- Test theme changes before deploying to users
- Always validate themes before saving

## Using WindowStyleManager

The `WindowStyleManager` class has been integrated with the theming system for backward compatibility:

```csharp
// Apply dark title bar (now uses theme colors)
WindowStyleManager.ApplyDarkTitleBar(this);

// Access theme colors dynamically
Color bg = WindowStyleManager.DarkBackgroundColor;
Color fg = WindowStyleManager.LightForegroundColor;
```

These properties now return colors from the current theme automatically.

## Accessing Theme Colors and Fonts

### Get a Color

```csharp
Color backgroundColor = ThemeManager.Instance.GetColor("Background");
Color accentColor = ThemeManager.Instance.GetColor("Accent");
```

Available color names:
- `Background`, `ControlBackground`, `InputBackground`, `HeaderBackground`
- `Foreground`, `SecondaryForeground`
- `Accent`, `Border`, `Selection`

### Get a Font

```csharp
Font defaultFont = ThemeManager.Instance.GetFont("Default");
Font headingFont = ThemeManager.Instance.GetFont("Heading");
Font searchFont = ThemeManager.Instance.GetFont("SearchBox");
```

## Saving Theme Preferences

Theme preferences are automatically saved in `%APPDATA%/PromptArq/settings.json`:

```json
{
  "CurrentTheme": "DarkBlue",
  // ... other settings
}
```

The saved theme is loaded automatically on application startup.

## Built-in Themes

The application ships with three built-in themes:

1. **Dark Blue** - Dark theme with blue accents (default)
2. **Light** - Clean light theme with blue accents
3. **High Contrast** - Accessibility-focused with maximum contrast

These themes are embedded as resources and extracted to `%APPDATA%/PromptArq/Themes/` on first run.

## Troubleshooting

### Theme Not Loading

- Check that the theme file exists in `%APPDATA%/PromptArq/Themes/`
- Verify the JSON syntax is correct
- Check application logs for validation errors
- Ensure all required properties are present

### Colors Not Applying

- Verify color format is correct (`#RRGGBB`)
- For custom-drawn controls, ensure DrawItem handler uses theme colors
- Check if form is registered with ThemeManager
- Force invalidation: `form.Invalidate(true)`

### Hot-Reload Not Working

- Verify hot-reload is enabled: `ThemeManager.Instance.SetHotReload(true)`
- Check file watcher is monitoring the correct directory
- Ensure file name matches current theme
- Wait for debounce period (500ms) after saving

## API Reference

### ThemeManager

**Properties:**
- `ThemeManager.Instance` - Singleton instance
- `CurrentTheme` - Gets the currently active theme
- `EnableHotReload` - Gets/sets hot-reload status

**Methods:**
- `Initialize()` - Initializes the ThemeManager
- `GetAvailableThemes()` - Returns list of theme names
- `LoadTheme(string themeName)` - Loads a theme by name
- `RegisterForm(Form form)` - Registers a form for theme updates
- `UnregisterForm(Form form)` - Unregisters a form
- `ApplyThemeToForm(Form form)` - Applies current theme to form
- `RefreshAllForms()` - Refreshes all registered forms
- `GetColor(string colorName)` - Gets a color from current theme
- `GetFont(string fontName)` - Gets a font from current theme
- `SetHotReload(bool enabled)` - Enables/disables hot-reload

**Events:**
- `ThemeChanged` - Fires when theme changes

### ThemeApplicator

**Static Methods:**
- `ApplyToForm(Form form, Theme theme)` - Applies theme to form
- `ApplyToControl(Control control, Theme theme)` - Applies theme to control
- `ParseColor(string hexColor)` - Parses hex color to Color
- `CreateFont(FontDefinition fontDef)` - Creates font from definition

### ThemeLoader

**Static Methods:**
- `LoadFromFile(string filePath)` - Loads theme from file
- `SaveToFile(Theme theme, string filePath)` - Saves theme to file
- `GetThemeFiles(string directory)` - Gets all theme files in directory
- `GetFallbackTheme()` - Returns default fallback theme

## Examples

### Example 1: Simple Form with Theming

```csharp
public class ExampleForm : Form
{
    public ExampleForm()
    {
        Text = "Example Form";
        Size = new Size(400, 300);
        
        // Apply theming
        ThemeManager.Instance.RegisterForm(this);
        ThemeManager.Instance.ApplyThemeToForm(this);
        
        // Subscribe to changes
        ThemeManager.Instance.ThemeChanged += (s, e) =>
        {
            ThemeManager.Instance.ApplyThemeToForm(this);
        };
    }
}
```

### Example 2: Creating a Custom Theme Programmatically

```csharp
var customTheme = new Theme
{
    Name = "Midnight Purple",
    Version = "1.0",
    Colors = new ThemeColors
    {
        Background = "#1A0A2E",
        ControlBackground = "#240E3A",
        InputBackground = "#3E2555",
        Foreground = "#E8E8E8",
        Accent = "#9D4EDD"
    }
};

string themePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "PromptArq", "Themes", "MidnightPurple.theme.json"
);

ThemeLoader.SaveToFile(customTheme, themePath);
```

### Example 3: Responding to Theme Changes

```csharp
public class ResponsiveForm : Form
{
    private Label _statusLabel;
    
    public ResponsiveForm()
    {
        InitializeComponent();
        
        _statusLabel = new Label { Text = "Current theme: " };
        Controls.Add(_statusLabel);
        
        // Register and apply
        ThemeManager.Instance.RegisterForm(this);
        ThemeManager.Instance.ApplyThemeToForm(this);
        UpdateStatus();
        
        // Listen for changes
        ThemeManager.Instance.ThemeChanged += OnThemeChanged;
    }
    
    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        ThemeManager.Instance.ApplyThemeToForm(this);
        UpdateStatus();
        
        MessageBox.Show(
            $"Theme changed from '{e.OldTheme.Name}' to '{e.NewTheme.Name}'",
            "Theme Update"
        );
    }
    
    private void UpdateStatus()
    {
        _statusLabel.Text = $"Current theme: {ThemeManager.Instance.CurrentTheme.Name}";
    }
}
```

## Best Practices

1. **Always register forms** with ThemeManager for automatic updates
2. **Subscribe to ThemeChanged** event to refresh custom UI elements
3. **Use ThemeApplicator.ParseColor()** for custom drawing
4. **Test themes** with all forms before deployment
5. **Enable hot-reload** only during development
6. **Validate themes** before loading in production
7. **Provide fallbacks** for missing theme properties
8. **Document custom themes** for team members
9. **Use semantic color names** (e.g., "accent" instead of "blue")
10. **Consider accessibility** when creating themes

## Support

For issues, questions, or feature requests related to the theming system:
- Check the application logs at `%APPDATA%/PromptArq/logs/`
- Review validation errors for theme files
- Ensure theme files follow the documented format
- Verify all required properties are present
