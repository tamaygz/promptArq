using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PromptArqApp.Theming;
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Registry;
using Serilog;

namespace PromptArqApp
{
    public partial class CommandPaletteForm : BorderlessFormBase
    {
        private TextBox _searchBox = null!;
        private ListBox _resultsList = null!;
        private Label _hintLabel = null!;
        private Panel _headerPanel = null!;
        private Panel _contentPanel = null!;
        private TextDisplayPanel _textDisplayPanel = null!;

        private List<PromptInfo> _allPrompts = new();
        private List<PromptAction> _currentActions = new();

        private PromptHistory _history = null!;
        private AppSettings _settings = null!;
        private HashSet<string> _recentPromptIds = new HashSet<string>();

        // Workflow engine fields (always used - no fallback)
        private readonly WorkflowEngine? _workflowEngine;
        private readonly IWorkflowRegistry? _workflowRegistry;
        private PromptArqApp.Workflow.Core.Workflow? _currentWorkflow;
        private IWorkflowNode? _currentNode;
        private WorkflowContext? _workflowContext;

        // Flag to suppress Deactivate hide when showing text display
        private bool _isShowingTextDisplay = false;

        // Constants for suggestion UI
        private const string SuggestionPrefix = "💡 ";
        private const string SuggestionSeparator = "─────── Recent Values ───────";

        public event EventHandler<PromptActionEventArgs>? ActionSelected;

        // Delegates for calling web app API (set by MainForm)
        public Func<string, Task<string[]>>? GetPlaceholdersFromWebApp { get; set; }
        public Func<string, Dictionary<string, string>, Task<string>>? FillContentInWebApp { get; set; }
        public Func<string, string?, Task<ExecutionResult>>? ExecutePromptInWebApp { get; set; }
        public Func<Task<List<SystemPromptInfo>>>? GetSystemPromptsFromWebApp { get; set; }
        public Func<string, string, Task<ExecutionResult>>? ExecuteOneTimePromptFromWebApp { get; set; }
        public Action<string>? NotifyAction { get; set; }

        // Legacy state fields removed - all workflows now use WorkflowEngine

        public CommandPaletteForm(PromptHistory history, AppSettings settings)
        {
            _history = history;
            _settings = settings;
            InitializeComponent();
            SetupCustomComponents();

            // Initialize text display panel
            _textDisplayPanel = new TextDisplayPanel();

            // Initialize workflow engine
            try
            {
                Log.Debug("[CommandPalette] About to request IWorkflowRegistry from service container...");
                _workflowRegistry = ServiceConfiguration.GetService<IWorkflowRegistry>();
                Log.Debug($"[CommandPalette] GetService returned: {(_workflowRegistry == null ? "NULL" : "SUCCESS")}");
                
                if (_workflowRegistry != null)
                {
                    Log.Debug("[CommandPalette] Creating WorkflowEngine...");
                    _workflowEngine = new WorkflowEngine(_workflowRegistry, ServiceConfiguration.ServiceProvider);
                    Log.Information("[CommandPalette] Workflow engine initialized successfully");
                }
                else
                {
                    Log.Warning("[CommandPalette] WorkflowRegistry is null - workflow engine not initialized");
                }
            }
            catch (Exception ex)
            {
                // Workflow engine is required - log error
                Log.Error(ex, "Failed to initialize workflow engine - application may not function correctly");
            }

            // Register with ThemeManager and apply theme
            ThemeManager.Instance.RegisterForm(this);
            ThemeManager.Instance.ApplyThemeToForm(this);

            // Subscribe to theme changes
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;

            // Cleanup event handler when form is disposed
            FormClosing += (s, e) =>
            {
                ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
            };
        }

        protected override bool IsInDraggableArea(Point clientPoint)
        {
            // Only allow dragging from the header panel area
            return clientPoint.Y < _headerPanel?.Height;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ApplyCurrentTheme()));
            }
            else
            {
                ApplyCurrentTheme();
            }
        }

        private void ApplyCurrentTheme()
        {
            ThemeManager.Instance.ApplyThemeToForm(this);

            // Force redraw of custom-drawn ListBox
            if (_resultsList != null && _resultsList.DrawMode != DrawMode.Normal)
            {
                _resultsList.Invalidate();
            }
        }

        private void SetupCustomComponents()
        {
            // Form settings
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(700, 500);
            TopMost = true;
            ShowInTaskbar = false;

            // Header panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(20, 15, 20, 15)
            };

            // Search box
            _searchBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Text = "",
                Multiline = true,
                MaxLength = 100000, // Allow long prompts to be pasted
                ScrollBars = ScrollBars.None, // Initially hidden, shown after 5 lines
                WordWrap = true,
                Padding = new Padding(0),
                Margin = new Padding(0),
                TabStop = true,
                TabIndex = 0,
                AcceptsReturn = false, // Prevent Enter from adding newlines
                AcceptsTab = false,    // Prevent Tab from adding tabs
                Enabled = true
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.KeyDown += SearchBox_KeyDown;

            var searchPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 10, 15, 10)
            };
            searchPanel.Controls.Add(_searchBox);

            _headerPanel.Controls.Add(searchPanel);

            // Hint label
            _hintLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                Text = "Type to search prompts... Press ESC to close",
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Content panel
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Results list
            _resultsList = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
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
                    _isShowingTextDisplay = false;
                    _textDisplayPanel?.Hide();
                    TopMost = false;
                    Hide();
                }
                else
                {
                    // Form is actually closing (not just hiding), clean up text display panel
                    _isShowingTextDisplay = false;
                    _textDisplayPanel?.Hide();
                }
            };

            // Close when clicking outside the form
            Deactivate += (s, e) =>
            {
                // Don't auto-hide if we're currently showing the text display
                // (use flag to avoid race condition with Visible property)
                if (_isShowingTextDisplay)
                {
                    Log.Debug("[CommandPalette] Deactivate suppressed - showing text display");
                    return;
                }
                
                _isShowingTextDisplay = false;
                _textDisplayPanel?.Hide();
                TopMost = false;
                Hide();
            };

            // Ensure focus is set when form is shown
            Shown += (s, e) =>
            {
                _searchBox.Focus();
            };
        }

        #region Workflow Engine Methods

        private void InitializeWorkflowContext()
        {
            if (_workflowEngine == null) return;

            _workflowContext = new WorkflowContext(ServiceConfiguration.ServiceProvider);
            
            // Populate context with data
            _workflowContext.Set("allPrompts", _allPrompts);
            _workflowContext.Set("searchQuery", "");
            
            // Add delegates to context
            if (GetPlaceholdersFromWebApp != null)
                _workflowContext.Set("GetPlaceholdersFromWebApp", GetPlaceholdersFromWebApp);
            if (FillContentInWebApp != null)
                _workflowContext.Set("FillContentInWebApp", FillContentInWebApp);
            if (ExecutePromptInWebApp != null)
                _workflowContext.Set("ExecutePromptInWebApp", ExecutePromptInWebApp);
            if (GetSystemPromptsFromWebApp != null)
                _workflowContext.Set("GetSystemPromptsFromWebApp", GetSystemPromptsFromWebApp);
            if (ExecuteOneTimePromptFromWebApp != null)
                _workflowContext.Set("ExecuteOneTimePromptFromWebApp", ExecuteOneTimePromptFromWebApp);
            if (NotifyAction != null)
                _workflowContext.Set("NotifyAction", NotifyAction);
        }

        private async Task StartDefaultWorkflowAsync()
        {
            Log.Debug("[CommandPalette] StartDefaultWorkflowAsync - Entry");
            if (_workflowEngine == null || _workflowRegistry == null || _workflowContext == null)
            {
                Log.Error($"[CommandPalette] Cannot start workflow - Engine:{_workflowEngine == null}, Registry:{_workflowRegistry == null}, Context:{_workflowContext == null}");
                return;
            }

            try
            {
                // Start with the preferred workflow (defaulting to quick-paste)
                var startingWorkflowId = ResolveStartingWorkflowId();
                if (string.IsNullOrWhiteSpace(startingWorkflowId))
                {
                    Log.Error("[CommandPalette] No enabled workflows available to run");
                    return;
                }

                Log.Debug($"[CommandPalette] Looking for workflow: {startingWorkflowId}");
                var allWorkflows = _workflowRegistry.GetAllWorkflows().ToList();
                Log.Debug($"[CommandPalette] Available workflows: {string.Join(", ", allWorkflows.Select(w => w.Id))}");

                _currentWorkflow = _workflowRegistry.GetWorkflow(startingWorkflowId);
                
                if (_currentWorkflow == null)
                {
                    Log.Error($"[CommandPalette] Starting workflow '{startingWorkflowId}' not found in registry");
                    return;
                }

                Log.Debug($"[CommandPalette] Found workflow: {_currentWorkflow.Name}, EntryNode: {_currentWorkflow.EntryNodeId}");
                var result = await _workflowEngine.StartWorkflowAsync(startingWorkflowId, _workflowContext);
                Log.Debug($"[CommandPalette] Workflow started. Success: {result.IsSuccess}, CurrentNode: {_workflowEngine.CurrentNode?.Name}");
                
                _currentNode = _workflowEngine.CurrentNode;
                _workflowContext = result.Context;

                await ProcessNodeResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[CommandPalette] Error starting workflow");
            }
        }

        private async Task ExecuteCurrentNodeAsync()
        {
            Log.Debug($"[CommandPalette] ExecuteCurrentNodeAsync - Entry. CurrentNode: {_currentNode?.Id ?? "null"}");
            
            if (_currentNode == null || _workflowContext == null || _workflowEngine == null)
            {
                Log.Warning($"[CommandPalette] ExecuteCurrentNodeAsync - Missing required objects. Node:{_currentNode == null}, Context:{_workflowContext == null}, Engine:{_workflowEngine == null}");
                return;
            }

            try
            {
                Log.Debug($"[CommandPalette] Calling WorkflowEngine.ExecuteNodeAsync for node: {_currentNode.Id}");
                var result = await _workflowEngine.ExecuteNodeAsync(_currentNode, _workflowContext);
                Log.Debug($"[CommandPalette] Node execution completed. IsSuccess: {result.IsSuccess}, NextNodeId: {result.NextNodeId ?? "null"}");
                
                _workflowContext = result.Context;
                await ProcessNodeResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[CommandPalette] Error executing node: {_currentNode.Id}");
                System.Diagnostics.Debug.WriteLine($"Error executing node: {ex.Message}");
            }
        }

        private async Task ProcessNodeResult(WorkflowResult result)
        {
            Log.Debug($"[CommandPalette] ProcessNodeResult - IsSuccess: {result.IsSuccess}, NextNodeId: {result.NextNodeId ?? "null"}, CurrentNode: {_currentNode?.Id ?? "null"}");
            
            if (!result.IsSuccess)
            {
                Log.Error($"[CommandPalette] Node execution failed: {result.ErrorMessage}");
                System.Diagnostics.Debug.WriteLine($"Node execution failed: {result.ErrorMessage}");
                return;
            }

            // Check if we need to switch workflows
            if (_workflowContext != null && _workflowContext.Has("switchToWorkflow"))
            {
                        var targetWorkflowId = _workflowContext.Get<string>("switchToWorkflow");
                Log.Debug($"[CommandPalette] Switching to workflow: {targetWorkflowId}");
                _workflowContext.Remove("switchToWorkflow");
                
                        // Start the new workflow if it remains enabled
                        if (!_settings.IsWorkflowEnabled(targetWorkflowId))
                        {
                            Log.Warning($"[CommandPalette] Workflow '{targetWorkflowId}' is disabled, skipping switch");
                            return;
                        }

                        if (_workflowEngine != null && _workflowRegistry != null)
                        {
                            try
                            {
                                _currentWorkflow = _workflowRegistry.GetWorkflow(targetWorkflowId);
                                if (_currentWorkflow != null)
                                {
                                    var newResult = await _workflowEngine.StartWorkflowAsync(targetWorkflowId, _workflowContext);
                                    _currentNode = _workflowEngine.CurrentNode;
                                    _workflowContext = newResult.Context;
                                    RenderNodeUI();
                                    return;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, $"[CommandPalette] Error switching workflow");
                                System.Diagnostics.Debug.WriteLine($"Error switching workflow: {ex.Message}");
                            }
                        }
            }

            // Check if workflow wants to close
            if (_workflowContext != null && _workflowContext.GetOrDefault<bool>("closePalette", false))
            {
                Log.Debug("[CommandPalette] Workflow requested palette close");
                _isShowingTextDisplay = false;
                _textDisplayPanel?.Hide();
                TopMost = false;
                Hide();
                _workflowEngine?.Reset();
                return;
            }

            // Move to next node if specified
            if (!string.IsNullOrEmpty(result.NextNodeId) && _currentWorkflow != null && _workflowEngine != null)
            {
                Log.Debug($"[CommandPalette] Moving to explicit next node: {result.NextNodeId}");
                var nextNodeDef = _currentWorkflow.GetNodeById(result.NextNodeId);
                if (nextNodeDef != null)
                {
                    _currentNode = _workflowRegistry?.CreateNode(nextNodeDef.NodeType, nextNodeDef.Configuration);
                    if (_currentNode != null)
                    {
                        // Set the node's ID to match the workflow definition's ID
                        if (_currentNode is PromptArqApp.Workflow.Nodes.WorkflowNodeBase nodeBase)
                        {
                            nodeBase.Id = nextNodeDef.Id;
                            
                            // If this is a ConditionalNode and workflow has branches for it, configure them
                            if (_currentNode is PromptArqApp.Workflow.Nodes.Utility.ConditionalNode conditionalNode && 
                                _currentWorkflow.Branches != null && 
                                _currentWorkflow.Branches.TryGetValue(nextNodeDef.Id, out var branches))
                            {
                                var config = new Dictionary<string, object>(nextNodeDef.Configuration)
                                {
                                    ["branches"] = branches
                                };
                                conditionalNode.Configure(config);
                            }
                        }
                        
                        // Render UI for the new node if it provides UI
                        RenderNodeUI();
                        
                        // If node doesn't need user input, execute it immediately
                        if (_currentNode is not INodeUIProvider)
                        {
                            await ExecuteCurrentNodeAsync();
                        }
                    }
                }
            }
            else if (_currentWorkflow != null)
            {
                // Check if there's a default next node in connections
                var nextNodeId = _currentWorkflow.GetNextNodeId(_currentNode?.Id ?? "");
                Log.Debug($"[CommandPalette] Checking default next node for '{_currentNode?.Id ?? "null"}': {nextNodeId ?? "null"}");
                
                // Only auto-advance if:
                // 1. There's a next node, AND
                // 2. Either the current node doesn't provide UI, OR it has processed user input (selectedItem exists)
                bool shouldAdvance = nextNodeId != null;
                if (shouldAdvance && _currentNode is INodeUIProvider)
                {
                    // UI nodes should only advance if they've processed user input
                    shouldAdvance = _workflowContext != null && _workflowContext.Has("selectedItem");
                    Log.Debug($"[CommandPalette] Current node is INodeUIProvider, has selectedItem: {shouldAdvance}");
                }
                
                if (shouldAdvance && _workflowEngine != null && _workflowContext != null)
                {
                    Log.Debug($"[CommandPalette] Moving to default next node: {nextNodeId}");
                    
                    var nextResult = await _workflowEngine.MoveToNextNodeAsync(nextNodeId, _workflowContext);
                    _currentNode = _workflowEngine.CurrentNode;
                    _workflowContext = nextResult.Context;
                    
                    // Clear selectedItem after moving to prevent persistence to the next node
                    _workflowContext.Remove("selectedItem");
                    
                    RenderNodeUI();
                    
                    // If node doesn't need user input, execute it immediately
                    if (_currentNode is not INodeUIProvider)
                    {
                        await ExecuteCurrentNodeAsync();
                    }
                }
                else
                {
                    // No navigation occurred - render current node UI
                    // This handles the initial workflow start where node just completes successfully
                    Log.Debug("[CommandPalette] Not advancing, rendering current node UI");
                    RenderNodeUI();
                }
            }
        }

        private void RenderNodeUI()
        {
            Log.Debug($"[CommandPalette] RenderNodeUI - CurrentNode is INodeUIProvider: {_currentNode is INodeUIProvider}, Context null: {_workflowContext == null}");
            
            if (_currentNode is not INodeUIProvider uiProvider || _workflowContext == null)
            {
                Log.Warning($"[CommandPalette] Cannot render UI - Node type: {_currentNode?.GetType().Name}, has UI provider: {_currentNode is INodeUIProvider}");
                return;
            }

            Log.Debug($"[CommandPalette] Rendering UI for node: {_currentNode.Name}, UIType: {uiProvider.UIType}");
            
            // Always hide text display panel first, then show it only if needed
            // This ensures it closes when transitioning between nodes
            _isShowingTextDisplay = false;
            _textDisplayPanel?.Hide();
            
            // Update hint text
            _hintLabel.Text = uiProvider.HintText;
            
            // Update search box state
            _searchBox.ReadOnly = uiProvider.ReadOnly;
            
            // Clear current search
            _searchBox.Text = "";
            
            // Render based on UI type
            switch (uiProvider.UIType)
            {
                case NodeUIType.ItemList:
                    Log.Debug("[CommandPalette] Rendering ItemList - calling FilterResults");
                    // FilterResults will call GetItems on the node
                    FilterResults();
                    break;
                    
                case NodeUIType.TextInput:
                    Log.Debug("[CommandPalette] Rendering TextInput - calling FilterResults");
                    // Show suggestions if available
                    FilterResults();
                    // Set focus to search box for text input
                    ActiveControl = _searchBox;
                    _searchBox.Focus();
                    _searchBox.SelectAll();
                    break;
                    
                case NodeUIType.TextDisplay:
                    Log.Debug("[CommandPalette] Rendering TextDisplay - showing TextDisplayPanel");
                    if (_currentNode is INodeTextProvider textProvider)
                    {
                        var textContent = textProvider.GetTextContent(_workflowContext);
                        Log.Debug($"[CommandPalette] TextDisplay content length: {textContent?.Length ?? 0}");
                        if (_textDisplayPanel != null)
                        {
                            Log.Debug("[CommandPalette] Setting _isShowingTextDisplay flag and showing panel");
                            _isShowingTextDisplay = true;
                            _textDisplayPanel.ShowText(textContent, this);
                            Log.Debug("[CommandPalette] TextDisplayPanel shown");
                            
                            // Ensure command palette stays visible and on top
                            this.BringToFront();
                            this.Activate();
                            this.Focus();
                        }
                        else
                        {
                            Log.Warning("[CommandPalette] TextDisplayPanel is null!");
                        }
                    }
                    else
                    {
                        Log.Warning($"[CommandPalette] Current node does not implement INodeTextProvider: {_currentNode?.GetType().Name}");
                    }
                    // Show items in the main list (e.g., action buttons)
                    Log.Debug("[CommandPalette] Calling FilterResults to show actions");
                    FilterResults();
                    // Set focus to search box
                    ActiveControl = _searchBox;
                    _searchBox.Focus();
                    break;
                    
                default:
                    Log.Debug($"[CommandPalette] Rendering default type: {uiProvider.UIType}");
                    FilterResults();
                    break;
            }
        }

        #endregion

        private string? ResolveStartingWorkflowId()
        {
            if (_workflowRegistry == null)
                return null;

            const string defaultWorkflowId = "quick-paste";
            if (_settings.IsWorkflowEnabled(defaultWorkflowId))
                return defaultWorkflowId;

            return _workflowRegistry.GetAllWorkflows()
                .FirstOrDefault(w => _settings.IsWorkflowEnabled(w.Id))
                ?.Id;
        }

        public async void ShowPalette(List<PromptInfo> prompts)
        {
            Log.Debug($"[CommandPalette] ShowPalette called with {prompts.Count} prompts");
            _allPrompts = prompts;
            
            // Reset window state
            WindowState = FormWindowState.Normal;

            // Manually center the form on the screen
            var screen = Screen.FromPoint(Cursor.Position);
            Location = new Point(
                screen.WorkingArea.Left + (screen.WorkingArea.Width - Width) / 2,
                screen.WorkingArea.Top + (screen.WorkingArea.Height - Height) / 2
            );

            // Show the form and ensure it gets focus
            TopMost = true;
            Show();
            Activate();
            BringToFront();

            // Use ActiveControl property instead of Focus() - this is the recommended approach
            // per Microsoft documentation for setting focus to controls after form show/hide cycles
            ActiveControl = _searchBox;
            _searchBox.Select(0, 0);
            
            Log.Debug($"[CommandPalette] WorkflowEngine null? {_workflowEngine == null}, Registry null? {_workflowRegistry == null}");
            if (_workflowEngine != null && _workflowRegistry != null)
            {
                // Use new workflow system - await to ensure workflow is initialized before FilterResults
                Log.Debug("[CommandPalette] Initializing workflow context...");
                InitializeWorkflowContext();
                Log.Debug("[CommandPalette] Starting default workflow...");
                await StartDefaultWorkflowAsync();
                Log.Debug("[CommandPalette] Default workflow started");
            }
            else
            {
                Log.Warning("[CommandPalette] Workflow engine or registry is null, using fallback");
                // Fallback if workflow engine not initialized
                FilterResults();
            }
        }


        private void ResetState()
        {
            _searchBox.Text = "";
            _searchBox.ReadOnly = false;
            _hintLabel.Text = "Type to search prompts... Press ESC to close";

            // Hide text display panel
            _isShowingTextDisplay = false;
            _textDisplayPanel?.Hide();

            // Clear any selection in results list to prevent focus issues
            _resultsList.ClearSelected();
            _resultsList.SelectedIndex = -1;
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            // Dynamically adjust search box height based on content
            AdjustSearchBoxHeight();

            // Always filter results when text changes (workflow engine handles state)
            FilterResults();
        }

        private void AdjustSearchBoxHeight()
        {
            // Get line count
            int lineCount = _searchBox.GetLineFromCharIndex(_searchBox.TextLength) + 1;
            int maxLines = 5;
            int lineHeight = _searchBox.Font.Height;
            int padding = 10;

            // Calculate heights
            int linesForHeight = Math.Min(lineCount, maxLines);
            int desiredHeight = (lineHeight * linesForHeight) + padding;
            int headerHeight = desiredHeight + 40; // Add header panel padding

            // Only update if changed
            if (_headerPanel.Height != headerHeight)
            {
                _headerPanel.Height = headerHeight;
            }

            // Show scrollbar only when exceeding 5 lines
            var newScrollBars = lineCount > maxLines ? ScrollBars.Vertical : ScrollBars.None;
            if (_searchBox.ScrollBars != newScrollBars)
            {
                _searchBox.ScrollBars = newScrollBars;
            }
        }

        private void FilterResults()
        {
            Log.Debug($"[CommandPalette] FilterResults called. CurrentNode: {_currentNode?.Name}, Context null: {_workflowContext == null}");
            _resultsList.Items.Clear();

            // Use workflow node if available
            if (_currentNode is INodeUIProvider uiProvider && _workflowContext != null)
            {
                // Update search query in context
                _workflowContext.Set("searchQuery", _searchBox.Text.Trim());
                
                Log.Debug($"[CommandPalette] Getting items from node: {_currentNode.Name}");
                // Get items from node
                var items = uiProvider.GetItems(_workflowContext).ToList();
                Log.Debug($"[CommandPalette] Node returned {items.Count} items");
                
                foreach (var item in items)
                {
                    _resultsList.Items.Add(item);
                    Log.Debug($"[CommandPalette] Added item: {item?.GetType().Name}");
                }
                
                Log.Debug($"[CommandPalette] ResultsList now has {_resultsList.Items.Count} items");
                
                // Auto-select first item if not searching
                if (_resultsList.Items.Count > 0 && string.IsNullOrEmpty(_searchBox.Text))
                {
                    // Don't auto-select to allow arrow key navigation
                }
                
                return;
            }

            Log.Debug("[CommandPalette] FilterResults - workflow not ready, returning early");
            // FilterResults called before workflow ready - this is normal timing during initialization
            // RenderNodeUI will call it again once workflow initializes
            // Silently return without warning as this is expected behavior
            return;
        }

        private void FilterPrompts()
        {
            var query = _searchBox.Text.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(query))
            {
                // Add "Co-Author One Time Prompt" as first item in empty state
                var oneTimePromptAction = new PromptAction
                {
                    Type = PromptActionType.CoAuthorOneTimePrompt,
                    Name = "Co-Author One Time Prompt",
                    Description = "Execute a prompt with AI system guidance",
                    Icon = "✨",
                    IsEnabled = true
                };
                _resultsList.Items.Add(oneTimePromptAction);

                // Show last used prompts if feature is enabled
                if (_settings.ShowLastUsedPrompts)
                {
                    var recentPrompts = _history.GetRecentPrompts();
                    _recentPromptIds = new HashSet<string>(recentPrompts.Select(p => p.PromptId));

                    // Add recent prompts first
                    foreach (var recentEntry in recentPrompts)
                    {
                        var prompt = _allPrompts.FirstOrDefault(p => p.Id == recentEntry.PromptId);
                        if (prompt != null)
                        {
                            _resultsList.Items.Add(prompt);
                        }
                    }

                    // Fill remaining space with other prompts
                    var remainingCount = 50 - _resultsList.Items.Count;
                    if (remainingCount > 0)
                    {
                        foreach (var prompt in _allPrompts.Where(p => !_recentPromptIds.Contains(p.Id)).Take(remainingCount))
                        {
                            _resultsList.Items.Add(prompt);
                        }
                    }
                }
                else
                {
                    _recentPromptIds.Clear();
                    // Show all prompts as before
                    foreach (var prompt in _allPrompts.Take(50))
                    {
                        _resultsList.Items.Add(prompt);
                    }
                }
            }
            else
            {
                _recentPromptIds.Clear();
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

        // Legacy ShowActions() and ShowOutputOptions() methods removed - workflow nodes handle this

        private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (_resultsList.Items.Count > 0)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        // Navigate down through list, wrap to top if at end
                        if (_resultsList.SelectedIndex == -1)
                        {
                            _resultsList.SelectedIndex = 0;
                        }
                        else if (_resultsList.SelectedIndex < _resultsList.Items.Count - 1)
                        {
                            _resultsList.SelectedIndex++;
                        }
                        else
                        {
                            _resultsList.SelectedIndex = 0; // Wrap to top
                        }
                        // Ensure selected item is visible
                        EnsureSelectedItemVisible();
                        // Keep focus on search box
                        _searchBox.Focus();
                    }
                    break;

                case Keys.Up:
                    if (_resultsList.Items.Count > 0)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        // Navigate up through list, wrap to bottom if at top
                        if (_resultsList.SelectedIndex <= 0)
                        {
                            _resultsList.SelectedIndex = _resultsList.Items.Count - 1; // Wrap to bottom
                        }
                        else
                        {
                            _resultsList.SelectedIndex--;
                        }
                        // Ensure selected item is visible
                        EnsureSelectedItemVisible();
                        // Keep focus on search box
                        _searchBox.Focus();
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
            // If list somehow gets focus (e.g., user clicked on it), redirect keyboard input to search box
            // while handling special keys
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (TrySelectSuggestion())
                    {
                        // Suggestion was selected, focus search box
                    }
                    else
                    {
                        HandleEnter();
                    }
                    e.Handled = true;
                    _searchBox.Focus();
                    break;

                case Keys.Escape:
                case Keys.Back:
                    HandleEscape();
                    e.Handled = true;
                    _searchBox.Focus();
                    break;

                case Keys.Up:
                case Keys.Down:
                    // Redirect navigation to search box
                    _searchBox.Focus();
                    // Let the key event propagate to search box
                    e.Handled = false;
                    break;

                default:
                    // For any other key (typing), redirect focus to search box
                    // and let the key be processed there
                    if (char.IsLetterOrDigit((char)e.KeyCode) || e.KeyCode == Keys.Space)
                    {
                        _searchBox.Focus();
                        e.Handled = false;
                    }
                    break;
            }
        }

        private void ResultsList_DoubleClick(object? sender, EventArgs e)
        {
            if (!TrySelectSuggestion())
            {
                HandleEnter();
            }
        }

        private bool TrySelectSuggestion()
        {
            // Suggestion handling is now done within FillPlaceholderNode
            return false;
        }

        /// <summary>
        /// Ensures the currently selected item in the results list is visible by scrolling if necessary.
        /// </summary>
        private void EnsureSelectedItemVisible()
        {
            if (_resultsList.SelectedIndex >= 0 && _resultsList.SelectedIndex < _resultsList.Items.Count)
            {
                _resultsList.TopIndex = _resultsList.SelectedIndex;
            }
        }

        private async void HandleEnter()
        {
            Log.Debug($"[CommandPalette] HandleEnter - CurrentNode: {_currentNode?.Id ?? "null"}, SelectedItem: {_resultsList.SelectedItem != null}");
            
            // Always use workflow engine
            if (_currentNode != null && _workflowContext != null)
            {
                // Get user input
                _workflowContext.Set("userInput", _searchBox.Text);
                
                // Handle different UI types
                if (_currentNode is INodeUIProvider uiProvider)
                {
                    if (uiProvider.UIType == NodeUIType.TextInput)
                    {
                        // For TextInput nodes, set the entered text as selectedItem so advancement works
                        string enteredText = _searchBox.Text?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(enteredText))
                        {
                            Log.Debug($"[CommandPalette] TextInput node - setting entered text as selectedItem: {enteredText.Substring(0, Math.Min(50, enteredText.Length))}");
                            _workflowContext.Set("selectedItem", enteredText);
                        }
                    }
                    else if (_resultsList.SelectedItem != null)
                    {
                        // For ItemList nodes, use the selected item
                        Log.Debug($"[CommandPalette] Setting selectedItem in context: {_resultsList.SelectedItem.GetType().Name}");
                        _workflowContext.Set("selectedItem", _resultsList.SelectedItem);
                    }
                }

                // Execute current node
                Log.Debug("[CommandPalette] Calling ExecuteCurrentNodeAsync...");
                await ExecuteCurrentNodeAsync();
                Log.Debug("[CommandPalette] ExecuteCurrentNodeAsync completed");
            }
            else
            {
                Log.Warning($"[CommandPalette] HandleEnter called but CurrentNode or Context is null - Node:{_currentNode == null}, Context:{_workflowContext == null}");
            }
        }

        private void HandleEscape()
        {
            // Always use workflow engine
            if (_workflowEngine != null)
            {
                var previousFrame = _workflowEngine.NavigateBack();
                if (previousFrame != null && _currentWorkflow != null && _workflowRegistry != null)
                {
                    // Restore previous node
                    var nodeDef = _currentWorkflow.GetNodeById(previousFrame.NodeId);
                    if (nodeDef != null)
                    {
                        _currentNode = _workflowRegistry.CreateNode(nodeDef.NodeType, nodeDef.Configuration);
                        if (_currentNode != null)
                        {
                            // Set the node's ID to match the workflow definition's ID
                            if (_currentNode is PromptArqApp.Workflow.Nodes.WorkflowNodeBase nodeBase)
                            {
                                nodeBase.Id = nodeDef.Id;
                                
                                // If this is a ConditionalNode and workflow has branches for it, configure them
                                if (_currentNode is PromptArqApp.Workflow.Nodes.Utility.ConditionalNode conditionalNode && 
                                    _currentWorkflow.Branches != null && 
                                    _currentWorkflow.Branches.TryGetValue(nodeDef.Id, out var branches))
                                {
                                    var config = new Dictionary<string, object>(nodeDef.Configuration)
                                    {
                                        ["branches"] = branches
                                    };
                                    conditionalNode.Configure(config);
                                }
                            }
                        }
                        _workflowContext = previousFrame.Context;
                        RenderNodeUI();
                    }
                }
                else
                {
                    // No more navigation history - close the palette
                    _isShowingTextDisplay = false;
                    _textDisplayPanel?.Hide();
                    TopMost = false;
                    Hide();
                }
            }
        }

        // Legacy HandleSelection method removed - workflow engine handles all selections via HandleEnter()


        // All legacy workflow methods removed (ShowActionsForPrompt, StartFillPlaceholdersWorkflow, 
        // AskForNextPlaceholder, FillPlaceholdersInContent, ShowOutputOptionsScreen, ExecuteAction,
        // GoBackToPrompts, GoBackToActions) - functionality now handled by workflow nodes

        private void ResultsList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var item = _resultsList.Items[e.Index];
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Get colors from current theme
            var theme = ThemeManager.Instance.CurrentTheme;
            var bgColor = isSelected
                ? ThemeApplicator.ParseColor(theme.Controls.ListBox.SelectedBackground)
                : ThemeApplicator.ParseColor(theme.Controls.ListBox.Background);

            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Use workflow node's display methods if available
            if (_currentNode is INodeUIProvider uiProvider)
            {
                DrawNodeItem(e.Graphics, e.Bounds, item, isSelected, uiProvider);
                return;
            }

            // Default drawing for standard types
            if (item is string text)
            {
                DrawPlaceholderPrompt(e.Graphics, e.Bounds, text, isSelected);
            }
            else if (item is PromptAction action)
            {
                DrawAction(e.Graphics, e.Bounds, action, isSelected);
            }
            else if (item is SystemPromptInfo systemPrompt)
            {
                DrawSystemPrompt(e.Graphics, e.Bounds, systemPrompt, isSelected);
            }
            else if (item is PromptInfo prompt)
            {
                DrawPrompt(e.Graphics, e.Bounds, prompt, isSelected);
            }
        }

        private void DrawNodeItem(Graphics g, Rectangle bounds, object item, bool isSelected, INodeUIProvider uiProvider)
        {
            // Check if node implements INodeItemRenderer for advanced rendering
            if (uiProvider is INodeItemRenderer itemRenderer)
            {
                // Try custom rendering first
                if (itemRenderer.CustomRenderItem(g, bounds, item, isSelected))
                {
                    return; // Custom rendering handled it
                }

                // Use template-based rendering
                var renderData = itemRenderer.GetItemRenderData(item);
                var theme = ThemeManager.Instance.CurrentTheme;
                var renderer = new PromptArqApp.Workflow.UI.WorkflowItemRenderer(theme);
                renderer.RenderItem(g, bounds, renderData, isSelected);
                return;
            }

            // Fallback to legacy drawing for nodes that don't implement INodeItemRenderer
            var theme2 = ThemeManager.Instance.CurrentTheme;
            var textColor = isSelected
                ? ThemeApplicator.ParseColor(theme2.Controls.ListBox.SelectedForeground)
                : ThemeApplicator.ParseColor(theme2.Controls.ListBox.Foreground);
            var subTextColor = ThemeApplicator.ParseColor(theme2.Colors.SecondaryForeground);

            // Get display info from node
            var displayText = uiProvider.GetDisplayText(item);
            var secondaryText = uiProvider.GetSecondaryText(item);

            // Draw based on item type for consistency
            if (item is PromptInfo prompt)
            {
                DrawPrompt(g, bounds, prompt, isSelected);
            }
            else if (item is PromptAction action)
            {
                DrawAction(g, bounds, action, isSelected);
            }
            else if (item is string text)
            {
                DrawPlaceholderPrompt(g, bounds, text, isSelected);
            }
            else
            {
                // Generic drawing for other types
                using (var titleFont = new Font(theme2.Fonts.Default.Family, 11F, FontStyle.Bold))
                using (var brush = new SolidBrush(textColor))
                using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter })
                {
                    var titleRect = new Rectangle(bounds.X + 15, bounds.Y + 8, bounds.Width - 30, 20);
                    g.DrawString(displayText, titleFont, brush, titleRect, sf);
                }

                if (!string.IsNullOrEmpty(secondaryText))
                {
                    using (var descFont = theme2.Fonts.Default.ToFont())
                    using (var brush = new SolidBrush(subTextColor))
                    using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter })
                    {
                        var descRect = new Rectangle(bounds.X + 15, bounds.Y + 30, bounds.Width - 30, 15);
                        g.DrawString(secondaryText, descFont, brush, descRect, sf);
                    }
                }
            }
        }

        private void DrawPlaceholderPrompt(Graphics g, Rectangle bounds, string text, bool isSelected)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var textColor = ThemeApplicator.ParseColor(theme.Controls.ListBox.Foreground);
            using (var font = theme.Fonts.Default.ToFont())
            using (var brush = new SolidBrush(textColor))
            {
                var textRect = new Rectangle(bounds.X + 15, bounds.Y + 15, bounds.Width - 30, 20);
                g.DrawString(text, font, brush, textRect);
            }
        }

        private void DrawPrompt(Graphics g, Rectangle bounds, PromptInfo prompt, bool isSelected)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var textColor = isSelected
                ? ThemeApplicator.ParseColor(theme.Controls.ListBox.SelectedForeground)
                : ThemeApplicator.ParseColor(theme.Controls.ListBox.Foreground);
            var subTextColor = ThemeApplicator.ParseColor(theme.Colors.SecondaryForeground);
            var isRecentlyUsed = _recentPromptIds.Contains(prompt.Id);

            // Icon/Badge area
            var iconRect = new Rectangle(bounds.X + 10, bounds.Y + 15, 40, 20);
            var projectColor = isRecentlyUsed ? Color.FromArgb(180, 120, 50) : Color.FromArgb(100, 150, 200);
            using (var brush = new SolidBrush(projectColor))
            {
                g.FillRectangle(brush, iconRect);
            }
            using (var badgeFont = new Font(theme.Fonts.Default.Family, 8F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                var projectText = string.IsNullOrEmpty(prompt.ProjectName) ? "?" : prompt.ProjectName.Substring(0, Math.Min(3, prompt.ProjectName.Length)).ToUpper();
                g.DrawString(projectText, badgeFont, brush, iconRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // Recently used indicator
            if (isRecentlyUsed)
            {
                using (var starFont = new Font(theme.Fonts.Default.Family, 10F))
                using (var brush = new SolidBrush(Color.FromArgb(255, 200, 100)))
                {
                    var starRect = new Rectangle(bounds.X + bounds.Width - 30, bounds.Y + 8, 20, 20);
                    g.DrawString("⭐", starFont, brush, starRect);
                }
            }

            // Title
            using (var titleFont = new Font(theme.Fonts.Default.Family, 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var titleRect = new Rectangle(bounds.X + 60, bounds.Y + 8, bounds.Width - 100, 20);
                g.DrawString(prompt.Title, titleFont, brush, titleRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }

            // Description
            if (!string.IsNullOrEmpty(prompt.Description))
            {
                using (var descFont = new Font(theme.Fonts.Default.Family, 9F, FontStyle.Regular))
                using (var brush = new SolidBrush(subTextColor))
                {
                    var descRect = new Rectangle(bounds.X + 60, bounds.Y + 28, bounds.Width - 70, 18);
                    g.DrawString(prompt.Description, descFont, brush, descRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                }
            }
        }

        private void DrawAction(Graphics g, Rectangle bounds, PromptAction action, bool isSelected)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var textColor = isSelected ? Color.White : Color.LightGray;
            var subTextColor = isSelected ? Color.LightGray : Color.Gray;

            // Icon
            using (var iconFont = new Font(theme.Fonts.SearchBox.Family, 16F))
            using (var brush = new SolidBrush(textColor))
            {
                var iconRect = new Rectangle(bounds.X + 15, bounds.Y + 12, 30, 30);
                g.DrawString(action.Icon, iconFont, brush, iconRect);
            }

            // Name
            using (var nameFont = new Font(theme.Fonts.Default.Family, 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var nameRect = new Rectangle(bounds.X + 60, bounds.Y + 10, bounds.Width - 70, 20);
                g.DrawString(action.Name, nameFont, brush, nameRect);
            }

            // Description
            using (var descFont = new Font(theme.Fonts.Default.Family, 9F, FontStyle.Regular))
            using (var brush = new SolidBrush(subTextColor))
            {
                var descRect = new Rectangle(bounds.X + 60, bounds.Y + 30, bounds.Width - 70, 18);
                g.DrawString(action.Description, descFont, brush, descRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }
        }

        private const int SystemPromptContentPreviewLength = 100;

        private void DrawSystemPrompt(Graphics g, Rectangle bounds, SystemPromptInfo systemPrompt, bool isSelected)
        {
            var theme = ThemeManager.Instance.CurrentTheme;
            var textColor = isSelected ? Color.White : Color.LightGray;
            var subTextColor = isSelected ? Color.LightGray : Color.Gray;

            // Icon/Badge area
            var iconRect = new Rectangle(bounds.X + 10, bounds.Y + 15, 40, 20);
            var badgeColor = Color.FromArgb(150, 100, 200); // Purple for system prompts
            using (var brush = new SolidBrush(badgeColor))
            {
                g.FillRectangle(brush, iconRect);
            }
            using (var badgeFont = new Font(theme.Fonts.Default.Family, 8F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString("SYS", badgeFont, brush, iconRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }

            // Name
            using (var nameFont = new Font(theme.Fonts.Default.Family, 11F, FontStyle.Bold))
            using (var brush = new SolidBrush(textColor))
            {
                var nameRect = new Rectangle(bounds.X + 60, bounds.Y + 8, bounds.Width - 70, 20);
                g.DrawString(systemPrompt.Name, nameFont, brush, nameRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
            }

            // Content preview
            var contentPreview = systemPrompt.Content.Length > SystemPromptContentPreviewLength
                ? systemPrompt.Content.Substring(0, SystemPromptContentPreviewLength) + "..."
                : systemPrompt.Content;
            using (var contentFont = new Font(theme.Fonts.Default.Family, 9F, FontStyle.Regular))
            using (var brush = new SolidBrush(subTextColor))
            {
                var contentRect = new Rectangle(bounds.X + 60, bounds.Y + 28, bounds.Width - 70, 18);
                g.DrawString(contentPreview, contentFont, brush, contentRect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
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

        /// <summary>
        /// Override Hide to ensure TextDisplayPanel is always closed when CommandPalette hides.
        /// </summary>
        public new void Hide()
        {
            _isShowingTextDisplay = false;
            _textDisplayPanel?.Hide();
            base.Hide();
        }

        // All One-Time Prompt legacy methods removed (StartOneTimePromptWorkflow, ShowSystemPrompts,
        // AskForUserPrompt, ExecuteOneTimePrompt, GoBackToSystemPromptSelection, GenerateAndShowPrompt,
        // ShowExecutionResult, ShowGeneratedPromptActions, HandleGeneratedPromptAction)
        // - functionality now handled by OneTimePrompt workflow nodes

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
