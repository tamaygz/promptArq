# WindowsApp Theming System - Implementation Plan

## Overview
This document outlines the comprehensive implementation plan for adding dynamic theming support to the PromptArq WindowsApp.

## Executive Summary

### Objectives
- Enable developers to define themes in human-readable JSON files
- Support runtime theme switching without application restart
- Apply themes to existing and new forms with 1-2 lines of code
- Provide hot-reload capability for theme development
- Include 3 example themes (Dark Blue, Light, High Contrast)

### Success Criteria
✅ Single JSON file per theme  
✅ 1-2 lines of code to apply theme to a form  
✅ Runtime theme switching works reliably  
✅ Clear developer documentation  
✅ 3 example themes provided  
✅ Hot-reload support for theme files  
✅ Works with all existing forms (MainForm, CommandPaletteForm, SettingsForm, TextDisplayPanel)

---

## Architecture Design

### Core Components

#### 1. Theme Model (Theme.cs)
**Purpose:** Define the data structure for theme files

**Classes:**
```csharp
public class Theme
{
    public string Name { get; set; }
    public string Version { get; set; }
    public ThemeColors Colors { get; set; }
    public ThemeFonts Fonts { get; set; }
    public WindowProperties Window { get; set; }
    public ControlThemes Controls { get; set; }
}

public class ThemeColors
{
    public string Background { get; set; }           // #1E1E1E
    public string ControlBackground { get; set; }    // #232323
    public string InputBackground { get; set; }      // #323232
    public string HeaderBackground { get; set; }     // #282828
    public string Foreground { get; set; }           // #FFFFFF
    public string SecondaryForeground { get; set; }  // #808080
    public string Accent { get; set; }               // #3C7FB1
    public string Border { get; set; }               // #3C3C3C
    public string Selection { get; set; }            // #3C78B4
}

public class ThemeFonts
{
    public FontDefinition Default { get; set; }
    public FontDefinition Heading { get; set; }
    public FontDefinition SearchBox { get; set; }
}

public class FontDefinition
{
    public string Family { get; set; }
    public float Size { get; set; }
    public string Style { get; set; }  // "Regular", "Bold", "Italic"
}

public class WindowProperties
{
    public double Opacity { get; set; }
    public int CornerRadius { get; set; }
    public string TitleBarColor { get; set; }  // BGR format: #00663300
    public string BorderColor { get; set; }
}

public class ControlThemes
{
    public ButtonTheme Button { get; set; }
    public ListBoxTheme ListBox { get; set; }
    public TextBoxTheme TextBox { get; set; }
}

public class ButtonTheme
{
    public string Background { get; set; }
    public string Foreground { get; set; }
    public string HoverBackground { get; set; }
}

public class ListBoxTheme
{
    public string Background { get; set; }
    public string Foreground { get; set; }
    public string SelectedBackground { get; set; }
    public string SelectedForeground { get; set; }
}
```

**Features:**
- JSON serialization/deserialization
- Color format validation (hex strings)
- Default value handling
- Clone() method for theme modification

---

#### 2. ThemeManager (ThemeManager.cs)
**Purpose:** Singleton service for managing themes globally

**API:**
```csharp
public sealed class ThemeManager : IDisposable
{
    // Singleton instance
    public static ThemeManager Instance { get; }
    
    // Current active theme
    public Theme CurrentTheme { get; private set; }
    
    // Events
    public event EventHandler<ThemeChangedEventArgs> ThemeChanged;
    
    // Initialization
    public static void Initialize();
    
    // Theme management
    public List<Theme> GetAvailableThemes();
    public bool LoadTheme(string themeName);
    public void ReloadCurrentTheme();
    
    // Application
    public void ApplyThemeToForm(Form form);
    public void RegisterForm(Form form);
    public void UnregisterForm(Form form);
    public void RefreshAllForms();
    
    // Utilities
    public Color GetColor(string colorName);
    public Font GetFont(string fontName);
    
    // Hot-reload
    public bool EnableHotReload { get; set; }
}
```

**Responsibilities:**
- Load themes from %APPDATA%/PromptArq/Themes/
- Maintain list of registered forms
- Notify forms of theme changes
- Coordinate hot-reload via FileSystemWatcher
- Integrate with AppSettings for persistence

**Thread Safety:**
- Lock on theme changes
- Thread-safe event firing
- Synchronization for form registry

---

#### 3. ThemeApplicator (ThemeApplicator.cs)
**Purpose:** Apply themes to forms and controls

**API:**
```csharp
public static class ThemeApplicator
{
    // Primary methods
    public static void ApplyToForm(Form form, Theme theme);
    public static void ApplyToControl(Control control, Theme theme);
    
    // Specialized applicators
    private static void ApplyToButton(Button button, Theme theme);
    private static void ApplyToTextBox(TextBox textBox, Theme theme);
    private static void ApplyToListBox(ListBox listBox, Theme theme);
    private static void ApplyToRichTextBox(RichTextBox richTextBox, Theme theme);
    private static void ApplyToDataGridView(DataGridView dgv, Theme theme);
    private static void ApplyToLabel(Label label, Theme theme);
    private static void ApplyToPanel(Panel panel, Theme theme);
    
    // Utilities
    private static void ApplyRecursively(Control control, Theme theme);
    private static Color ParseColor(string hexColor);
    private static Font CreateFont(FontDefinition fontDef);
}
```

**Algorithm:**
1. Start with form-level properties (BackColor, Font, Opacity)
2. Recursively walk control tree
3. Match control type to specialized applicator
4. Apply theme properties to control
5. Handle special cases (OwnerDraw controls, custom painting)
6. Trigger control refresh/invalidation

**Special Cases:**
- **ListBox with OwnerDraw:** Update DrawItem event handler colors
- **RichTextBox:** Apply SelectionColor, ensure BackColor persists
- **DataGridView:** Theme all cell styles, headers, borders
- **Custom-drawn panels:** Force invalidation to trigger Paint events

---

#### 4. ThemeLoader (ThemeLoader.cs)
**Purpose:** Handle theme file I/O and validation

**API:**
```csharp
public static class ThemeLoader
{
    // Load/Save
    public static Theme LoadFromFile(string filePath);
    public static void SaveToFile(Theme theme, string filePath);
    
    // Discovery
    public static List<string> GetThemeFiles(string directory);
    public static List<Theme> LoadAllThemes(string directory);
    
    // Validation
    public static bool ValidateTheme(Theme theme, out List<string> errors);
    
    // Defaults
    public static void ExtractDefaultThemes(string targetDirectory);
    public static Theme GetFallbackTheme();
}
```

**Error Handling:**
- JSON parsing errors → fallback to default theme
- Missing properties → use default values
- Invalid colors → use fallback color (#FFFFFF)
- File access errors → log and continue

---

### Integration Points

#### WindowStyleManager.cs Refactoring
**Current state:** Static color constants  
**New state:** Dynamic properties backed by ThemeManager

```csharp
public static class WindowStyleManager
{
    // OLD: Static constants
    // public static readonly Color DarkBackgroundColor = Color.FromArgb(30, 30, 30);
    
    // NEW: Dynamic properties
    public static Color DarkBackgroundColor => ThemeManager.Instance.GetColor("Background");
    public static Color DarkControlBackgroundColor => ThemeManager.Instance.GetColor("ControlBackground");
    public static Color DarkInputBackgroundColor => ThemeManager.Instance.GetColor("InputBackground");
    // ... etc
    
    // Backward compatibility maintained
    public static void ApplyDarkTheme(Form form, bool applyRoundedCorners = true)
    {
        ThemeManager.Instance.ApplyThemeToForm(form);
        if (applyRoundedCorners && form.FormBorderStyle == FormBorderStyle.None)
        {
            ApplyRoundedCorners(form, ThemeManager.Instance.CurrentTheme.Window.CornerRadius);
        }
    }
}
```

**Benefits:**
- Existing code continues to work
- Colors automatically update when theme changes
- No breaking changes

---

#### AppSettings.cs Updates
**Add property:**
```csharp
public string CurrentTheme { get; set; } = "Dark Blue";
```

**Integration:**
```csharp
// In MainForm constructor (after ThemeManager.Initialize())
var settings = AppSettings.Load();
ThemeManager.Instance.LoadTheme(settings.CurrentTheme);
```

---

#### SettingsForm.cs Enhancements
**Add UI elements:**
- ComboBox for theme selection
- Preview button (optional)
- Apply button (reload theme without restart)

**Layout changes:**
```
[Hotkey Configuration]
[...]
[Command Palette Features]
[...]
[Appearance Settings]      <-- NEW SECTION
  Theme: [Dropdown]
  [Preview] [Apply]
```

**Code:**
```csharp
private ComboBox _themeSelector;

private void InitializeComponent()
{
    // ... existing code ...
    
    var appearanceLabel = new Label
    {
        Text = "Appearance Settings",
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Location = new Point(20, 480),
        AutoSize = true
    };
    Controls.Add(appearanceLabel);
    
    var themeLabel = new Label
    {
        Text = "Theme:",
        Location = new Point(20, 510),
        AutoSize = true
    };
    Controls.Add(themeLabel);
    
    _themeSelector = new ComboBox
    {
        Location = new Point(80, 507),
        Size = new Size(200, 25),
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    _themeSelector.Items.AddRange(ThemeManager.Instance.GetAvailableThemes()
        .Select(t => t.Name).ToArray());
    _themeSelector.SelectedItem = _settings.CurrentTheme;
    Controls.Add(_themeSelector);
}

private void SaveButton_Click(object? sender, EventArgs e)
{
    // ... existing code ...
    
    // Save theme preference
    _settings.CurrentTheme = _themeSelector.SelectedItem?.ToString() ?? "Dark Blue";
    
    // Apply new theme
    if (ThemeManager.Instance.CurrentTheme.Name != _settings.CurrentTheme)
    {
        ThemeManager.Instance.LoadTheme(_settings.CurrentTheme);
        ThemeManager.Instance.RefreshAllForms();
    }
    
    _settings.Save();
}
```

---

## Theme File Format

### JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "PromptArq Theme",
  "type": "object",
  "required": ["name", "version", "colors", "fonts", "window"],
  "properties": {
    "name": {
      "type": "string",
      "description": "Display name of the theme"
    },
    "version": {
      "type": "string",
      "description": "Theme version (e.g., '1.0')"
    },
    "colors": {
      "type": "object",
      "required": ["background", "foreground"],
      "properties": {
        "background": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "controlBackground": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "inputBackground": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "headerBackground": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "foreground": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "secondaryForeground": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "accent": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "border": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" },
        "selection": { "type": "string", "pattern": "^#[0-9A-Fa-f]{6}$" }
      }
    },
    "fonts": {
      "type": "object",
      "properties": {
        "default": { "$ref": "#/definitions/font" },
        "heading": { "$ref": "#/definitions/font" },
        "searchBox": { "$ref": "#/definitions/font" }
      }
    },
    "window": {
      "type": "object",
      "properties": {
        "opacity": { "type": "number", "minimum": 0, "maximum": 1 },
        "cornerRadius": { "type": "integer", "minimum": 0 },
        "titleBarColor": { "type": "string", "pattern": "^#[0-9A-Fa-f]{8}$" },
        "borderColor": { "type": "string", "pattern": "^#[0-9A-Fa-f]{8}$" }
      }
    },
    "controls": {
      "type": "object",
      "properties": {
        "button": { "$ref": "#/definitions/buttonTheme" },
        "listBox": { "$ref": "#/definitions/listBoxTheme" }
      }
    }
  },
  "definitions": {
    "font": {
      "type": "object",
      "properties": {
        "family": { "type": "string" },
        "size": { "type": "number" },
        "style": { "type": "string", "enum": ["Regular", "Bold", "Italic", "BoldItalic"] }
      }
    },
    "buttonTheme": {
      "type": "object",
      "properties": {
        "background": { "type": "string" },
        "foreground": { "type": "string" },
        "hoverBackground": { "type": "string" }
      }
    },
    "listBoxTheme": {
      "type": "object",
      "properties": {
        "background": { "type": "string" },
        "foreground": { "type": "string" },
        "selectedBackground": { "type": "string" },
        "selectedForeground": { "type": "string" }
      }
    }
  }
}
```

---

### Example Themes

#### DarkBlue.theme.json (Current default)
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
    }
  }
}
```

#### Light.theme.json
```json
{
  "name": "Light",
  "version": "1.0",
  "colors": {
    "background": "#FFFFFF",
    "controlBackground": "#F5F5F5",
    "inputBackground": "#FFFFFF",
    "headerBackground": "#E8E8E8",
    "foreground": "#000000",
    "secondaryForeground": "#666666",
    "accent": "#0078D4",
    "border": "#CCCCCC",
    "selection": "#0078D4"
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
    "titleBarColor": "#00FFFFFF",
    "borderColor": "#00CCCCCC"
  },
  "controls": {
    "button": {
      "background": "#E1E1E1",
      "foreground": "#000000",
      "hoverBackground": "#0078D4"
    },
    "listBox": {
      "background": "#FFFFFF",
      "foreground": "#000000",
      "selectedBackground": "#0078D4",
      "selectedForeground": "#FFFFFF"
    }
  }
}
```

#### HighContrast.theme.json
```json
{
  "name": "High Contrast",
  "version": "1.0",
  "colors": {
    "background": "#000000",
    "controlBackground": "#000000",
    "inputBackground": "#000000",
    "headerBackground": "#000000",
    "foreground": "#FFFFFF",
    "secondaryForeground": "#00FF00",
    "accent": "#FFFF00",
    "border": "#FFFFFF",
    "selection": "#FFFF00"
  },
  "fonts": {
    "default": {
      "family": "Segoe UI",
      "size": 12,
      "style": "Bold"
    },
    "heading": {
      "family": "Segoe UI",
      "size": 16,
      "style": "Bold"
    },
    "searchBox": {
      "family": "Segoe UI",
      "size": 18,
      "style": "Bold"
    }
  },
  "window": {
    "opacity": 1.0,
    "cornerRadius": 0,
    "titleBarColor": "#00000000",
    "borderColor": "#00FFFFFF"
  },
  "controls": {
    "button": {
      "background": "#000000",
      "foreground": "#FFFFFF",
      "hoverBackground": "#FFFF00"
    },
    "listBox": {
      "background": "#000000",
      "foreground": "#FFFFFF",
      "selectedBackground": "#FFFF00",
      "selectedForeground": "#000000"
    }
  }
}
```

---

## Implementation Phases

### Phase 1: Core Infrastructure (Foundation)
**Files to create:**
- `WindowsApp/Theming/Theme.cs`
- `WindowsApp/Theming/ThemeManager.cs`
- `WindowsApp/Theming/ThemeApplicator.cs`
- `WindowsApp/Theming/ThemeLoader.cs`
- `WindowsApp/Themes/DarkBlue.theme.json`
- `WindowsApp/Themes/Light.theme.json`
- `WindowsApp/Themes/HighContrast.theme.json`

**Tasks:**
1. ✅ Create Theme.cs with all model classes
2. ✅ Implement JSON serialization/deserialization
3. ✅ Create default theme JSON files
4. ✅ Implement ThemeLoader with file I/O
5. ✅ Add theme validation logic
6. ✅ Test theme loading from files

**Estimated effort:** 6-8 hours

---

### Phase 2: ThemeManager Service
**Files to create/modify:**
- `WindowsApp/Theming/ThemeManager.cs`
- `WindowsApp/AppSettings.cs` (modify)
- `WindowsApp/Program.cs` (modify)

**Tasks:**
1. ✅ Implement singleton pattern
2. ✅ Add theme discovery and loading
3. ✅ Create form registry system
4. ✅ Implement ThemeChanged event
5. ✅ Add %APPDATA%/PromptArq/Themes/ directory creation
6. ✅ Integrate with AppSettings
7. ✅ Add initialization to Program.cs
8. ✅ Test theme switching

**Estimated effort:** 4-6 hours

---

### Phase 3: Theme Application Logic
**Files to create/modify:**
- `WindowsApp/Theming/ThemeApplicator.cs`
- `WindowsApp/WindowStyleManager.cs` (refactor)

**Tasks:**
1. ✅ Implement ApplyToForm() method
2. ✅ Implement ApplyToControl() with recursion
3. ✅ Create specialized applicators for each control type
4. ✅ Refactor WindowStyleManager to use ThemeManager
5. ✅ Add color/font utility methods
6. ✅ Handle special cases (OwnerDraw, RichTextBox, etc.)
7. ✅ Test on simple form

**Estimated effort:** 8-10 hours

---

### Phase 4: Form Integration
**Files to modify:**
- `WindowsApp/MainForm.cs`
- `WindowsApp/CommandPaletteForm.cs`
- `WindowsApp/SettingsForm.cs`
- `WindowsApp/TextDisplayPanel.cs`

**Tasks:**
1. ✅ Update MainForm constructor with theming
2. ✅ Update CommandPaletteForm (complex custom drawing)
3. ✅ Update SettingsForm
4. ✅ Update TextDisplayPanel
5. ✅ Subscribe all forms to ThemeChanged event
6. ✅ Test theme switching on all forms
7. ✅ Fix custom drawing issues in CommandPaletteForm

**Estimated effort:** 6-8 hours

---

### Phase 5: SettingsForm UI
**Files to modify:**
- `WindowsApp/SettingsForm.cs`

**Tasks:**
1. ✅ Add theme selector ComboBox
2. ✅ Populate with available themes
3. ✅ Wire up save logic
4. ✅ Add Apply button for instant preview
5. ✅ Test UI integration
6. ✅ Handle edge cases (missing themes, invalid selections)

**Estimated effort:** 3-4 hours

---

### Phase 6: Hot-Reload
**Files to modify:**
- `WindowsApp/Theming/ThemeManager.cs`

**Tasks:**
1. ✅ Add FileSystemWatcher
2. ✅ Implement file change detection
3. ✅ Add debouncing for multiple rapid changes
4. ✅ Trigger theme reload on file change
5. ✅ Implement EnableHotReload property
6. ✅ Test hot-reload functionality
7. ✅ Add error handling for corrupted files during reload

**Estimated effort:** 3-4 hours

---

### Phase 7: Documentation
**Files to create:**
- `WindowsApp/docs/ThemeGuide.md`
- Update `WindowsApp/README.md`

**Tasks:**
1. ✅ Write comprehensive ThemeGuide.md
2. ✅ Document JSON schema
3. ✅ Provide code examples
4. ✅ Document color format requirements
5. ✅ Add troubleshooting section
6. ✅ Update README with theming info
7. ✅ Add XML documentation comments to public APIs

**Estimated effort:** 4-5 hours

---

### Phase 8: Testing & Polish
**Tasks:**
1. ✅ Test all 3 default themes
2. ✅ Test theme switching in all forms
3. ✅ Test hot-reload
4. ✅ Test with invalid theme files
5. ✅ Test with missing theme files
6. ✅ Performance testing (theme application speed)
7. ✅ Memory leak testing (form registration/unregistration)
8. ✅ Fix bugs and edge cases

**Estimated effort:** 4-6 hours

---

## Total Estimated Effort
**40-50 hours** across 8 phases

---

## Success Metrics

### Functional Requirements
- ✅ Theme defined in single JSON file
- ✅ 1-2 lines of code to apply theme to form
- ✅ Runtime theme switching works
- ✅ Hot-reload works
- ✅ 3 example themes provided
- ✅ Works with all existing forms

### Non-Functional Requirements
- ✅ Theme application < 100ms per form
- ✅ No memory leaks in form registry
- ✅ Backward compatibility maintained
- ✅ Clear error messages for invalid themes
- ✅ Comprehensive documentation

### Developer Experience
- ✅ Easy to create new themes
- ✅ Easy to apply themes to forms
- ✅ Easy to customize theme properties
- ✅ Good error messages
- ✅ Example code provided

---

## Risk Mitigation

### Risk: Breaking existing forms
**Mitigation:** Refactor WindowStyleManager to maintain backward compatibility. Existing code continues to work.

### Risk: Performance issues with recursive control theming
**Mitigation:** Optimize recursion with early exits. Cache theme colors. Profile and measure.

### Risk: Custom-drawn controls don't update correctly
**Mitigation:** Special handling for OwnerDraw controls. Force invalidation. Test extensively.

### Risk: Theme files get corrupted
**Mitigation:** Validation on load. Fallback to default theme. Backup mechanism.

### Risk: Hot-reload causes flickering
**Mitigation:** Debounce FileSystemWatcher. Batch updates. SuspendLayout/ResumeLayout.

---

## Future Enhancements (Out of Scope for v1)

1. **Theme Editor GUI**
   - Visual theme builder
   - Live preview
   - Export to JSON

2. **Advanced Features**
   - Theme inheritance (extend base themes)
   - Per-control overrides
   - Animation/transitions
   - Theme variants (light/dark mode)

3. **Community Features**
   - Theme marketplace
   - Import from URL
   - Theme sharing
   - Rating system

4. **Accessibility**
   - System theme detection
   - High contrast mode auto-detection
   - Screen reader optimizations
   - Colorblind-friendly palettes

---

## References

### External Resources
- [Windows Forms Custom Drawing](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-create-a-windows-forms-control-that-shows-progress)
- [DWM API for Title Bars](https://docs.microsoft.com/en-us/windows/win32/api/dwmapi/)
- [JSON Schema Specification](https://json-schema.org/)
- [Newtonsoft.Json Documentation](https://www.newtonsoft.com/json/help/html/Introduction.htm)

### Internal Resources
- WindowsApp/AGENTS.md - Agent guidance
- WindowsApp/README.md - Project overview
- WindowsApp/WindowStyleManager.cs - Current styling approach

---

## Conclusion

This implementation plan provides a comprehensive, phased approach to adding dynamic theming to the WindowsApp. The design prioritizes:

1. **Developer Experience** - 1-2 lines of code, clear APIs
2. **Flexibility** - JSON-based, extensible schema
3. **Performance** - Efficient application, caching
4. **Reliability** - Validation, error handling, fallbacks
5. **Maintainability** - Clear separation of concerns, good documentation

The phased approach allows for incremental development and testing, reducing risk and ensuring each component works correctly before moving to the next phase.
