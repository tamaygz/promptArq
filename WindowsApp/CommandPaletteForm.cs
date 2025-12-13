using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        // Delegates for calling web app API (set by MainForm)
        public Func<string, Task<string[]>>? GetPlaceholdersFromWebApp { get; set; }
        public Func<string, Dictionary<string, string>, Task<string>>? FillContentInWebApp { get; set; }
        public Func<string, string?, Task<ExecutionResult>>? ExecutePromptInWebApp { get; set; }

        // State machine for multi-step workflows
        private WorkflowState _workflowState = WorkflowState.SelectingPrompt;
        private List<string> _placeholders = new();
        private Dictionary<string, string> _placeholderValues = new();
        private int _currentPlaceholderIndex = 0;
        private string _filledContent = "";

        private enum WorkflowState
        {
            SelectingPrompt,
            SelectingAction,
            FillingPlaceholder,
            ChoosingOutput
        }

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

            // Close when clicking outside the form
            Deactivate += (s, e) =>
            {
                Hide();
            };
        }

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        public void ShowPalette(List<PromptInfo> prompts)
        {
            _allPrompts = prompts;
            ResetState();
            
            // Reset window state
            WindowState = FormWindowState.Normal;
            
            // Manually center the form on the screen
            var screen = Screen.FromPoint(Cursor.Position);
            Location = new Point(
                screen.WorkingArea.Left + (screen.WorkingArea.Width - Width) / 2,
                screen.WorkingArea.Top + (screen.WorkingArea.Height - Height) / 2
            );
            
            FilterResults();

            // Show the form
            Show();

            // Force the form to receive focus
            Activate();
            BringToFront();
            TopMost = true;

            // Ensure search box gets focus and is ready for input
            _searchBox.Focus();
            _searchBox.Select();
        }


        private void ResetState()
        {
            _workflowState = WorkflowState.SelectingPrompt;
            _showingActions = false;
            _selectedPrompt = null;
            _placeholders.Clear();
            _placeholderValues.Clear();
            _currentPlaceholderIndex = 0;
            _filledContent = "";
            _searchBox.Text = "";
            _hintLabel.Text = "Type to search prompts... Press ESC to close";
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            if (_workflowState != WorkflowState.FillingPlaceholder)
            {
                FilterResults();
            }
        }

        private void FilterResults()
        {
            _resultsList.Items.Clear();

            switch (_workflowState)
            {
                case WorkflowState.SelectingPrompt:
                    FilterPrompts();
                    break;
                
                case WorkflowState.SelectingAction:
                    ShowActions();
                    break;
                
                case WorkflowState.ChoosingOutput:
                    ShowOutputOptions();
                    break;
            }

            // Don't auto-select for SelectingPrompt state - let user navigate with arrow keys
            if (_resultsList.Items.Count > 0 && _workflowState != WorkflowState.SelectingPrompt)
            {
                _resultsList.SelectedIndex = 0;
            }
        }

        private void FilterPrompts()
        {
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
        }

        private void ShowActions()
        {
            foreach (var action in _currentActions)
            {
                _resultsList.Items.Add(action);
            }
        }

        private void ShowOutputOptions()
        {
            // This method is called from ChoosingOutput state in FilterResults
            // The actions are already set by ShowOutputOptionsScreen, so just show them
            foreach (var action in _currentActions)
            {
                _resultsList.Items.Add(action);
            }
        }

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_resultsList.Items.Count > 0 && _workflowState != WorkflowState.FillingPlaceholder)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        // If no selection, select first item, otherwise let list handle navigation
                        if (_resultsList.SelectedIndex == -1)
                        {
                            _resultsList.SelectedIndex = 0;
                        }
                        _resultsList.Focus();
                    }
                    break;

                case Keys.Up:
                    if (_resultsList.Items.Count > 0 && _workflowState != WorkflowState.FillingPlaceholder)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        _resultsList.SelectedIndex = _resultsList.Items.Count - 1;
                        _resultsList.Focus();
                    }
                    break;

                case Keys.Enter:
                    HandleEnter();
                    e.Handled = true;
                    break;

                case Keys.Escape:
                    HandleEscape();
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
                case Keys.Back:
                    HandleEscape();
                    e.Handled = true;
                    break;
            }
        }

        private void ResultsList_DoubleClick(object? sender, EventArgs e)
        {
            HandleSelection();
        }

        private void HandleEnter()
        {
            if (_workflowState == WorkflowState.FillingPlaceholder)
            {
                // Save current placeholder value and move to next
                var currentPlaceholder = _placeholders[_currentPlaceholderIndex];
                _placeholderValues[currentPlaceholder] = _searchBox.Text;
                
                _currentPlaceholderIndex++;
                
                if (_currentPlaceholderIndex < _placeholders.Count)
                {
                    // Show next placeholder
                    AskForNextPlaceholder();
                }
                else
                {
                    // All placeholders filled, show output options
                    FillPlaceholdersInContent();
                    ShowOutputOptionsScreen();
                }
            }
            else
            {
                HandleSelection();
            }
        }

        private void HandleEscape()
        {
            switch (_workflowState)
            {
                case WorkflowState.SelectingPrompt:
                    Hide();
                    break;
                
                case WorkflowState.SelectingAction:
                    GoBackToPrompts();
                    break;
                
                case WorkflowState.FillingPlaceholder:
                    // Go back one placeholder or to action selection
                    if (_currentPlaceholderIndex > 0)
                    {
                        _currentPlaceholderIndex--;
                        AskForNextPlaceholder();
                    }
                    else
                    {
                        GoBackToActions();
                    }
                    break;
                
                case WorkflowState.ChoosingOutput:
                    // Go back to first placeholder
                    _currentPlaceholderIndex = 0;
                    _placeholderValues.Clear();
                    AskForNextPlaceholder();
                    break;
            }
        }

        private void HandleSelection()
        {
            if (_resultsList.SelectedItem == null) return;

            switch (_workflowState)
            {
                case WorkflowState.SelectingPrompt:
                    var prompt = _resultsList.SelectedItem as PromptInfo;
                    if (prompt != null)
                    {
                        ShowActionsForPrompt(prompt);
                    }
                    break;
                
                case WorkflowState.SelectingAction:
                    var action = _resultsList.SelectedItem as PromptAction;
                    if (action != null && _selectedPrompt != null)
                    {
                        if (action.Type == PromptActionType.FillPlaceholders)
                        {
                            StartFillPlaceholdersWorkflow();
                        }
                        else if (action.Type == PromptActionType.Paste || action.Type == PromptActionType.Copy)
                        {
                            // If execute_llm is true, delegate to MainForm for LLM execution
                            if (_selectedPrompt.ExecuteLLM)
                            {
                                ActionSelected?.Invoke(this, new PromptActionEventArgs(_selectedPrompt, action));
                                Hide();
                            }
                            else
                            {
                                // Direct execution - handle paste/copy internally
                                ExecuteAction(action, _selectedPrompt.Content);
                            }
                        }
                        else
                        {
                            // Delegate to MainForm for actions that need WebView2 access
                            ActionSelected?.Invoke(this, new PromptActionEventArgs(_selectedPrompt, action));
                            Hide();
                        }
                    }
                    break;
                
                case WorkflowState.ChoosingOutput:
                    var outputAction = _resultsList.SelectedItem as PromptAction;
                    if (outputAction != null && _selectedPrompt != null)
                    {
                        // Check if this is the "Copy Generated Prompt" action or needs LLM execution
                        bool isCopyGenerated = outputAction.Name == "Copy Generated Prompt";
                        bool needsLLMExecution = _selectedPrompt.ExecuteLLM && !isCopyGenerated;
                        
                        if (needsLLMExecution)
                        {
                            // Create a temporary prompt with filled content for LLM execution
                            var tempPrompt = new PromptInfo
                            {
                                Id = _selectedPrompt.Id,
                                Title = _selectedPrompt.Title,
                                Content = _filledContent,
                                ExecuteLLM = true
                            };
                            ActionSelected?.Invoke(this, new PromptActionEventArgs(tempPrompt, outputAction));
                            Hide();
                        }
                        else
                        {
                            // Direct execution or copy generated
                            ExecuteAction(outputAction, _filledContent);
                        }
                    }
                    break;
            }
        }

        private void ShowActionsForPrompt(PromptInfo prompt)
        {
            _selectedPrompt = prompt;
            _workflowState = WorkflowState.SelectingAction;
            _showingActions = true;
            _searchBox.Text = "";
            _hintLabel.Text = $"Actions for: {prompt.Title}  |  Press ESC or Backspace to go back";

            _currentActions = new List<PromptAction>();

            // Different actions based on execute_llm flag
            if (prompt.ExecuteLLM)
            {
                _currentActions.Add(new PromptAction { Type = PromptActionType.Paste, Name = "Execute & Paste", Description = "Execute through LLM and paste", Icon = "??", IsEnabled = true });
                _currentActions.Add(new PromptAction { Type = PromptActionType.Copy, Name = "Execute & Copy", Description = "Execute through LLM and copy", Icon = "??", IsEnabled = true });
            }
            else
            {
                _currentActions.Add(new PromptAction { Type = PromptActionType.Paste, Name = "Paste", Description = "Paste to current focus", Icon = "??", IsEnabled = true });
                _currentActions.Add(new PromptAction { Type = PromptActionType.Copy, Name = "Copy to Clipboard", Description = "Copy prompt content", Icon = "??", IsEnabled = true });
            }

            _currentActions.Add(new PromptAction { Type = PromptActionType.OpenInEditor, Name = "Open in Editor", Description = "Edit this prompt", Icon = "??", IsEnabled = true });

            if (prompt.HasPlaceholders)
            {
                _currentActions.Insert(0, new PromptAction 
                { 
                    Type = PromptActionType.FillPlaceholders, 
                    Name = "Fill Placeholders", 
                    Description = "Fill in template variables", 
                    Icon = "??", 
                    IsEnabled = true 
                });
            }

            // _currentActions.Add(new PromptAction { Type = PromptActionType.Export, Name = "Export", Description = "Export to JSON file", Icon = "??", IsEnabled = true });
            // _currentActions.Add(new PromptAction { Type = PromptActionType.Share, Name = "Share", Description = "Generate share link", Icon = "??", IsEnabled = true });

            // if (prompt.IsArchived)
            // {
            //     _currentActions.Add(new PromptAction { Type = PromptActionType.Restore, Name = "Restore", Description = "Restore from archive", Icon = "??", IsEnabled = true });
            // }
            // else
            // {
            //     _currentActions.Add(new PromptAction { Type = PromptActionType.Archive, Name = "Archive", Description = "Move to archive", Icon = "??", IsEnabled = true });
            // }

            FilterResults();
        }

        private async void StartFillPlaceholdersWorkflow()
        {
            if (_selectedPrompt == null || GetPlaceholdersFromWebApp == null) return;

            try
            {
                // Get placeholders from web app API (no more regex parsing!)
                _placeholders = (await GetPlaceholdersFromWebApp(_selectedPrompt.Id)).ToList();

                if (_placeholders.Count == 0)
                {
                    ShowToast("No placeholders found in this prompt", 2000);
                    return;
                }

                _placeholderValues.Clear();
                _currentPlaceholderIndex = 0;
                
                AskForNextPlaceholder();
            }
            catch (Exception ex)
            {
                ShowToast($"Error getting placeholders: {ex.Message}", 3000);
                GoBackToActions();
            }
        }

        private void AskForNextPlaceholder()
        {
            _workflowState = WorkflowState.FillingPlaceholder;
            
            var currentPlaceholder = _placeholders[_currentPlaceholderIndex];
            var previousValue = _placeholderValues.ContainsKey(currentPlaceholder) 
                ? _placeholderValues[currentPlaceholder] 
                : "";
            
            _searchBox.Text = previousValue;
            _searchBox.SelectAll();
            
            _hintLabel.Text = $"Fill placeholder ({_currentPlaceholderIndex + 1}/{_placeholders.Count}): {currentPlaceholder}  |  Press Enter to continue, ESC to go back";
            
            _resultsList.Items.Clear();
            _resultsList.Items.Add($"Enter value for: {currentPlaceholder}");
            
            _searchBox.Focus();
        }

        private async void FillPlaceholdersInContent()
        {
            if (_selectedPrompt == null || FillContentInWebApp == null) return;

            try
            {
                // Use web app API to fill placeholders (no more regex replacement!)
                _filledContent = await FillContentInWebApp(_selectedPrompt.Id, _placeholderValues);
                ShowOutputOptionsScreen();
            }
            catch (Exception ex)
            {
                ShowToast($"Error filling placeholders: {ex.Message}", 3000);
                GoBackToActions();
            }
        }

        private void ShowOutputOptionsScreen()
        {
            _workflowState = WorkflowState.ChoosingOutput;
            _searchBox.Text = "";
            _hintLabel.Text = "All placeholders filled! Choose output method  |  Press ESC to edit values";
            
            // Clear and rebuild actions for output screen
            _currentActions.Clear();
            
            // Add execute actions based on execute_llm flag first
            if (_selectedPrompt?.ExecuteLLM == true)
            {
                _currentActions.Add(new PromptAction 
                { 
                    Type = PromptActionType.Paste, 
                    Name = "Execute & Paste", 
                    Description = "Execute through LLM and paste to active window", 
                    Icon = "??", 
                    IsEnabled = true 
                });
                _currentActions.Add(new PromptAction 
                { 
                    Type = PromptActionType.Copy, 
                    Name = "Execute & Copy", 
                    Description = "Execute through LLM and copy to clipboard", 
                    Icon = "??", 
                    IsEnabled = true 
                });
            }
            else
            {
                _currentActions.Add(new PromptAction 
                { 
                    Type = PromptActionType.Paste, 
                    Name = "Paste to Active Window", 
                    Description = "Paste filled prompt to active window", 
                    Icon = "??", 
                    IsEnabled = true 
                });
            }
            
            // Always add "Copy generated prompt" below execute options
            _currentActions.Add(new PromptAction 
            { 
                Type = PromptActionType.Copy, 
                Name = "Copy Generated Prompt", 
                Description = "Copy the filled template to clipboard", 
                Icon = "??", 
                IsEnabled = true 
            });
            
            FilterResults();
        }

        private async void ExecuteAction(PromptAction action, string content)
        {
            if (string.IsNullOrEmpty(content) || _selectedPrompt == null) return;

            try
            {
                // Check if this is LLM execution (first Copy/Paste in the list)
                bool isLLMExecution = _selectedPrompt.ExecuteLLM && 
                    (_currentActions.IndexOf(action) == 0 || 
                     (_currentActions.Count > 1 && _currentActions.IndexOf(action) == 1 && action.Type == PromptActionType.Copy));

                string finalContent = content;

                if (isLLMExecution && ExecutePromptInWebApp != null)
                {
                    // Execute through LLM using web app API
                    ShowToast("Executing through LLM...", 2000);
                    var result = await ExecutePromptInWebApp(_selectedPrompt.Id, content);

                    if (result.Success && result.Result != null)
                    {
                        finalContent = result.Result;
                    }
                    else
                    {
                        ShowToast($"LLM execution failed: {result.Error}", 3000);
                        return;
                    }
                }

                // Now paste or copy the final content
                if (action.Type == PromptActionType.Paste)
                {
                    Clipboard.SetText(finalContent);
                    Hide();
                    System.Threading.Thread.Sleep(300);
                    SendKeys.SendWait("^v");
                }
                else if (action.Type == PromptActionType.Copy)
                {
                    Clipboard.SetText(finalContent);
                    Hide();
                    ShowToast(isLLMExecution ? "LLM result copied!" : "Prompt copied to clipboard!", 2000);
                }

                ResetState();
            }
            catch (Exception ex)
            {
                ShowToast($"Error: {ex.Message}", 3000);
            }
        }

        private void GoBackToPrompts()
        {
            _workflowState = WorkflowState.SelectingPrompt;
            _showingActions = false;
            _selectedPrompt = null;
            _searchBox.Text = "";
            _hintLabel.Text = "Type to search prompts... Press ESC to close";
            FilterResults();
            _searchBox.Focus();
        }

        private void GoBackToActions()
        {
            if (_selectedPrompt != null)
            {
                ShowActionsForPrompt(_selectedPrompt);
            }
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

            if (_workflowState == WorkflowState.FillingPlaceholder && item is string text)
            {
                DrawPlaceholderPrompt(e.Graphics, e.Bounds, text, isSelected);
            }
            else if (item is PromptAction action)
            {
                DrawAction(e.Graphics, e.Bounds, action, isSelected);
            }
            else if (item is PromptInfo prompt)
            {
                DrawPrompt(e.Graphics, e.Bounds, prompt, isSelected);
            }
        }

        private void DrawPlaceholderPrompt(Graphics g, Rectangle bounds, string text, bool isSelected)
        {
            var textColor = Color.LightGray;
            using (var font = new Font("Segoe UI", 10F, FontStyle.Italic))
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(bounds.X + 15, bounds.Y + 15, bounds.Width - 30, 20);
                g.DrawString(text, font, brush, textRect);
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
                g.DrawString(action.Description, font, brush, descRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                HandleEscape();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void ShowToast(string message, int durationMs = 2000)
        {
            var toast = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                Size = new Size(300, 60),
                Opacity = 0.95
            };

            var label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.White,
                Padding = new Padding(10)
            };

            toast.Controls.Add(label);

            // Position at bottom center of screen
            var screen = Screen.FromPoint(Cursor.Position);
            toast.Location = new Point(
                screen.WorkingArea.Left + (screen.WorkingArea.Width - toast.Width) / 2,
                screen.WorkingArea.Bottom - toast.Height - 50
            );

            // Rounded corners
            toast.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, toast.Width, toast.Height, 10, 10));

            toast.Show();

            // Auto-close after duration
            var timer = new System.Windows.Forms.Timer { Interval = durationMs };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                toast.Close();
                toast.Dispose();
            };
            timer.Start();
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
