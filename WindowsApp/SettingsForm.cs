using System;
using System.Drawing;
using System.Windows.Forms;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    public class SettingsForm : BorderlessFormBase
    {
        private readonly AppSettings _settings;

        private const string SectionGeneralKey = "general";
        private const string SectionHotkeysKey = "hotkeys";
        private const string SectionAppearanceKey = "appearance";

        private TreeView _sectionTreeView = null!;
        private Panel _contentPanel = null!;
        private Panel _generalSectionPanel = null!;
        private Panel _hotkeySectionPanel = null!;
        private Panel _appearanceSectionPanel = null!;

        private DataGridView _hotkeyGrid = null!;
        private Button _saveButton = null!;
        private Button _applyButton = null!;
        private Button _cancelButton = null!;
        private Button _addButton = null!;
        private Button _removeButton = null!;
        private Button _resetButton = null!;
        private CheckBox _showLastUsedPromptsCheckBox = null!;
        private CheckBox _showLastUsedPlaceholderValuesCheckBox = null!;
        private ComboBox _themeComboBox = null!;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            LoadHotkeys();

            // Register with ThemeManager
            ThemeManager.Instance.RegisterForm(this);
            ThemeManager.Instance.ApplyThemeToForm(this);

            // Subscribe to theme changes
            EventHandler<ThemeChangedEventArgs> themeChangedHandler = (s, e) =>
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
            ThemeManager.Instance.ThemeChanged += themeChangedHandler;
            
            // Cleanup on closing
            FormClosing += (s, e) =>
            {
                ThemeManager.Instance.ThemeChanged -= themeChangedHandler;
            };
        }

        private void InitializeComponent()
        {
            Text = "PromptArq Settings";
            Size = new Size(900, 720);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;

            var sectionsLabel = new Label
            {
                Text = "Settings Sections",
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(sectionsLabel);

            _sectionTreeView = new TreeView
            {
                Location = new Point(20, 50),
                Size = new Size(200, 520),
                HideSelection = false,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };

            var generalNode = new TreeNode("General") { Tag = SectionGeneralKey };
            var hotkeysNode = new TreeNode("Hotkeys") { Tag = SectionHotkeysKey };
            var appearanceNode = new TreeNode("Appearance") { Tag = SectionAppearanceKey };

            _sectionTreeView.Nodes.AddRange(new[] { generalNode, hotkeysNode, appearanceNode });
            _sectionTreeView.AfterSelect += SectionTreeView_AfterSelect;
            Controls.Add(_sectionTreeView);

            _contentPanel = new Panel
            {
                Location = new Point(240, 40),
                Size = new Size(640, 520),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_contentPanel);

            _generalSectionPanel = CreateGeneralSectionPanel();
            _hotkeySectionPanel = CreateHotkeySectionPanel();
            _appearanceSectionPanel = CreateAppearanceSectionPanel();

            _contentPanel.Controls.Add(_generalSectionPanel);
            _contentPanel.Controls.Add(_hotkeySectionPanel);
            _contentPanel.Controls.Add(_appearanceSectionPanel);

            _sectionTreeView.SelectedNode = generalNode;
            _sectionTreeView.ExpandAll();
            ShowSection(SectionGeneralKey);

            const int buttonTop = 620;

            _applyButton = new Button
            {
                Text = "Apply",
                Location = new Point(520, buttonTop),
                Size = new Size(100, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _applyButton.Click += ApplyButton_Click;
            Controls.Add(_applyButton);

            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(630, buttonTop),
                Size = new Size(100, 30),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _saveButton.Click += SaveButton_Click;
            Controls.Add(_saveButton);
            AcceptButton = _saveButton;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(740, buttonTop),
                Size = new Size(100, 30),
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
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

        private void ApplyButton_Click(object? sender, EventArgs e)
        {
            ApplySettings();
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            ApplySettings();
        }

        private void ApplySettings()
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

            // Handle theme change
            var selectedTheme = _themeComboBox.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedTheme) && selectedTheme != _settings.CurrentTheme)
            {
                var oldTheme = _settings.CurrentTheme;
                
                // Load and apply the new theme
                if (ThemeManager.Instance.LoadTheme(selectedTheme))
                {
                    // Only update CurrentTheme if theme loaded successfully
                    _settings.CurrentTheme = selectedTheme;
                    
                    // Show toast notification (2 seconds)
                    NotificationManager.ShowToast(
                        $"Theme changed to '{selectedTheme}'",
                        2000
                    );
                }
                else
                {
                    // Revert selection if theme failed to load
                    _themeComboBox.SelectedItem = oldTheme;
                    
                    NotificationManager.ShowToast(
                        $"Failed to load theme '{selectedTheme}'",
                        2000
                    );
                }
            }
            else
            {
                // No theme change, just show success (2 seconds)
                NotificationManager.ShowToast(
                    "Settings saved successfully",
                    2000
                );
            }

            _settings.Save();
        }

        private Panel CreateGeneralSectionPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var generalLabel = new Label
            {
                Text = "General Settings",
                Location = new Point(10, 10),
                AutoSize = true
            };
            panel.Controls.Add(generalLabel);

            var descriptionLabel = new Label
            {
                Text = "Manage how the command palette behaves and which suggestions surface when it opens.",
                Location = new Point(10, 35),
                Size = new Size(600, 40)
            };
            panel.Controls.Add(descriptionLabel);

            _showLastUsedPromptsCheckBox = new CheckBox
            {
                Text = "Show last used prompts when palette opens (empty search)",
                Location = new Point(10, 90),
                AutoSize = true,
                Checked = _settings.ShowLastUsedPrompts,
                MaximumSize = new Size(600, 0)
            };
            _showLastUsedPromptsCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panel.Controls.Add(_showLastUsedPromptsCheckBox);

            _showLastUsedPlaceholderValuesCheckBox = new CheckBox
            {
                Text = "Suggest last used values when filling placeholders",
                Location = new Point(10, 130),
                AutoSize = true,
                Checked = _settings.ShowLastUsedPlaceholderValues,
                MaximumSize = new Size(600, 0)
            };
            _showLastUsedPlaceholderValuesCheckBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panel.Controls.Add(_showLastUsedPlaceholderValuesCheckBox);

            return panel;
        }

        private Panel CreateHotkeySectionPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var titleLabel = new Label
            {
                Text = "Hotkey Configuration",
                Location = new Point(10, 10),
                AutoSize = true
            };
            panel.Controls.Add(titleLabel);

            var instructionsLabel = new Label
            {
                Text = "Configure global hotkeys for quick actions. Changes take effect after clicking Save.",
                Location = new Point(10, 35),
                Size = new Size(600, 30)
            };
            panel.Controls.Add(instructionsLabel);

            _hotkeyGrid = CreateHotkeyGridControl();
            _hotkeyGrid.Location = new Point(10, 70);
            _hotkeyGrid.Size = new Size(600, 240);
            _hotkeyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panel.Controls.Add(_hotkeyGrid);

            _addButton = new Button
            {
                Text = "Add Hotkey",
                Location = new Point(10, 330),
                Size = new Size(120, 30)
            };
            _addButton.Click += AddButton_Click;
            panel.Controls.Add(_addButton);

            _removeButton = new Button
            {
                Text = "Remove",
                Location = new Point(140, 330),
                Size = new Size(100, 30)
            };
            _removeButton.Click += RemoveButton_Click;
            panel.Controls.Add(_removeButton);

            _resetButton = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(250, 330),
                Size = new Size(150, 30)
            };
            _resetButton.Click += ResetButton_Click;
            panel.Controls.Add(_resetButton);

            return panel;
        }

        private Panel CreateAppearanceSectionPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var appearanceLabel = new Label
            {
                Text = "Appearance",
                Location = new Point(10, 10),
                AutoSize = true
            };
            panel.Controls.Add(appearanceLabel);

            var descriptionLabel = new Label
            {
                Text = "Pick a theme that keeps PromptArq in sync with your workflow.",
                Location = new Point(10, 35),
                Size = new Size(600, 30)
            };
            panel.Controls.Add(descriptionLabel);

            var themeLabel = new Label
            {
                Text = "Theme:",
                Location = new Point(10, 70),
                AutoSize = true
            };
            panel.Controls.Add(themeLabel);

            _themeComboBox = new ComboBox
            {
                Location = new Point(70, 67),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            var availableThemes = ThemeManager.Instance.GetAvailableThemes();
            foreach (var theme in availableThemes)
            {
                _themeComboBox.Items.Add(theme);
            }

            if (!string.IsNullOrWhiteSpace(_settings.CurrentTheme) && _themeComboBox.Items.Contains(_settings.CurrentTheme))
            {
                _themeComboBox.SelectedItem = _settings.CurrentTheme;
            }
            else if (_themeComboBox.Items.Count > 0)
            {
                _themeComboBox.SelectedIndex = 0;
            }

            panel.Controls.Add(_themeComboBox);
            return panel;
        }

        private DataGridView CreateHotkeyGridControl()
        {
            var grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
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
            grid.Columns.Add(keyColumn);

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Ctrl",
                HeaderText = "Ctrl",
                FillWeight = 10
            });

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Alt",
                HeaderText = "Alt",
                FillWeight = 10
            });

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Shift",
                HeaderText = "Shift",
                FillWeight = 10
            });

            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Win",
                HeaderText = "Win",
                FillWeight = 10
            });

            return grid;
        }

        private void ShowSection(string sectionKey)
        {
            var key = sectionKey switch
            {
                SectionHotkeysKey => SectionHotkeysKey,
                SectionAppearanceKey => SectionAppearanceKey,
                _ => SectionGeneralKey
            };

            _generalSectionPanel.Visible = key == SectionGeneralKey;
            _hotkeySectionPanel.Visible = key == SectionHotkeysKey;
            _appearanceSectionPanel.Visible = key == SectionAppearanceKey;

            if (_generalSectionPanel.Visible)
            {
                _generalSectionPanel.BringToFront();
            }
            else if (_hotkeySectionPanel.Visible)
            {
                _hotkeySectionPanel.BringToFront();
            }
            else
            {
                _appearanceSectionPanel.BringToFront();
            }
        }

        private void SectionTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            var sectionKey = (e.Node?.Tag as string) ?? SectionGeneralKey;
            ShowSection(sectionKey);
        }
    }
}
