using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PromptArqApp.Workflow.Registry;
using PromptArqApp.Theming;

namespace PromptArqApp
{
    public class SettingsForm : BorderlessFormBase
    {
        private readonly AppSettings _settings;
        private readonly IWorkflowRegistry? _workflowRegistry;

        private const string SectionGeneralKey = "general";
        private const string SectionHotkeysKey = "hotkeys";
        private const string SectionAppearanceKey = "appearance";
        private const string SectionWorkflowsKey = "workflows";

        private TreeView _sectionTreeView = null!;
        private Panel _contentPanel = null!;
        private Panel _generalSectionPanel = null!;
        private Panel _hotkeySectionPanel = null!;
        private Panel _appearanceSectionPanel = null!;
        private Panel _workflowSectionPanel = null!;
        private ListView _workflowListView = null!;
        private Label _workflowCountLabel = null!;
        private Button _refreshWorkflowsButton = null!;
        private bool _suppressWorkflowListItemCheck;

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

        public SettingsForm(AppSettings settings, IWorkflowRegistry? workflowRegistry)
        {
            _settings = settings;
            _workflowRegistry = workflowRegistry;
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
            var workflowsNode = new TreeNode("Workflows") { Tag = SectionWorkflowsKey };

            _sectionTreeView.Nodes.AddRange(new[] { generalNode, hotkeysNode, appearanceNode, workflowsNode });
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
            _workflowSectionPanel = CreateWorkflowSectionPanel();

            _contentPanel.Controls.Add(_generalSectionPanel);
            _contentPanel.Controls.Add(_hotkeySectionPanel);
            _contentPanel.Controls.Add(_appearanceSectionPanel);
            _contentPanel.Controls.Add(_workflowSectionPanel);

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
                var row = _hotkeyGrid.Rows[_hotkeyGrid.Rows.Add(
                    hotkey.Action,
                    FormatHotkeyDisplay(hotkey),
                    "Record"
                )];
                row.Tag = hotkey; // Store the full HotkeyConfig for later retrieval
            }
        }

        private string FormatHotkeyDisplay(HotkeyConfig hotkey)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (hotkey.Ctrl) parts.Add("Ctrl");
            if (hotkey.Alt) parts.Add("Alt");
            if (hotkey.Shift) parts.Add("Shift");
            if (hotkey.Win) parts.Add("Win");
            parts.Add(GetFriendlyKeyName(hotkey.Key));
            if (!string.IsNullOrEmpty(hotkey.Key2))
            {
                parts.Add(GetFriendlyKeyName(hotkey.Key2));
            }
            return string.Join(" + ", parts);
        }

        private string GetFriendlyKeyName(string keyString)
        {
            // Handle digit keys (D0-D9 → "0"-"9")
            if (keyString.Length == 2 && keyString[0] == 'D' && char.IsDigit(keyString[1]))
                return keyString[1].ToString();
            
            // Already friendly names for letters and function keys
            return keyString;
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
                // Open hotkey recorder
                using var recorder = new HotkeyRecorderDialog();
                if (recorder.ShowDialog() == DialogResult.OK && recorder.RecordedHotkey != null)
                {
                    var hotkey = recorder.RecordedHotkey;
                    hotkey.Action = actionTextBox.Text;
                    
                    var row = _hotkeyGrid.Rows[_hotkeyGrid.Rows.Add(
                        hotkey.Action,
                        FormatHotkeyDisplay(hotkey),
                        "Record"
                    )];
                    row.Tag = hotkey;
                }
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
                if (row.Tag is HotkeyConfig hotkey)
                {
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
                Text = "Configure global hotkeys for quick actions. Click 'Record' to capture key combinations. Changes take effect after clicking Save.",
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

        private Panel CreateWorkflowSectionPanel()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var titleLabel = new Label
            {
                Text = "Workflows",
                Location = new Point(10, 10),
                AutoSize = true
            };
            panel.Controls.Add(titleLabel);

            var descriptionLabel = new Label
            {
                Text = "See which JSON workflows were loaded and inspect metadata from the registry.",
                Location = new Point(10, 35),
                Size = new Size(600, 30)
            };
            panel.Controls.Add(descriptionLabel);

            _workflowCountLabel = new Label
            {
                Text = "Workflows loaded: 0",
                Location = new Point(10, 70),
                AutoSize = true
            };
            panel.Controls.Add(_workflowCountLabel);

            _workflowListView = new ListView
            {
                Location = new Point(10, 100),
                Size = new Size(600, 260),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                MultiSelect = false,
                HideSelection = false,
                CheckBoxes = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _workflowListView.ItemCheck += WorkflowListView_ItemCheck;

            _workflowListView.Columns.Add(new ColumnHeader { Text = "Icon", Width = 40 });
            _workflowListView.Columns.Add(new ColumnHeader { Text = "Name", Width = 160 });
            _workflowListView.Columns.Add(new ColumnHeader { Text = "ID", Width = 140 });
            _workflowListView.Columns.Add(new ColumnHeader { Text = "Entry Node", Width = 120 });
            _workflowListView.Columns.Add(new ColumnHeader { Text = "Nodes", Width = 60 });
            _workflowListView.Columns.Add(new ColumnHeader { Text = "Tags", Width = 200 });
            _workflowListView.Columns.Add(new ColumnHeader { Text = "Author", Width = 140 });

            panel.Controls.Add(_workflowListView);

            _refreshWorkflowsButton = new Button
            {
                Text = "Refresh List",
                Location = new Point(10, 370),
                Size = new Size(120, 30),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _refreshWorkflowsButton.Click += (s, e) => PopulateWorkflowList();
            panel.Controls.Add(_refreshWorkflowsButton);

            PopulateWorkflowList();
            return panel;
        }

        private void PopulateWorkflowList()
        {
            if (_workflowListView == null || _workflowCountLabel == null)
                return;

            _suppressWorkflowListItemCheck = true;
            _workflowListView.Items.Clear();

            if (_workflowRegistry == null)
            {
                _workflowCountLabel.Text = "Workflow registry unavailable";
                _suppressWorkflowListItemCheck = false;
                return;
            }

            var workflows = _workflowRegistry.GetAllWorkflows().OrderBy(w => w.Name).ToArray();
            _workflowCountLabel.Text = $"Workflows loaded: {workflows.Length}";

            if (workflows.Length == 0)
            {
                var placeholder = new ListViewItem(new[] { string.Empty, "No workflows registered", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty });
                placeholder.ForeColor = SystemColors.GrayText;
                _workflowListView.Items.Add(placeholder);
                _suppressWorkflowListItemCheck = false;
                return;
            }

            foreach (var workflow in workflows)
            {
                var tags = workflow.Metadata?.Tags ?? Array.Empty<string>();
                var item = new ListViewItem(new[]
                {
                    workflow.Icon,
                    workflow.Name,
                    workflow.Id,
                    workflow.EntryNodeId,
                    workflow.Nodes.Count.ToString(),
                    string.Join(", ", tags),
                    workflow.Metadata.Author
                });

                var enabled = _settings.IsWorkflowEnabled(workflow.Id);
                item.Checked = enabled;
                item.ForeColor = enabled ? SystemColors.WindowText : SystemColors.GrayText;

                _workflowListView.Items.Add(item);
            }
            _suppressWorkflowListItemCheck = false;
        }

        private void WorkflowListView_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_suppressWorkflowListItemCheck)
                return;

            if (e.Index < 0 || e.Index >= _workflowListView.Items.Count)
                return;

            var item = _workflowListView.Items[e.Index];
            if (item.SubItems.Count < 3)
                return;

            var workflowId = item.SubItems[2].Text;
            if (string.IsNullOrWhiteSpace(workflowId))
                return;

            var enabled = e.NewValue == CheckState.Checked;
            _settings.SetWorkflowEnabled(workflowId, enabled);
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

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Hotkey",
                HeaderText = "Hotkey",
                ReadOnly = true,
                FillWeight = 40
            });

            var recordButton = new DataGridViewButtonColumn
            {
                Name = "RecordButton",
                HeaderText = "Record",
                Text = "Record",
                UseColumnTextForButtonValue = true,
                FillWeight = 20
            };
            grid.Columns.Add(recordButton);

            // Handle button clicks
            grid.CellContentClick += HotkeyGrid_CellContentClick;

            return grid;
        }

        private void HotkeyGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var grid = sender as DataGridView;
            if (grid == null) return;

            // Check if Record button was clicked
            if (grid.Columns[e.ColumnIndex].Name == "RecordButton")
            {
                var row = grid.Rows[e.RowIndex];
                var existingHotkey = row.Tag as HotkeyConfig;

                // Open hotkey recorder dialog
                using var recorder = new HotkeyRecorderDialog(existingHotkey);
                if (recorder.ShowDialog() == DialogResult.OK && recorder.RecordedHotkey != null)
                {
                    var newHotkey = recorder.RecordedHotkey;
                    newHotkey.Action = row.Cells["Action"].Value?.ToString() ?? "";
                    
                    // Update row
                    row.Cells["Hotkey"].Value = FormatHotkeyDisplay(newHotkey);
                    row.Tag = newHotkey;
                }
            }
        }

        private void ShowSection(string sectionKey)
        {
            var key = sectionKey switch
            {
                SectionHotkeysKey => SectionHotkeysKey,
                SectionAppearanceKey => SectionAppearanceKey,
                SectionWorkflowsKey => SectionWorkflowsKey,
                _ => SectionGeneralKey
            };

            _generalSectionPanel.Visible = key == SectionGeneralKey;
            _hotkeySectionPanel.Visible = key == SectionHotkeysKey;
            _appearanceSectionPanel.Visible = key == SectionAppearanceKey;
            _workflowSectionPanel.Visible = key == SectionWorkflowsKey;

            if (_generalSectionPanel.Visible)
            {
                _generalSectionPanel.BringToFront();
            }
            else if (_hotkeySectionPanel.Visible)
            {
                _hotkeySectionPanel.BringToFront();
            }
            else if (_appearanceSectionPanel.Visible)
            {
                _appearanceSectionPanel.BringToFront();
            }
            else
            {
                _workflowSectionPanel.BringToFront();
                PopulateWorkflowList();
            }
        }

        private void SectionTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            var sectionKey = (e.Node?.Tag as string) ?? SectionGeneralKey;
            ShowSection(sectionKey);
        }
    }
}
