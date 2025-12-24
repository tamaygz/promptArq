using System;
using System.Drawing;
using System.Windows.Forms;
using Serilog;

namespace PromptArqApp.Theming
{
    /// <summary>
    /// Applies theme settings to forms and controls recursively.
    /// </summary>
    public static class ThemeApplicator
    {
        private static readonly ILogger Logger = LoggerConfig.ForContext("ThemeApplicator");

        /// <summary>
        /// Applies a theme to a form and all its controls
        /// </summary>
        public static void ApplyToForm(Form form, Theme theme)
        {
            if (form == null || theme == null)
                return;

            try
            {
                Logger.Debug("Applying theme '{ThemeName}' to form '{FormName}'", theme.Name, form.Name);

                form.SuspendLayout();

                // Apply form-level properties
                form.BackColor = ParseColor(theme.Colors.Background);
                form.ForeColor = ParseColor(theme.Colors.Foreground);
                form.Opacity = theme.Window.Opacity;

                // Apply to all controls recursively
                foreach (Control control in form.Controls)
                {
                    ApplyToControl(control, theme);
                }

                form.ResumeLayout(true);
                form.Invalidate(true);

                Logger.Debug("Theme applied successfully to form '{FormName}'", form.Name);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error applying theme to form '{FormName}'", form.Name);
            }
        }

        /// <summary>
        /// Applies a theme to a control and all its children recursively
        /// </summary>
        public static void ApplyToControl(Control control, Theme theme)
        {
            if (control == null || theme == null)
                return;

            try
            {
                // Apply type-specific theming
                switch (control)
                {
                    case Button button:
                        ApplyToButton(button, theme);
                        break;
                    case TextBox textBox:
                        ApplyToTextBox(textBox, theme);
                        break;
                    case RichTextBox richTextBox:
                        ApplyToRichTextBox(richTextBox, theme);
                        break;
                    case ListBox listBox:
                        ApplyToListBox(listBox, theme);
                        break;
                    case DataGridView dgv:
                        ApplyToDataGridView(dgv, theme);
                        break;
                    case Label label:
                        ApplyToLabel(label, theme);
                        break;
                    case Panel panel:
                        ApplyToPanel(panel, theme);
                        break;
                    case CheckBox checkBox:
                        ApplyToCheckBox(checkBox, theme);
                        break;
                    case ComboBox comboBox:
                        ApplyToComboBox(comboBox, theme);
                        break;
                    default:
                        // Apply default colors for unknown control types
                        control.BackColor = ParseColor(theme.Colors.ControlBackground);
                        control.ForeColor = ParseColor(theme.Colors.Foreground);
                        break;
                }

                // Recursively apply to child controls
                foreach (Control child in control.Controls)
                {
                    ApplyToControl(child, theme);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Error applying theme to control '{ControlName}'", control.Name);
            }
        }

        /// <summary>
        /// Applies theme to a Button control
        /// </summary>
        private static void ApplyToButton(Button button, Theme theme)
        {
            button.BackColor = ParseColor(theme.Controls.Button.Background);
            button.ForeColor = ParseColor(theme.Controls.Button.Foreground);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = ParseColor(theme.Colors.Border);
            
            // Note: Not disposing old Font as it may be shared across controls
            // WinForms controls manage their Font lifecycle internally
            button.Font = theme.Fonts.Default.ToFont();
            
            // Store hover colors using a dedicated data structure
            var hoverData = new ButtonHoverData
            {
                HoverColor = ParseColor(theme.Controls.Button.HoverBackground),
                NormalColor = button.BackColor
            };
            
            // Store in Tag or use a dictionary to avoid handler accumulation
            button.Tag = hoverData;
            
            // Remove old handlers before adding new ones
            button.MouseEnter -= Button_MouseEnter;
            button.MouseLeave -= Button_MouseLeave;
            
            // Add new handlers
            button.MouseEnter += Button_MouseEnter;
            button.MouseLeave += Button_MouseLeave;
        }

        private class ButtonHoverData
        {
            public Color HoverColor { get; set; }
            public Color NormalColor { get; set; }
        }

        private static void Button_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is ButtonHoverData data)
            {
                btn.BackColor = data.HoverColor;
            }
        }
        
        private static void Button_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is ButtonHoverData data)
            {
                btn.BackColor = data.NormalColor;
            }
        }

        /// <summary>
        /// Applies theme to a TextBox control
        /// </summary>
        private static void ApplyToTextBox(TextBox textBox, Theme theme)
        {
            textBox.BackColor = ParseColor(theme.Controls.TextBox.Background);
            textBox.ForeColor = ParseColor(theme.Controls.TextBox.Foreground);
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = theme.Fonts.Default.ToFont();
        }

        /// <summary>
        /// Applies theme to a RichTextBox control
        /// </summary>
        private static void ApplyToRichTextBox(RichTextBox richTextBox, Theme theme)
        {
            var foreColor = ParseColor(theme.Colors.Foreground);
            var backColor = ParseColor(theme.Colors.ControlBackground);
            
            richTextBox.BackColor = backColor;
            richTextBox.ForeColor = foreColor;
            richTextBox.BorderStyle = BorderStyle.None;
            richTextBox.Font = theme.Fonts.Default.ToFont();

            // CRITICAL: Set default formatting for the insertion point
            // This ensures that any text added in the future uses correct theme colors
            // We must do this BEFORE and AFTER clearing existing text formatting
            
            int currentSelection = richTextBox.SelectionStart;
            int currentLength = richTextBox.SelectionLength;
            
            // Set insertion point formatting (affects future text)
            richTextBox.Select(0, 0);
            richTextBox.SelectionColor = foreColor;
            richTextBox.SelectionBackColor = Color.Empty;
            
            // Clear any per-character formatting for existing text
            // This fixes the issue where SelectionBackColor persists even after changing themes
            if (richTextBox.TextLength > 0)
            {
                // Select all text and reset formatting
                richTextBox.SelectAll();
                
                // Set text color and clear background for ALL existing text
                richTextBox.SelectionColor = foreColor;
                richTextBox.SelectionBackColor = Color.Empty;
            }
            
            // Set insertion point at the end with correct formatting
            // This ensures any new text appended uses correct colors
            richTextBox.Select(richTextBox.TextLength, 0);
            richTextBox.SelectionColor = foreColor;
            richTextBox.SelectionBackColor = Color.Empty;
            
            // Restore original selection
            richTextBox.Select(currentSelection, currentLength);
        }

        /// <summary>
        /// Applies theme to a ListBox control
        /// </summary>
        private static void ApplyToListBox(ListBox listBox, Theme theme)
        {
            listBox.BackColor = ParseColor(theme.Controls.ListBox.Background);
            listBox.ForeColor = ParseColor(theme.Controls.ListBox.Foreground);
            listBox.BorderStyle = BorderStyle.None;
            listBox.Font = theme.Fonts.Default.ToFont();

            // For owner-drawn listboxes, ensure they redraw
            if (listBox.DrawMode != DrawMode.Normal)
            {
                listBox.Invalidate();
            }
        }

        /// <summary>
        /// Applies theme to a DataGridView control
        /// </summary>
        private static void ApplyToDataGridView(DataGridView dgv, Theme theme)
        {
            dgv.BackgroundColor = ParseColor(theme.Colors.ControlBackground);
            dgv.ForeColor = ParseColor(theme.Colors.Foreground);
            dgv.GridColor = ParseColor(theme.Colors.Border);
            dgv.BorderStyle = BorderStyle.None;
            dgv.Font = theme.Fonts.Default.ToFont();

            // Apply to cells
            dgv.DefaultCellStyle.BackColor = ParseColor(theme.Colors.ControlBackground);
            dgv.DefaultCellStyle.ForeColor = ParseColor(theme.Colors.Foreground);
            dgv.DefaultCellStyle.SelectionBackColor = ParseColor(theme.Colors.Selection);
            dgv.DefaultCellStyle.SelectionForeColor = ParseColor(theme.Colors.Foreground);

            // Apply to headers
            dgv.ColumnHeadersDefaultCellStyle.BackColor = ParseColor(theme.Colors.HeaderBackground);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ParseColor(theme.Colors.Foreground);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ParseColor(theme.Colors.HeaderBackground);
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = ParseColor(theme.Colors.Foreground);

            dgv.RowHeadersDefaultCellStyle.BackColor = ParseColor(theme.Colors.HeaderBackground);
            dgv.RowHeadersDefaultCellStyle.ForeColor = ParseColor(theme.Colors.Foreground);
            dgv.RowHeadersDefaultCellStyle.SelectionBackColor = ParseColor(theme.Colors.Selection);
            dgv.RowHeadersDefaultCellStyle.SelectionForeColor = ParseColor(theme.Colors.Foreground);

            dgv.EnableHeadersVisualStyles = false;
        }

        /// <summary>
        /// Applies theme to a Label control
        /// </summary>
        private static void ApplyToLabel(Label label, Theme theme)
        {
            // Apply font based on size - headings use larger font
            label.Font = label.Font.Size >= 12
                ? theme.Fonts.Heading.ToFont()
                : theme.Fonts.Default.ToFont();

            label.ForeColor = ParseColor(theme.Colors.Foreground);
            
            // Keep label background transparent or use parent background
            if (label.BackColor != Color.Transparent)
            {
                label.BackColor = ParseColor(theme.Colors.Background);
            }
        }

        /// <summary>
        /// Applies theme to a Panel control
        /// </summary>
        private static void ApplyToPanel(Panel panel, Theme theme)
        {
            // Use header background for panels with "Header" in name, otherwise use control background
            var backColorHex = panel.Name.Contains("Header", StringComparison.OrdinalIgnoreCase)
                ? theme.Colors.HeaderBackground
                : theme.Colors.ControlBackground;
            
            panel.BackColor = ParseColor(backColorHex);
            panel.ForeColor = ParseColor(theme.Colors.Foreground);
        }

        /// <summary>
        /// Applies theme to a CheckBox control
        /// </summary>
        private static void ApplyToCheckBox(CheckBox checkBox, Theme theme)
        {
            checkBox.ForeColor = ParseColor(theme.Colors.Foreground);
            checkBox.Font = theme.Fonts.Default.ToFont();
            
            // Keep background transparent for checkboxes
            if (checkBox.BackColor != Color.Transparent)
            {
                checkBox.BackColor = ParseColor(theme.Colors.ControlBackground);
            }
        }

        /// <summary>
        /// Applies theme to a ComboBox control
        /// </summary>
        private static void ApplyToComboBox(ComboBox comboBox, Theme theme)
        {
            comboBox.BackColor = ParseColor(theme.Controls.TextBox.Background);
            comboBox.ForeColor = ParseColor(theme.Controls.TextBox.Foreground);
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.Font = theme.Fonts.Default.ToFont();
        }

        /// <summary>
        /// Parses a hex color string to a Color object
        /// </summary>
        public static Color ParseColor(string hexColor)
        {
            try
            {
                return ColorTranslator.FromHtml(hexColor);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to parse color '{Color}', using fallback", hexColor);
                return Color.White;
            }
        }

        /// <summary>
        /// Creates a Font from a FontDefinition
        /// </summary>
        public static Font CreateFont(FontDefinition fontDef)
        {
            return fontDef.ToFont();
        }
    }
}
