> # Architecture & Project Structure Proposal
> 
> Based on codebase analysis, workflow engine research, and .NET best practices, here's a comprehensive architecture proposal for the extensible command palette system.
> 
> ---
> 
> ## 1. Technology Stack & Libraries
> 
> ### Core Dependencies
> 
> ```xml
> <!-- Existing packages to keep -->
> <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2792.45" />
> <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
> <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
> <PackageReference Include="System.Management" Version="8.0.0" />
> <PackageReference Include="Serilog" Version="3.1.1" />
> <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
> 
> <!-- NEW: Dependency Injection & IoC -->
> <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
> <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
> <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
> <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
> 
> <!-- NEW: Lightweight workflow (optional - only if needed for complex flows) -->
> <!-- <PackageReference Include="WorkflowCore" Version="3.17.0" /> -->
> <!-- Decision: Build our own simpler engine to avoid overhead -->
> 
> <!-- NEW: Reactive Extensions for event handling -->
> <PackageReference Include="System.Reactive" Version="6.0.0" />
> 
> <!-- NEW: Global hotkey library (better than current implementation) -->
> <PackageReference Include="GlobalHotKeys" Version="1.2.0" />
> 
> <!-- NEW: Expression evaluation for dynamic conditions -->
> <PackageReference Include="DynamicExpresso.Core" Version="2.16.1" />
> 
> <!-- NEW: Better JSON handling -->
> <PackageReference Include="System.Text.Json" Version="8.0.0" />
> 
> <!-- NEW: Plugin loading -->
> <PackageReference Include="System.Composition" Version="8.0.0" />
> <PackageReference Include="System.Composition.AttributedModel" Version="8.0.0" />
> <PackageReference Include="System.Composition.Hosting" Version="8.0.0" />
> ```
> 
> ### Why These Choices?
> 
> 1. **Microsoft.Extensions.DependencyInjection** - Standard .NET DI container, sufficient for our needs
>    - No need for Autofac unless we need advanced features
>    - Native integration with .NET ecosystem
>    - Lightweight and performant
> 
> 2. **System.Reactive (Rx.NET)** - For event-driven workflows
>    - Observable sequences for node execution
>    - Built-in operators for composition
>    - Better than custom event handling
> 
> 3. **DynamicExpresso** - For conditional nodes
>    - Evaluate expressions like `"hasPlaceholders == true"`
>    - Safer than `eval()` or script engines
>    - Good performance
> 
> 4. **System.Composition (MEF2)** - For plugin discovery
>    - Built-in to .NET
>    - Attribute-based discovery
>    - Lightweight compared to full MEF
> 
> 5. **Custom workflow engine** - Rather than WorkflowCore
>    - WorkflowCore is designed for long-running server workflows
>    - Our workflows are short-lived UI interactions
>    - Custom engine = simpler, faster, fewer dependencies
> 
> ---
> 
> ## 2. Project Structure
> 
> ```
> WindowsApp/
> ├── Program.cs                          # Application entry point
> ├── PromptArqApp.csproj
> ├── AGENTS.md
> │
> ├── Core/                               # 🆕 Core abstractions & interfaces
> │   ├── Workflows/
> │   │   ├── IWorkflowNode.cs
> │   │   ├── IWorkflowNodeProvider.cs
> │   │   ├── WorkflowContext.cs
> │   │   ├── WorkflowResult.cs
> │   │   ├── Workflow.cs
> │   │   ├── WorkflowNodeDefinition.cs
> │   │   └── WorkflowMetadata.cs
> │   ├── Capabilities/
> │   │   ├── ICapabilityProvider.cs
> │   │   ├── CapabilityCategory.cs
> │   │   └── CapabilityInfo.cs
> │   ├── Actions/
> │   │   ├── IUniversalAction.cs
> │   │   ├── ContentType.cs
> │   │   └── ActionContext.cs
> │   ├── Services/
> │   │   ├── IClipboardService.cs
> │   │   ├── ISnippetService.cs
> │   │   ├── IHotkeyService.cs
> │   │   ├── INotificationService.cs
> │   │   ├── IPromptService.cs
> │   │   └── ISystemService.cs
> │   └── Extensions/
> │       ├── ServiceCollectionExtensions.cs
> │       └── WorkflowBuilderExtensions.cs
> │
> ├── Engine/                             # 🆕 Workflow execution engine
> │   ├── WorkflowEngine.cs               # Main orchestration
> │   ├── WorkflowExecutionContext.cs     # Runtime state
> │   ├── NavigationStack.cs              # Back navigation
> │   ├── NodeExecutor.cs                 # Executes individual nodes
> │   ├── NodeUIProvider.cs               # UI abstraction
> │   ├── ExpressionEvaluator.cs          # For conditional nodes
> │   └── Events/
> │       ├── NodeExecutedEventArgs.cs
> │       ├── NodeErrorEventArgs.cs
> │       └── WorkflowCompletedEventArgs.cs
> │
> ├── Registry/                           # 🆕 Discovery & registration
> │   ├── WorkflowRegistry.cs             # Workflow management
> │   ├── CapabilityRegistry.cs           # Capability discovery
> │   ├── ActionRegistry.cs               # Universal actions
> │   ├── NodeFactory.cs                  # Node instantiation
> │   ├── PluginLoader.cs                 # Plugin assembly loading
> │   └── WorkflowValidator.cs            # Validate workflow definitions
> │
> ├── Nodes/                              # 🆕 Built-in workflow nodes
> │   ├── Input/
> │   │   ├── SearchPromptsNode.cs
> │   │   ├── TextInputNode.cs
> │   │   ├── FillPlaceholderNode.cs
> │   │   ├── SelectionNode.cs
> │   │   ├── FileSearchNode.cs
> │   │   └── ListFilterNode.cs
> │   ├── Action/
> │   │   ├── ExecuteLLMNode.cs
> │   │   ├── CopyToClipboardNode.cs
> │   │   ├── PasteNode.cs
> │   │   ├── OpenInEditorNode.cs
> │   │   ├── HttpRequestNode.cs
> │   │   └── RunScriptNode.cs
> │   ├── UI/
> │   │   ├── ShowActionsNode.cs
> │   │   ├── ShowTextPanelNode.cs
> │   │   ├── ShowNotificationNode.cs
> │   │   └── ShowConfirmationNode.cs
> │   ├── Utility/
> │   │   ├── ConditionalNode.cs
> │   │   ├── FilterNode.cs
> │   │   ├── TransformNode.cs
> │   │   ├── LoopNode.cs
> │   │   ├── DelayNode.cs
> │   │   ├── ScriptFilterNode.cs         # Alfred's most powerful feature
> │   │   └── AggregateNode.cs
> │   └── Output/
> │       ├── CloseCommandPaletteNode.cs
> │       ├── RecordHistoryNode.cs
> │       ├── LogNode.cs
> │       └── ChainWorkflowNode.cs
> │
> ├── Capabilities/                       # 🆕 Built-in capability providers
> │   ├── PromptCapabilities.cs
> │   ├── ClipboardCapabilities.cs
> │   ├── SystemCapabilities.cs
> │   ├── WindowManagementCapabilities.cs
> │   ├── FileOperationCapabilities.cs
> │   ├── CalculatorCapability.cs
> │   └── SnippetCapabilities.cs
> │
> ├── Actions/                            # 🆕 Universal actions
> │   ├── Clipboard/
> │   │   ├── CopyAction.cs
> │   │   └── PasteAction.cs
> │   ├── Web/
> │   │   ├── OpenUrlAction.cs
> │   │   └── SearchWebAction.cs
> │   ├── File/
> │   │   ├── OpenFileAction.cs
> │   │   ├── RevealInExplorerAction.cs
> │   │   ├── CopyPathAction.cs
> │   │   └── CompressAction.cs
> │   ├── Text/
> │   │   ├── ExtractUrlsAction.cs
> │   │   ├── ToUpperCaseAction.cs
> │   │   └── ToLowerCaseAction.cs
> │   └── Email/
> │       └── SendToAction.cs
> │
> ├── Services/                           # 🆕 Service implementations
> │   ├── ClipboardService.cs             # Clipboard history & management
> │   ├── SnippetService.cs               # Text snippet expansion
> │   ├── HotkeyService.cs                # Global hotkey management
> │   ├── NotificationService.cs          # Toast notifications (existing)
> │   ├── PromptService.cs                # Prompt management (new wrapper)
> │   ├── SystemCommandService.cs         # Windows system commands
> │   ├── WindowService.cs                # Window management
> │   ├── ProcessService.cs               # Process management
> │   └── ScriptExecutionService.cs       # Execute PowerShell/scripts
> │
> ├── Plugins/                            # 🆕 Plugin infrastructure
> │   ├── IWorkflowPlugin.cs
> │   ├── PluginMetadata.cs
> │   ├── BuiltInWorkflowsPlugin.cs       # Ships with app
> │   └── ThirdParty/                     # Drop folder for .dll files
> │
> ├── UI/                                 # 🆕 Reorganized UI components
> │   ├── CommandPalette/
> │   │   ├── CommandPaletteForm.cs       # Refactored generic shell
> │   │   ├── NodeRenderer.cs             # Renders node UI
> │   │   ├── CategoryHeader.cs           # UI component
> │   │   └── SearchBox.cs                # Enhanced search box
> │   ├── Panels/
> │   │   ├── TextDisplayPanel.cs         # Existing
> │   │   └── WorkflowPreviewPanel.cs     # For debugging workflows
> │   ├── Dialogs/
> │   │   ├── SettingsForm.cs             # Existing
> │   │   └── WorkflowEditorDialog.cs     # Future: visual editor
> │   └── Components/
> │       ├── BorderlessFormBase.cs       # Existing
> │       └── ThemeableControl.cs         # Base for themed controls
> │
> ├── Data/                               # Data access & persistence
> │   ├── Models/
> │   │   ├── ClipboardEntry.cs
> │   │   ├── Snippet.cs
> │   │   ├── WorkflowExecution.cs
> │   │   └── HotkeyConfig.cs            # Existing
> │   ├── Repositories/
> │   │   ├── IClipboardRepository.cs
> │   │   ├── ISnippetRepository.cs
> │   │   ├── IWorkflowRepository.cs
> │   │   └── Implementations/
> │   │       ├── SqliteClipboardRepository.cs
> │   │       ├── SqliteSnippetRepository.cs
> │   │       └── JsonWorkflowRepository.cs
> │   └── Migrations/
> │       └── InitialSchema.sql
> │
> ├── Configuration/                      # Configuration management
> │   ├── AppSettings.cs                  # Existing
> │   ├── WorkflowSettings.cs
> │   ├── ClipboardSettings.cs
> │   └── SnippetSettings.cs
> │
> ├── Utilities/                          # Helper classes
> │   ├── JsonHelper.cs
> │   ├── ProcessHelper.cs
> │   ├── WindowsApiHelper.cs
> │   └── ContentTypeDetector.cs          # For universal actions
> │
> ├── Theming/                            # Existing theming system
> │   ├── Theme.cs
> │   ├── ThemeManager.cs
> │   ├── ThemeApplicator.cs
> │   └── ThemeLoader.cs
> │
> ├── Legacy/                             # 🆕 Temporary during migration
> │   ├── PromptAction.cs                 # Old enum-based system
> │   └── PromptHistory.cs                # To be replaced
> │
> └── Workflows/                          # 🆕 Workflow definitions (JSON)
>     ├── BuiltIn/
>     │   ├── fill-placeholders.workflow.json
>     │   ├── one-time-prompt.workflow.json
>     │   ├── quick-paste.workflow.json
>     │   ├── clipboard-history.workflow.json
>     │   ├── calculator.workflow.json
>     │   └── system-commands.workflow.json
>     └── Custom/                         # User-created workflows
>         └── .gitkeep
> ```
> 
> ---
> 
> ## 3. Architecture Patterns
> 
> ### A. Dependency Injection Pattern
> 
> **Why:** Decoupling, testability, lifecycle management
> 
> ```csharp
> // Program.cs - Application bootstrapping
> public class Program
> {
>     [STAThread]
>     static void Main()
>     {
>         var host = CreateHostBuilder().Build();
>         
>         // Run the application with DI container
>         Application.Run(host.Services.GetRequiredService<MainForm>());
>     }
>     
>     static IHostBuilder CreateHostBuilder() =>
>         Host.CreateDefaultBuilder()
>             .ConfigureServices((context, services) =>
>             {
>                 // Core services
>                 services.AddSingleton<IWorkflowEngine, WorkflowEngine>();
>                 services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();
>                 services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();
>                 services.AddSingleton<IActionRegistry, ActionRegistry>();
>                 
>                 // Business services
>                 services.AddSingleton<IClipboardService, ClipboardService>();
>                 services.AddSingleton<ISnippetService, SnippetService>();
>                 services.AddSingleton<IHotkeyService, HotkeyService>();
>                 services.AddSingleton<IPromptService, PromptService>();
>                 services.AddSingleton<ISystemService, SystemCommandService>();
>                 
>                 // Repositories
>                 services.AddSingleton<IClipboardRepository, SqliteClipboardRepository>();
>                 services.AddSingleton<ISnippetRepository, SqliteSnippetRepository>();
>                 services.AddSingleton<IWorkflowRepository, JsonWorkflowRepository>();
>                 
>                 // UI
>                 services.AddTransient<MainForm>();
>                 services.AddTransient<CommandPaletteForm>();
>                 services.AddSingleton<ThemeManager>();
>                 
>                 // Plugin loading
>                 services.AddSingleton<PluginLoader>();
>                 services.LoadPlugins(); // Extension method
>             })
>             .UseSerilog();
> }
> ```
> 
> ### B. Strategy Pattern (for Nodes)
> 
> **Why:** Each node type encapsulates its own behavior
> 
> ```csharp
> public interface IWorkflowNode
> {
>     string Id { get; }
>     string Name { get; }
>     Task<WorkflowResult> ExecuteAsync(WorkflowContext context);
> }
> 
> // Each node is a strategy
> public class CopyToClipboardNode : IWorkflowNode
> {
>     private readonly IClipboardService _clipboard;
>     
>     public CopyToClipboardNode(IClipboardService clipboard)
>     {
>         _clipboard = clipboard;
>     }
>     
>     public async Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
>     {
>         var text = context.Get<string>("text");
>         await _clipboard.SetTextAsync(text);
>         return WorkflowResult.Success(context);
>     }
> }
> ```
> 
> ### C. Chain of Responsibility (for Workflow Execution)
> 
> **Why:** Nodes can pass control to next node in chain
> 
> ```csharp
> public class WorkflowEngine
> {
>     public async Task<WorkflowResult> ExecuteWorkflowAsync(
>         string workflowId, 
>         WorkflowContext context)
>     {
>         var workflow = _registry.GetWorkflow(workflowId);
>         var currentNode = workflow.GetEntryNode();
>         
>         while (currentNode != null)
>         {
>             var result = await ExecuteNodeAsync(currentNode, context);
>             
>             if (!result.Success)
>                 return result;
>                 
>             currentNode = GetNextNode(workflow, result);
>         }
>         
>         return WorkflowResult.Success(context);
>     }
> }
> ```
> 
> ### D. Observer Pattern (for Events)
> 
> **Why:** Decouple workflow execution from UI updates
> 
> ```csharp
> public class WorkflowEngine
> {
>     public event EventHandler<NodeExecutedEventArgs>? NodeExecuted;
>     public event EventHandler<NodeErrorEventArgs>? NodeError;
>     
>     private async Task<WorkflowResult> ExecuteNodeAsync(
>         IWorkflowNode node, 
>         WorkflowContext context)
>     {
>         try
>         {
>             var result = await node.ExecuteAsync(context);
>             NodeExecuted?.Invoke(this, new NodeExecutedEventArgs(node, result));
>             return result;
>         }
>         catch (Exception ex)
>         {
>             NodeError?.Invoke(this, new NodeErrorEventArgs(node, ex));
>             throw;
>         }
>     }
> }
> 
> // CommandPaletteForm subscribes
> public class CommandPaletteForm
> {
>     public CommandPaletteForm(IWorkflowEngine engine)
>     {
>         engine.NodeExecuted += OnNodeExecuted;
>     }
>     
>     private void OnNodeExecuted(object? sender, NodeExecutedEventArgs e)
>     {
>         // Update UI based on node execution
>         RenderNode(e.Node);
>     }
> }
> ```
> 
> ### E. Factory Pattern (for Node Creation)
> 
> **Why:** Centralized node instantiation with DI
> 
> ```csharp
> public class NodeFactory
> {
>     private readonly IServiceProvider _services;
>     private readonly Dictionary<string, Type> _nodeTypes;
>     
>     public IWorkflowNode CreateNode(string nodeType, Dictionary<string, object>? config)
>     {
>         if (!_nodeTypes.TryGetValue(nodeType, out var type))
>             throw new InvalidOperationException($"Unknown node type: {nodeType}");
>         
>         // Create instance with DI
>         var node = (IWorkflowNode)ActivatorUtilities.CreateInstance(_services, type);
>         
>         // Apply configuration
>         if (config != null && node is IConfigurableNode configurable)
>         {
>             configurable.Configure(config);
>         }
>         
>         return node;
>     }
> }
> ```
> 
> ### F. Composite Pattern (for Complex Workflows)
> 
> **Why:** Workflows can contain sub-workflows
> 
> ```csharp
> public class SubWorkflowNode : IWorkflowNode
> {
>     private readonly IWorkflowEngine _engine;
>     private readonly string _subWorkflowId;
>     
>     public async Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
>     {
>         // Execute sub-workflow
>         return await _engine.ExecuteWorkflowAsync(_subWorkflowId, context);
>     }
> }
> ```
> 
> ### G. Repository Pattern (for Data Access)
> 
> **Why:** Abstract persistence layer
> 
> ```csharp
> public interface IClipboardRepository
> {
>     Task<IEnumerable<ClipboardEntry>> GetRecentAsync(int count);
>     Task<IEnumerable<ClipboardEntry>> SearchAsync(string query);
>     Task AddAsync(ClipboardEntry entry);
>     Task ClearAsync();
> }
> 
> public class SqliteClipboardRepository : IClipboardRepository
> {
>     private readonly string _connectionString;
>     
>     public async Task<IEnumerable<ClipboardEntry>> GetRecentAsync(int count)
>     {
>         using var connection = new SqliteConnection(_connectionString);
>         // Implementation
>     }
> }
> ```
> 
> ---
> 
> ## 4. Key Design Decisions
> 
> ### ✅ Build Custom Workflow Engine (Not WorkflowCore)
> 
> **Rationale:**
> - WorkflowCore is for long-running server workflows with persistence
> - Our workflows are short-lived UI interactions (seconds, not hours/days)
> - Custom engine = simpler, faster, fewer dependencies
> - Better control over UI integration
> 
> ### ✅ Use Microsoft.Extensions.DependencyInjection (Not Autofac)
> 
> **Rationale:**
> - Sufficient for our needs
> - Native .NET integration
> - Lighter weight
> - One less dependency
> 
> ### ✅ MEF2 for Plugin Discovery (System.Composition)
> 
> **Rationale:**
> - Built-in to .NET
> - Attribute-based discovery
> - Good balance of features vs complexity
> - Can load plugins from assemblies at runtime
> 
> ### ✅ JSON for Workflow Definitions
> 
> **Rationale:**
> - Human-readable and editable
> - Easy to serialize/deserialize
> - Can be stored in files or database
> - Enables hot-reload
> 
> **Example workflow file:**
> ```json
> {
>   "id": "fill-placeholders",
>   "name": "Fill Placeholders",
>   "version": "1.0.0",
>   "author": "PromptArq",
>   "description": "Fill template variables and execute",
>   "icon": "📝",
>   "metadata": {
>     "tags": ["prompts", "templates"],
>     "category": "Prompts"
>   },
>   "nodes": [
>     {
>       "id": "search",
>       "type": "SearchPromptsNode",
>       "config": {
>         "filter": "hasPlaceholders"
>       }
>     },
>     {
>       "id": "show-actions",
>       "type": "ShowActionsNode"
>     },
>     {
>       "id": "fill-loop",
>       "type": "LoopNode",
>       "config": {
>         "itemsKey": "placeholders",
>         "bodyNodeId": "fill-input",
>         "exitNodeId": "show-output"
>       }
>     },
>     {
>       "id": "fill-input",
>       "type": "FillPlaceholderNode"
>     },
>     {
>       "id": "show-output",
>       "type": "ShowActionsNode"
>     },
>     {
>       "id": "execute",
>       "type": "ExecuteLLMNode"
>     },
>     {
>       "id": "copy",
>       "type": "CopyToClipboardNode"
>     }
>   ],
>   "connections": {
>     "search": "show-actions",
>     "show-actions": "fill-loop",
>     "fill-loop": "fill-input",
>     "fill-input": "fill-loop",
>     "show-output": "execute",
>     "execute": "copy"
>   },
>   "entryNodeId": "search"
> }
> ```
> 
> ### ✅ Reactive Extensions for Event Handling
> 
> **Rationale:**
> - Better than custom event aggregator
> - Built-in operators for composition
> - Good for async workflows
> - Industry standard
> 
> ---
> 
> ## 5. Migration Strategy
> 
> ### Phase 1: Infrastructure Setup (Week 1-2)
> ```
> ✓ Set up new project structure
> ✓ Install NuGet packages
> ✓ Implement DI container and service registration
> ✓ Create core interfaces (IWorkflowNode, ICapabilityProvider, etc.)
> ✓ Build WorkflowEngine skeleton
> ✓ Build WorkflowRegistry
> ```
> 
> ### Phase 2: Core Nodes (Week 3-4)
> ```
> ✓ Implement 10 essential nodes:
>   - SearchPromptsNode
>   - TextInputNode
>   - ShowActionsNode
>   - CopyToClipboardNode
>   - PasteNode
>   - ConditionalNode
>   - LoopNode
>   - ShowTextPanelNode
>   - ExecuteLLMNode
>   - CloseCommandPaletteNode
> ✓ Write unit tests for each node
> ```
> 
> ### Phase 3: Refactor CommandPaletteForm (Week 5-6)
> ```
> ✓ Refactor to use WorkflowEngine
> ✓ Implement generic NodeRenderer
> ✓ Remove hardcoded WorkflowState enum
> ✓ Test with simple workflow
> ```
> 
> ### Phase 4: Convert Existing Workflows (Week 7-8)
> ```
> ✓ Convert "Fill Placeholders" to JSON workflow
> ✓ Convert "One Time Prompt" to JSON workflow
> ✓ Create BuiltInWorkflowsPlugin
> ✓ Test all existing functionality
> ```
> 
> ### Phase 5: New Features (Week 9-12)
> ```
> ✓ Implement ClipboardService & history
> ✓ Implement SnippetService & expansion
> ✓ Implement CapabilityRegistry
> ✓ Build 5 Windows system command workflows
> ✓ Implement ScriptFilterNode
> ✓ Implement UniversalActions
> ```
> 
> ### Phase 6: Polish & Plugin System (Week 13-14)
> ```
> ✓ Plugin loader
> ✓ Hot reload
> ✓ Documentation
> ✓ Example plugins
> ```
> 
> ---
> 
> ## 6. Testing Strategy
> 
> ```
> WindowsApp.Tests/
> ├── Unit/
> │   ├── Nodes/
> │   │   ├── ConditionalNodeTests.cs
> │   │   ├── LoopNodeTests.cs
> │   │   └── FilterNodeTests.cs
> │   ├── Engine/
> │   │   ├── WorkflowEngineTests.cs
> │   │   ├── NodeExecutorTests.cs
> │   │   └── NavigationStackTests.cs
> │   └── Services/
> │       ├── ClipboardServiceTests.cs
> │       └── SnippetServiceTests.cs
> ├── Integration/
> │   ├── WorkflowExecutionTests.cs
> │   ├── PluginLoadingTests.cs
> │   └── CapabilityRegistryTests.cs
> └── E2E/
>     └── FillPlaceholdersWorkflowTests.cs
> ```
> 
> **Test frameworks:**
> - xUnit (modern, good for .NET)
> - Moq (mocking dependencies)
> - FluentAssertions (better assertions)
> 
> ---
> 
> ## 7. Performance Considerations
> 
> 1. **Lazy loading** - Only load workflows when needed
> 2. **Node pooling** - Reuse node instances where possible
> 3. **Async/await** - All I/O operations are async
> 4. **Caching** - Cache compiled expressions, workflows
> 5. **Debouncing** - Search input debouncing (existing)
> 
> ---
> 
> ## 8. Next Steps
> 
> 1. Create new folder structure
> 2. Install NuGet packages
> 3. Implement core interfaces
> 4. Build minimal workflow engine
> 5. Create one simple test workflow
> 6. Validate approach before full migration
> 
> ---
> 
> This architecture provides:
> - ✅ **Extensibility** - Easy to add workflows and nodes
> - ✅ **Testability** - All components can be unit tested
> - ✅ **Maintainability** - Clear separation of concerns
> - ✅ **Performance** - Async, lightweight, efficient
> - ✅ **Flexibility** - JSON workflows, plugin system
> - ✅ **Windows Integration** - Native Windows features
> 
> **Total estimated effort:** 12-14 weeks for complete implementation 

 _Originally posted by @tamaygz in [#33](https://github.com/tamaygz/promptArq/issues/33#issuecomment-3690135618)_