using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PromptArqApp
{
    public partial class CommandPaletteForm : Form
    {
        private TextBox _searchBox = null!;
        private ListBox _resultsList = null!;
        private Label _hintLabel = null!;
        private Panel _headerPanel = null!;
        private Panel _contentPanel = null!;
        
        private List<PromptInfo> _allPrompts = new();
        private List<PromptAction> _currentActions = new();
        private PromptInfo? _selectedPrompt;
        private bool _showingActions = false;

        public event EventHandler<PromptActionEventArgs>? ActionSelected;

        public CommandPaletteForm()
        {
            InitializeComponent();
            SetupCustomComponents();
        }

        private void SetupCustomComponents()
        {
            // Form settings
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(700, 500);
            BackColor = Color.FromArgb(30, 30, 30);
            Opacity = 0.97;
            TopMost = true;
            ShowInTaskbar = false;
            
            // Add rounded corners effect
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 15, 15));

            // Header panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(20, 15, 20, 15)
            };

            // Search box
            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 16F, FontStyle.Regular),
                Text = ""
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.KeyDown += SearchBox_KeyDown;

            var searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15),
                BackColor = Color.FromArgb(50, 50, 50)
            };
            searchPanel.Controls.Add(_searchBox);

            _headerPanel.Controls.Add(searchPanel);

            // Hint label
            _hintLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Text = "Type to search prompts... Press ESC to close",
                ForeColor = Color.Gray,
                BackColor = Color.FromArgb(40, 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            // Content panel
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(10)
            };

            // Results list
            _resultsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 35),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ItemHeight = 50,
                DrawMode = DrawMode.OwnerDrawFixed,
                SelectionMode = SelectionMode.One
            };
            _resultsList.DrawItem += ResultsList_DrawItem;
            _resultsList.DoubleClick += ResultsList_DoubleClick;
            _resultsList.KeyDown += ResultsList_KeyDown;

            _contentPanel.Controls.Add(_resultsList);

            // Add controls to form
            Controls.Add(_contentPanel);
            Controls.Add(_hintLabel);
            Controls.Add(_headerPanel);

            // Handle form closing
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        public void ShowPalette(List<PromptInfo> prompts)
        {
            _allPrompts = prompts;
            _showingActions = false;
            _selectedPrompt = null;
            _searchBox.Text = "";
            _hintLabel.Text = "Type to search prompts... Press ESC to close";
            
            FilterResults();
            Show();
            _searchBox.Focus();
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            FilterResults();
        }

        private void FilterResults()
        {
            _resultsList.Items.Clear();

            if (_showingActions)
            {
                foreach (var action in _currentActions)
                {
                    _resultsList.Items.Add(action);
                }
                return;
            }

            var query = _searchBox.Text.Trim().ToLowerInvariant();
            
            if (string.IsNullOrEmpty(query))
            {
                foreach (var prompt in _allPrompts.Take(50))
                {
                    _resultsList.Items.Add(prompt);
                }
            }
            else
            {
                var filtered = _allPrompts
                    .Where(p => 
                        p.Title.ToLowerInvariant().Contains(query) ||
                        p.Description.ToLowerInvariant().Contains(query) ||
                        p.Content.ToLowerInvariant().Contains(query) ||
                        p.ProjectName.ToLowerInvariant().Contains(query) ||
                        p.CategoryName.ToLowerInvariant().Contains(query) ||
                        p.Tags.Any(t => t.ToLowerInvariant().Contains(query))
                    )
                    .Take(50);

                foreach (var prompt in filtered)
                {
                    _resultsList.Items.Add(prompt);
                }
            }

            if (_resultsList.Items.Count > 0)
            {
                _resultsList.SelectedIndex = 0;
            }
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_resultsList.Items.Count > 0)
                    {
                        _resultsList.Focus();
                        if (_resultsList.SelectedIndex < 0)
                        {
                            _resultsList.SelectedIndex = 0;
                        }
                    }
                    e.Handled = true;
                    break;

                case Keys.Up:
                    if (_resultsList.Items.Count > 0)
                    {
                        _resultsList.Focus();
                        if (_resultsList.SelectedIndex < 0)
                        {
                            _resultsList.SelectedIndex = _resultsList.Items.Count - 1;
                        }
                    }
                    e.Handled = true;
                    break;

                case Keys.Enter:
                    HandleSelection();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    if (_showingActions)
                    {
                        GoBackToPrompts();
                    }
                    else
                    {
                        Hide();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void ResultsList_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    HandleSelection();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    if (_showingActions)
                    {
                        GoBackToPrompts();
                    }
                    else
                    {
                        Hide();
                    }
                    e.Handled = true;
                    break;

                case Keys.Back:
                    if (_showingActions)
                    {
                        GoBackToPrompts();
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void ResultsList_DoubleClick(object? sender, EventArgs e)
        {
            HandleSelection();
        }

        private void HandleSelection()
        {
            if (_resultsList.SelectedItem == null) return;

            if (_showingActions)
            {
                var action = _resultsList.SelectedItem as PromptAction;
                if (action != null && _selectedPrompt != null)
                {
                    ActionSelected?.Invoke(this, new PromptActionEventArgs(_selectedPrompt, action));
                    Hide();
                }
            }
            else
            {
                var prompt = _resultsList.SelectedItem as PromptInfo;
                if (prompt != null)
                {
                    ShowActionsForPrompt(prompt);
                }
            }
        }

        private void ShowActionsForPrompt(PromptInfo prompt)
        {
            _selectedPrompt = prompt;
            _showingActions = true;
            _searchBox.Text = "";
            _hintLabel.Text = $"Actions for: {prompt.Title}  |  Press ESC or Backspace to go back";

            _currentActions = new List<PromptAction>
            {
                new PromptAction { Type = PromptActionType.Execute, Name = "Execute", Description = "Run this prompt with the LLM", Icon = "?", IsEnabled = true },
                new PromptAction { Type = PromptActionType.Copy, Name = "Copy to Clipboard", Description = "Copy prompt content", Icon = "??", IsEnabled = true },
                new PromptAction { Type = PromptActionType.OpenInEditor, Name = "Open in Editor", Description = "Edit this prompt", Icon = "?", IsEnabled = true },
                new PromptAction { Type = PromptActionType.Improve, Name = "Improve with AI", Description = "Enhance this prompt using AI", Icon = "?", IsEnabled = true },
            };

            if (prompt.HasPlaceholders)
            {
                _currentActions.Insert(1, new PromptAction 
                { 
                    Type = PromptActionType.FillPlaceholders, 
                    Name = "Fill Placeholders", 
                    Description = "Fill in template variables", 
                    Icon = "??", 
                    IsEnabled = true 
                });
            }

            _currentActions.Add(new PromptAction { Type = PromptActionType.Export, Name = "Export", Description = "Export to JSON file", Icon = "??", IsEnabled = true });
            _currentActions.Add(new PromptAction { Type = PromptActionType.Share, Name = "Share", Description = "Generate share link", Icon = "??", IsEnabled = true });

            if (prompt.IsArchived)
            {
                _currentActions.Add(new PromptAction { Type = PromptActionType.Restore, Name = "Restore", Description = "Restore from archive", Icon = "?", IsEnabled = true });
            }
            else
            {
                _currentActions.Add(new PromptAction { Type = PromptActionType.Archive, Name = "Archive", Description = "Move to archive", Icon = "??", IsEnabled = true });
            }

            FilterResults();
        }

        private void GoBackToPrompts()
        {
            _showingActions = false;
            _selectedPrompt = null;
            _searchBox.Text = "";
            _hintLabel.Text = "Type to search prompts... Press ESC to close";
            FilterResults();
            _searchBox.Focus();
        }

        private void ResultsList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var item = _resultsList.Items[e.Index];
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Background
            var bgColor = isSelected ? Color.FromArgb(60, 120, 180) : Color.FromArgb(35, 35, 35);
            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            if (_showingActions && item is PromptAction action)
            {
                DrawAction(e.Graphics, e.Bounds, action, isSelected);
            }
            else if (item is PromptInfo prompt)
            {
                DrawPrompt(e.Graphics, e.Bounds, prompt, isSelected);
            }
        }

        private void DrawPrompt(Graphics g, Rectangle bounds, PromptInfo prompt, bool isSelected)
        {
            var textColor = isSelected ? Color.White : Color.LightGray;
            var subTextColor = isSelected ? Color.LightGray : Color.Gray;

            // Icon/Badge area
            var iconRect = new Rectangle(bounds.X + 10, bounds.Y + 15, 40, 20);
            var projectColor = Color.FromArgb(100, 150, 200);
            using (var brush = new SolidBrush(projectColor))
            {
                g.FillRectangle(brush, iconRect);
            }
            using (var font = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                var projectText = string.IsNullOrEmpty(prompt.ProjectName) ? "?" : prompt.ProjectName.Substring(0, Math.Min(3, prompt.ProjectName.Length)).ToUpper();
                g.DrawString(projectText, font, brush, iconRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // Title
            using (var font = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var titleRect = new Rectangle(bounds.X + 60, bounds.Y + 8, bounds.Width - 70, 20);
                g.DrawString(prompt.Title, font, brush, titleRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }

            // Description
            if (!string.IsNullOrEmpty(prompt.Description))
            {
                using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
                using (var brush = new SolidBrush(subTextColor))
                {
                    var descRect = new Rectangle(bounds.X + 60, bounds.Y + 28, bounds.Width - 70, 18);
                    g.DrawString(prompt.Description, font, brush, descRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                }
            }
        }

        private void DrawAction(Graphics g, Rectangle bounds, PromptAction action, bool isSelected)
        {
            var textColor = isSelected ? Color.White : Color.LightGray;
            var subTextColor = isSelected ? Color.LightGray : Color.Gray;

            // Icon
            using (var font = new Font("Segoe UI", 16F))
            using (var brush = new SolidBrush(textColor))
            {
                var iconRect = new Rectangle(bounds.X + 15, bounds.Y + 12, 30, 30);
                g.DrawString(action.Icon, font, brush, iconRect);
            }

            // Name
            using (var font = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var nameRect = new Rectangle(bounds.X + 60, bounds.Y + 10, bounds.Width - 70, 20);
                g.DrawString(action.Name, font, brush, nameRect);
            }

            // Description
            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
            using (var brush = new SolidBrush(subTextColor))
            {
                var descRect = new Rectangle(bounds.X + 60, bounds.Y + 30, bounds.Width - 70, 18);
                g.DrawString(action.Description, font, brush, descRect);
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (_showingActions)
                {
                    GoBackToPrompts();
                }
                else
                {
                    Hide();
                }
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }
    }

    public class PromptActionEventArgs : EventArgs
    {
        public PromptInfo Prompt { get; }
        public PromptAction Action { get; }

        public PromptActionEventArgs(PromptInfo prompt, PromptAction action)
        {
            Prompt = prompt;
            Action = action;
        }
    }
}
