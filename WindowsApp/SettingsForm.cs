using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    public class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private DataGridView _hotkeyGrid = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Button _addButton = null!;
        private Button _removeButton = null!;
        private Button _resetButton = null!;
        private CheckBox _showLastUsedPromptsCheckBox = null!;
        private CheckBox _showLastUsedPlaceholderValuesCheckBox = null!;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            LoadHotkeys();

            // Register with ThemeManager
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

        private void InitializeComponent()
        {
            Text = "PromptArq Settings";
            Size = new Size(700, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Apply dark title bar for consistent styling
            HandleCreated += (s, e) => WindowStyleManager.ApplyDarkTitleBar(this);

            // Title label
            var titleLabel = new Label
            {
                Text = "Hotkey Configuration",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(titleLabel);

            // Instructions label
            var instructionsLabel = new Label
            {
                Text = "Configure global hotkeys for quick actions. Changes take effect after clicking Save.",
                Location = new Point(20, 55),
                Size = new Size(640, 30),
                ForeColor = Color.Gray
            };
            Controls.Add(instructionsLabel);

            // DataGridView for hotkeys
            _hotkeyGrid = new DataGridView
            {
                Location = new Point(20, 95),
                Size = new Size(640, 240),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _hotkeyGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Action",
                HeaderText = "Action",
                ReadOnly = true,
                FillWeight = 40
            });

            var keyColumn = new DataGridViewComboBoxColumn
            {
                Name = "Key",
                HeaderText = "Key",
                FillWeight = 20
            };
            keyColumn.Items.AddRange(new object[] {
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9"
            });
            _hotkeyGrid.Columns.Add(keyColumn);

            _hotkeyGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Ctrl",
                HeaderText = "Ctrl",
                FillWeight = 10
            });

            _hotkeyGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Alt",
                HeaderText = "Alt",
                FillWeight = 10
            });

            _hotkeyGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Shift",
                HeaderText = "Shift",
                FillWeight = 10
            });

            _hotkeyGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Win",
                HeaderText = "Win",
                FillWeight = 10
            });

            Controls.Add(_hotkeyGrid);

            // Command Palette Features section
            // Add button
            _addButton = new Button
            {
                Text = "Add Hotkey",
                Location = new Point(20, 345),
                Size = new Size(100, 30)
            };
            _addButton.Click += AddButton_Click;
            Controls.Add(_addButton);

            // Remove button
            _removeButton = new Button
            {
                Text = "Remove",
                Location = new Point(130, 345),
                Size = new Size(100, 30)
            };
            _removeButton.Click += RemoveButton_Click;
            Controls.Add(_removeButton);

            // Reset button
            _resetButton = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(240, 345),
                Size = new Size(120, 30)
            };
            _resetButton.Click += ResetButton_Click;
            Controls.Add(_resetButton);

            // Move features label and checkboxes below the hotkey buttons to avoid overlap
            var featuresLabel = new Label
            {
                Text = "Command Palette Features",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(20, 390),
                AutoSize = true
            };
            Controls.Add(featuresLabel);

            _showLastUsedPromptsCheckBox = new CheckBox
            {
                Text = "Show last used prompts when palette opens (empty search)",
                Location = new Point(20, 420),
                Size = new Size(640, 20),
                Checked = _settings.ShowLastUsedPrompts
            };
            Controls.Add(_showLastUsedPromptsCheckBox);

            _showLastUsedPlaceholderValuesCheckBox = new CheckBox
            {
                Text = "Suggest last used values when filling placeholders",
                Location = new Point(20, 450),
                Size = new Size(640, 20),
                Checked = _settings.ShowLastUsedPlaceholderValues
            };
            Controls.Add(_showLastUsedPlaceholderValuesCheckBox);

            // Save button
            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(450, 505),
                Size = new Size(100, 30),
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;
            Controls.Add(_saveButton);
            AcceptButton = _saveButton;

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(560, 505),
                Size = new Size(100, 30),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(_cancelButton);
            CancelButton = _cancelButton;
        }

        private void LoadHotkeys()
        {
            _hotkeyGrid.Rows.Clear();
            foreach (var hotkey in _settings.Hotkeys)
            {
                _hotkeyGrid.Rows.Add(
                    hotkey.Action,
                    hotkey.Key,
                    hotkey.Ctrl,
                    hotkey.Alt,
                    hotkey.Shift,
                    hotkey.Win
                );
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            var actionDialog = new Form
            {
                Text = "Add Hotkey",
                Size = new Size(350, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var actionLabel = new Label
            {
                Text = "Action Name:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            actionDialog.Controls.Add(actionLabel);

            var actionTextBox = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(290, 25)
            };
            actionDialog.Controls.Add(actionTextBox);

            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(150, 80),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            actionDialog.Controls.Add(okButton);
            actionDialog.AcceptButton = okButton;

            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(235, 80),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            actionDialog.Controls.Add(cancelButton);
            actionDialog.CancelButton = cancelButton;

            if (actionDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(actionTextBox.Text))
            {
                _hotkeyGrid.Rows.Add(actionTextBox.Text, "P", true, false, false, false);
            }
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (_hotkeyGrid.SelectedRows.Count > 0)
            {
                _hotkeyGrid.Rows.RemoveAt(_hotkeyGrid.SelectedRows[0].Index);
            }
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will reset all hotkeys to default values. Continue?",
                "Reset Hotkeys",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _settings.SetDefaultHotkeys();
                LoadHotkeys();
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            _settings.Hotkeys.Clear();

            foreach (DataGridViewRow row in _hotkeyGrid.Rows)
            {
                if (row.Cells["Action"].Value != null && row.Cells["Key"].Value != null)
                {
                    var hotkey = new HotkeyConfig
                    {
                        Action = row.Cells["Action"].Value?.ToString() ?? "",
                        Key = row.Cells["Key"].Value?.ToString() ?? "",
                        Ctrl = Convert.ToBoolean(row.Cells["Ctrl"].Value ?? false),
                        Alt = Convert.ToBoolean(row.Cells["Alt"].Value ?? false),
                        Shift = Convert.ToBoolean(row.Cells["Shift"].Value ?? false),
                        Win = Convert.ToBoolean(row.Cells["Win"].Value ?? false)
                    };
                    _settings.Hotkeys.Add(hotkey);
                }
            }

            // Save feature flags
            _settings.ShowLastUsedPrompts = _showLastUsedPromptsCheckBox.Checked;
            _settings.ShowLastUsedPlaceholderValues = _showLastUsedPlaceholderValuesCheckBox.Checked;

            _settings.Save();
        }
    }
}
