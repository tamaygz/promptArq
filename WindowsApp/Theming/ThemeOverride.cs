namespace PromptArqApp.Theming
{
    /// <summary>
    /// Allows controls to override default theme settings by storing this in their Tag property.
    /// The ThemeApplicator will respect these overrides when applying themes.
    /// </summary>
    public class ThemeOverride
    {
        /// <summary>
        /// The font type to use instead of the default.
        /// Valid values: "Default", "Heading", "SearchBox"
        /// </summary>
        public string? FontType { get; set; }

        /// <summary>
        /// Creates a theme override with the specified font type.
        /// </summary>
        /// <param name="fontType">The font type to use (e.g., "Heading", "SearchBox")</param>
        public static ThemeOverride WithFont(string fontType)
        {
            return new ThemeOverride { FontType = fontType };
        }
    }
}
