using System;
using System.Drawing;
using PromptArqApp.Theming;
using PromptArqApp.Workflow.Core;

namespace PromptArqApp.Workflow.UI
{
    /// <summary>
    /// Renders list items based on ItemRenderData and templates.
    /// This is part of the workflow system's UI layer, providing consistent
    /// rendering across all nodes while allowing customization.
    /// </summary>
    public class WorkflowItemRenderer
    {
        private readonly Theme _theme;

        public WorkflowItemRenderer(Theme theme)
        {
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        }

        /// <summary>
        /// Renders an item using the node's render data and template.
        /// </summary>
        /// <param name="g">Graphics object for drawing.</param>
        /// <param name="bounds">Bounds rectangle for the item.</param>
        /// <param name="renderData">The render data from the node.</param>
        /// <param name="isSelected">Whether the item is currently selected.</param>
        public void RenderItem(Graphics g, Rectangle bounds, ItemRenderData renderData, bool isSelected)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (renderData == null) throw new ArgumentNullException(nameof(renderData));

            switch (renderData.Template)
            {
                case ItemRenderTemplate.Standard:
                    RenderStandardTemplate(g, bounds, renderData, isSelected);
                    break;

                case ItemRenderTemplate.Badge:
                    RenderBadgeTemplate(g, bounds, renderData, isSelected);
                    break;

                case ItemRenderTemplate.Simple:
                    RenderSimpleTemplate(g, bounds, renderData, isSelected);
                    break;

                case ItemRenderTemplate.Detailed:
                    RenderDetailedTemplate(g, bounds, renderData, isSelected);
                    break;

                case ItemRenderTemplate.Custom:
                    // Custom rendering should be handled by the node itself via INodeItemRenderer.CustomRenderItem
                    // Fall back to standard if not handled
                    RenderStandardTemplate(g, bounds, renderData, isSelected);
                    break;

                default:
                    RenderStandardTemplate(g, bounds, renderData, isSelected);
                    break;
            }
        }

        /// <summary>
        /// Standard template: Icon on left (spans both rows), Row 1: main text, Row 2: hint/explanation
        /// </summary>
        private void RenderStandardTemplate(Graphics g, Rectangle bounds, ItemRenderData data, bool isSelected)
        {
            var textColor = GetTextColor(isSelected);
            var subTextColor = GetSecondaryTextColor();

            const int leftMargin = 15;
            const int iconSize = 30;
            int textLeft = leftMargin;

            // Draw icon if provided (left side, vertically centered, spans both rows)
            if (!string.IsNullOrEmpty(data.Icon))
            {
                using (var iconFont = new Font(_theme.Fonts.SearchBox.Family, 16F))
                using (var brush = new SolidBrush(data.ItemColor ?? textColor))
                {
                    var iconRect = new Rectangle(
                        bounds.X + leftMargin,
                        bounds.Y + (bounds.Height - iconSize) / 2,
                        iconSize,
                        iconSize
                    );
                    g.DrawString(data.Icon, iconFont, brush, iconRect, new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
                textLeft = leftMargin + iconSize + 15;
            }

            // Draw main text (row 1)
            using (var mainFont = new Font(_theme.Fonts.Default.Family, 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(
                    bounds.X + textLeft,
                    bounds.Y + 8,
                    bounds.Width - textLeft - 15,
                    20
                );
                g.DrawString(data.MainText, mainFont, brush, textRect, new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter
                });
            }

            // Draw secondary text (row 2) if provided
            if (!string.IsNullOrEmpty(data.SecondaryText))
            {
                using (var secondaryFont = new Font(_theme.Fonts.Default.Family, 9F, FontStyle.Regular))
                using (var brush = new SolidBrush(subTextColor))
                {
                    var textRect = new Rectangle(
                        bounds.X + textLeft,
                        bounds.Y + 30,
                        bounds.Width - textLeft - 15,
                        18
                    );
                    g.DrawString(data.SecondaryText, secondaryFont, brush, textRect, new StringFormat
                    {
                        Trimming = StringTrimming.EllipsisCharacter
                    });
                }
            }
        }

        /// <summary>
        /// Badge template: Colored badge on left, Row 1: title, Row 2: description
        /// </summary>
        private void RenderBadgeTemplate(Graphics g, Rectangle bounds, ItemRenderData data, bool isSelected)
        {
            var textColor = GetTextColor(isSelected);
            var subTextColor = GetSecondaryTextColor();

            const int leftMargin = 10;
            const int badgeWidth = 40;
            const int badgeHeight = 20;
            int textLeft = leftMargin + badgeWidth + 10;

            // Draw badge (left side, vertically centered)
            var badgeRect = new Rectangle(
                bounds.X + leftMargin,
                bounds.Y + (bounds.Height - badgeHeight) / 2,
                badgeWidth,
                badgeHeight
            );
            var badgeColor = data.BadgeColor ?? Color.FromArgb(100, 150, 200);
            using (var brush = new SolidBrush(badgeColor))
            {
                g.FillRectangle(brush, badgeRect);
            }

            // Draw badge text
            if (!string.IsNullOrEmpty(data.BadgeText))
            {
                using (var badgeFont = new Font(_theme.Fonts.Default.Family, 8F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    g.DrawString(data.BadgeText, badgeFont, brush, badgeRect, new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
            }

            // Draw main text (row 1)
            using (var mainFont = new Font(_theme.Fonts.Default.Family, 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(
                    bounds.X + textLeft,
                    bounds.Y + 8,
                    bounds.Width - textLeft - 15,
                    20
                );
                g.DrawString(data.MainText, mainFont, brush, textRect, new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter
                });
            }

            // Draw secondary text (row 2)
            if (!string.IsNullOrEmpty(data.SecondaryText))
            {
                using (var secondaryFont = new Font(_theme.Fonts.Default.Family, 9F, FontStyle.Regular))
                using (var brush = new SolidBrush(subTextColor))
                {
                    var textRect = new Rectangle(
                        bounds.X + textLeft,
                        bounds.Y + 28,
                        bounds.Width - textLeft - 15,
                        18
                    );
                    g.DrawString(data.SecondaryText, secondaryFont, brush, textRect, new StringFormat
                    {
                        Trimming = StringTrimming.EllipsisCharacter
                    });
                }
            }
        }

        /// <summary>
        /// Simple template: Single line text, centered vertically
        /// </summary>
        private void RenderSimpleTemplate(Graphics g, Rectangle bounds, ItemRenderData data, bool isSelected)
        {
            var textColor = GetTextColor(isSelected, useFullForeground: true);

            using (var font = _theme.Fonts.Default.ToFont())
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(
                    bounds.X + 15,
                    bounds.Y,
                    bounds.Width - 30,
                    bounds.Height
                );
                g.DrawString(data.MainText, font, brush, textRect, new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                });
            }
        }

        /// <summary>
        /// Detailed template: Icon on left, Row 1: title + metadata (right-aligned), Row 2: description
        /// </summary>
        private void RenderDetailedTemplate(Graphics g, Rectangle bounds, ItemRenderData data, bool isSelected)
        {
            var textColor = GetTextColor(isSelected);
            var subTextColor = GetSecondaryTextColor();
            var metadataColor = GetSecondaryTextColor();

            const int leftMargin = 15;
            const int rightMargin = 15;
            const int iconSize = 30;
            int textLeft = leftMargin;

            // Draw icon if provided (left side, vertically centered)
            if (!string.IsNullOrEmpty(data.Icon))
            {
                using (var iconFont = new Font(_theme.Fonts.SearchBox.Family, 16F))
                using (var brush = new SolidBrush(data.ItemColor ?? textColor))
                {
                    var iconRect = new Rectangle(
                        bounds.X + leftMargin,
                        bounds.Y + (bounds.Height - iconSize) / 2,
                        iconSize,
                        iconSize
                    );
                    g.DrawString(data.Icon, iconFont, brush, iconRect, new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    });
                }
                textLeft = leftMargin + iconSize + 15;
            }

            // Measure metadata text to calculate main text available width
            int metadataWidth = 0;
            if (!string.IsNullOrEmpty(data.MetadataText))
            {
                using (var metadataFont = new Font(_theme.Fonts.Default.Family, 9F, FontStyle.Regular))
                {
                    var metadataSize = g.MeasureString(data.MetadataText, metadataFont);
                    metadataWidth = (int)metadataSize.Width + 10; // Add padding
                }
            }

            // Draw main text (row 1) - left side
            using (var mainFont = new Font(_theme.Fonts.Default.Family, 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(
                    bounds.X + textLeft,
                    bounds.Y + 8,
                    bounds.Width - textLeft - metadataWidth - rightMargin,
                    20
                );
                g.DrawString(data.MainText, mainFont, brush, textRect, new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter
                });
            }

            // Draw metadata text (row 1) - right-aligned
            if (!string.IsNullOrEmpty(data.MetadataText))
            {
                using (var metadataFont = new Font(_theme.Fonts.Default.Family, 9F, FontStyle.Regular))
                using (var brush = new SolidBrush(metadataColor))
                {
                    var metadataRect = new Rectangle(
                        bounds.X + bounds.Width - metadataWidth - rightMargin,
                        bounds.Y + 10,
                        metadataWidth,
                        18
                    );
                    g.DrawString(data.MetadataText, metadataFont, brush, metadataRect, new StringFormat
                    {
                        Alignment = StringAlignment.Far
                    });
                }
            }

            // Draw secondary text (row 2)
            if (!string.IsNullOrEmpty(data.SecondaryText))
            {
                using (var secondaryFont = new Font(_theme.Fonts.Default.Family, 9F, FontStyle.Regular))
                using (var brush = new SolidBrush(subTextColor))
                {
                    var textRect = new Rectangle(
                        bounds.X + textLeft,
                        bounds.Y + 30,
                        bounds.Width - textLeft - rightMargin,
                        18
                    );
                    g.DrawString(data.SecondaryText, secondaryFont, brush, textRect, new StringFormat
                    {
                        Trimming = StringTrimming.EllipsisCharacter
                    });
                }
            }
        }

        private Color GetTextColor(bool isSelected, bool useFullForeground = false)
        {
            if (isSelected)
            {
                return ThemeApplicator.ParseColor(_theme.Controls.ListBox.SelectedForeground);
            }
            return ThemeApplicator.ParseColor(_theme.Controls.ListBox.Foreground);
        }

        private Color GetSecondaryTextColor()
        {
            return ThemeApplicator.ParseColor(_theme.Colors.SecondaryForeground);
        }
    }
}
