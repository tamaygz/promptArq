using System.Drawing;

namespace PromptArqApp.Workflow.Core
{
    /// <summary>
    /// Defines different rendering templates for list items in the command palette.
    /// Templates provide consistent visual patterns while allowing customization.
    /// </summary>
    public enum ItemRenderTemplate
    {
        /// <summary>
        /// Standard template: Icon on left (spans both rows), Row 1: main text, Row 2: hint/explanation
        /// </summary>
        Standard,

        /// <summary>
        /// Badge template: Colored badge on left, Row 1: title, Row 2: description
        /// </summary>
        Badge,

        /// <summary>
        /// Simple template: Single line text, centered vertically
        /// </summary>
        Simple,

        /// <summary>
        /// Detailed template: Icon on left, Row 1: title + metadata (right-aligned), Row 2: description
        /// </summary>
        Detailed,

        /// <summary>
        /// Custom template: Node provides complete custom rendering logic
        /// </summary>
        Custom
    }

    /// <summary>
    /// Contains all data needed to render a list item.
    /// Nodes populate this structure to control how their items appear.
    /// </summary>
    public class ItemRenderData
    {
        /// <summary>
        /// Primary text displayed on the first row (required)
        /// </summary>
        public string MainText { get; set; } = "";

        /// <summary>
        /// Secondary text displayed on the second row (optional)
        /// </summary>
        public string? SecondaryText { get; set; }

        /// <summary>
        /// Icon/emoji displayed on the left (optional)
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Badge text for Badge template (e.g., "SYS", "PRJ") (optional)
        /// </summary>
        public string? BadgeText { get; set; }

        /// <summary>
        /// Badge background color for Badge template (optional)
        /// </summary>
        public Color? BadgeColor { get; set; }

        /// <summary>
        /// Custom color for the item (optional)
        /// </summary>
        public Color? ItemColor { get; set; }

        /// <summary>
        /// Metadata text displayed on the right side of row 1 for Detailed template (optional)
        /// </summary>
        public string? MetadataText { get; set; }

        /// <summary>
        /// The rendering template to use
        /// </summary>
        public ItemRenderTemplate Template { get; set; } = ItemRenderTemplate.Standard;

        /// <summary>
        /// Reference to the original item being rendered
        /// </summary>
        public object? OriginalItem { get; set; }
    }

    /// <summary>
    /// Extended interface for nodes that want to control their item rendering.
    /// This allows nodes to specify how each item should be displayed using templates.
    /// </summary>
    public interface INodeItemRenderer
    {
        /// <summary>
        /// Gets the render data for an item, specifying how it should be displayed.
        /// </summary>
        /// <param name="item">The item to get render data for.</param>
        /// <returns>ItemRenderData containing all rendering information.</returns>
        ItemRenderData GetItemRenderData(object item);

        /// <summary>
        /// Optional: Provides custom rendering for items when Template is Custom.
        /// Return false to fall back to default rendering.
        /// </summary>
        /// <param name="graphics">Graphics object for drawing.</param>
        /// <param name="bounds">Bounds rectangle for the item.</param>
        /// <param name="item">The item to render.</param>
        /// <param name="isSelected">Whether the item is currently selected.</param>
        /// <returns>True if custom rendering was performed, false to use default.</returns>
        bool CustomRenderItem(Graphics graphics, Rectangle bounds, object item, bool isSelected)
        {
            return false; // Default: no custom rendering
        }
    }
}
