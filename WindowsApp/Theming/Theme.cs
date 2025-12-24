using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;

namespace PromptArqApp.Theming
{
    /// <summary>
    /// Represents a complete theme definition including colors, fonts, and window properties.
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Theme name displayed to users
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; } = "Default";

        /// <summary>
        /// Theme version for compatibility tracking
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Color definitions for the theme
        /// </summary>
        [JsonProperty("colors")]
        public ThemeColors Colors { get; set; } = new ThemeColors();

        /// <summary>
        /// Font definitions for the theme
        /// </summary>
        [JsonProperty("fonts")]
        public ThemeFonts Fonts { get; set; } = new ThemeFonts();

        /// <summary>
        /// Window-specific properties
        /// </summary>
        [JsonProperty("window")]
        public WindowProperties Window { get; set; } = new WindowProperties();

        /// <summary>
        /// Control-specific theme definitions
        /// </summary>
        [JsonProperty("controls")]
        public ControlThemes Controls { get; set; } = new ControlThemes();

        /// <summary>
        /// Creates a deep clone of the theme
        /// </summary>
        public Theme Clone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Theme>(json) ?? new Theme();
        }

        /// <summary>
        /// Validates the theme and returns a list of validation errors
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
            {
                errors.Add("Theme name is required");
            }

            if (string.IsNullOrWhiteSpace(Version))
            {
                errors.Add("Theme version is required");
            }

            // Validate all color properties
            errors.AddRange(Colors.Validate());

            return errors;
        }
    }

    /// <summary>
    /// Color definitions for various UI elements
    /// </summary>
    public class ThemeColors
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "#1E1E1E";

        [JsonProperty("controlBackground")]
        public string ControlBackground { get; set; } = "#232323";

        [JsonProperty("inputBackground")]
        public string InputBackground { get; set; } = "#323232";

        [JsonProperty("headerBackground")]
        public string HeaderBackground { get; set; } = "#282828";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "#FFFFFF";

        [JsonProperty("secondaryForeground")]
        public string SecondaryForeground { get; set; } = "#808080";

        [JsonProperty("accent")]
        public string Accent { get; set; } = "#3C7FB1";

        [JsonProperty("border")]
        public string Border { get; set; } = "#3C3C3C";

        [JsonProperty("selection")]
        public string Selection { get; set; } = "#3C78B4";

        /// <summary>
        /// Validates all color hex strings
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            var properties = GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(this) as string;
                    if (!string.IsNullOrEmpty(value) && !IsValidHexColor(value))
                    {
                        errors.Add($"Invalid color format for {prop.Name}: {value}");
                    }
                }
            }

            return errors;
        }

        private static bool IsValidHexColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return false;

            // Support #RGB, #RRGGBB, #AARRGGBB formats
            return System.Text.RegularExpressions.Regex.IsMatch(
                color,
                @"^#([A-Fa-f0-9]{3}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$"
            );
        }
    }

    /// <summary>
    /// Font definitions for different UI contexts
    /// </summary>
    public class ThemeFonts
    {
        [JsonProperty("default")]
        public FontDefinition Default { get; set; } = new FontDefinition
        {
            Family = "Segoe UI",
            Size = 10,
            Style = "Regular"
        };

        [JsonProperty("heading")]
        public FontDefinition Heading { get; set; } = new FontDefinition
        {
            Family = "Segoe UI",
            Size = 14,
            Style = "Bold"
        };

        [JsonProperty("searchBox")]
        public FontDefinition SearchBox { get; set; } = new FontDefinition
        {
            Family = "Segoe UI",
            Size = 16,
            Style = "Regular"
        };
    }

    /// <summary>
    /// Font definition with family, size, and style
    /// </summary>
    public class FontDefinition
    {
        [JsonProperty("family")]
        public string Family { get; set; } = "Segoe UI";

        [JsonProperty("size")]
        public float Size { get; set; } = 10;

        [JsonProperty("style")]
        public string Style { get; set; } = "Regular";

        /// <summary>
        /// Converts the font definition to a System.Drawing.Font
        /// </summary>
        public Font ToFont()
        {
            FontStyle fontStyle;
            switch (Style?.ToLower())
            {
                case "bold":
                    fontStyle = FontStyle.Bold;
                    break;
                case "italic":
                    fontStyle = FontStyle.Italic;
                    break;
                case "bold italic":
                case "bolditalic":
                    fontStyle = FontStyle.Bold | FontStyle.Italic;
                    break;
                case "underline":
                    fontStyle = FontStyle.Underline;
                    break;
                case "strikeout":
                    fontStyle = FontStyle.Strikeout;
                    break;
                default:
                    fontStyle = FontStyle.Regular;
                    break;
            }

            try
            {
                return new Font(Family, Size, fontStyle);
            }
            catch
            {
                // Fallback to default font if specified font is not available
                return new Font("Segoe UI", Size, fontStyle);
            }
        }
    }

    /// <summary>
    /// Window-specific properties like opacity and corner radius
    /// </summary>
    public class WindowProperties
    {
        [JsonProperty("opacity")]
        public double Opacity { get; set; } = 0.97;

        [JsonProperty("cornerRadius")]
        public int CornerRadius { get; set; } = 15;

        [JsonProperty("titleBarColor")]
        public string TitleBarColor { get; set; } = "#00663300";

        [JsonProperty("borderColor")]
        public string BorderColor { get; set; } = "#00663300";
    }

    /// <summary>
    /// Control-specific theme definitions
    /// </summary>
    public class ControlThemes
    {
        [JsonProperty("button")]
        public ButtonTheme Button { get; set; } = new ButtonTheme();

        [JsonProperty("listBox")]
        public ListBoxTheme ListBox { get; set; } = new ListBoxTheme();

        [JsonProperty("textBox")]
        public TextBoxTheme TextBox { get; set; } = new TextBoxTheme();
    }

    /// <summary>
    /// Button control theme
    /// </summary>
    public class ButtonTheme
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "#323232";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "#FFFFFF";

        [JsonProperty("hoverBackground")]
        public string HoverBackground { get; set; } = "#3C78B4";
    }

    /// <summary>
    /// ListBox control theme
    /// </summary>
    public class ListBoxTheme
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "#232323";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "#D3D3D3";

        [JsonProperty("selectedBackground")]
        public string SelectedBackground { get; set; } = "#3C78B4";

        [JsonProperty("selectedForeground")]
        public string SelectedForeground { get; set; } = "#FFFFFF";
    }

    /// <summary>
    /// TextBox control theme
    /// </summary>
    public class TextBoxTheme
    {
        [JsonProperty("background")]
        public string Background { get; set; } = "#323232";

        [JsonProperty("foreground")]
        public string Foreground { get; set; } = "#FFFFFF";
    }
}
