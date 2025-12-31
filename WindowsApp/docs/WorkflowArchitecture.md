# Workflow System Architecture

## Overview

The workflow system transforms the command palette from a hardcoded state machine into a generic, extensible system inspired by [Alfred](https://www.alfredapp.com/) for macOS. This enables easy addition of new commands and workflows without modifying core code.

## Core Concepts

### Workflow
A **Workflow** is a directed graph of nodes that defines a multi-step process. Each workflow has:
- Unique ID and metadata
- Collection of node definitions
- Connections between nodes (edges in the graph)
- Entry point (first node to execute)

```csharp
var workflow = new Workflow
{
    Id = "fill-placeholders",
    Name = "Fill Placeholders and Execute",
    Description = "Fill template variables and execute prompt",
    Icon = "📝",
    EntryNodeId = "search-prompts",
    Nodes = new List<WorkflowNodeDefinition> { ... },
    Connections = new Dictionary<string, string> { ... }
};
```

### Node
A **Node** is a single step in a workflow. Nodes can:
- Display UI (input, selection, display)
- Perform actions (copy, paste, execute)
- Control flow (conditional, loop, branch)
- Transform data

All nodes implement `IWorkflowNode`:

```csharp
public interface IWorkflowNode
{
    string Id { get; }
    string Name { get; }
    Task<WorkflowResult> ExecuteAsync(WorkflowContext context);
}
```

### Context
**WorkflowContext** is a data bag that flows through the workflow, carrying state between nodes:

```csharp
var context = new WorkflowContext(services);
context.Set("selectedPrompt", prompt);
context.Set("placeholderValues", values);

var prompt = context.Get<PromptInfo>("selectedPrompt");
var values = context.GetOrDefault<Dictionary<string, string>>("placeholderValues", new());
```

### Result
**WorkflowResult** indicates success/failure and controls flow:

```csharp
// Success
return WorkflowResult.CreateSuccess(context);

// Success with next node override
return WorkflowResult.CreateSuccess(context, nextNodeId: "special-node");

// Error
return WorkflowResult.CreateError(context, "Something went wrong");
```

## Architecture Layers

### 1. Core Layer (`Workflow/Core/`)
Defines the fundamental abstractions:

- **IWorkflowNode** - Base node interface
- **INodeUIProvider** - Optional UI rendering interface
- **WorkflowContext** - State container with typed accessors
- **WorkflowResult** - Execution result with success/error handling
- **Workflow** - Workflow definition
- **WorkflowEngine** - Executes workflows and manages navigation
- **WorkflowNavigationStack** - Enables back navigation (ESC key)

### 2. Registry Layer (`Workflow/Registry/`)
Manages workflow and node discovery:

- **IWorkflowRegistry** - Registry interface
- **WorkflowRegistry** - Default implementation
- **IWorkflowPlugin** - Plugin interface for extensibility

### 3. Nodes Layer (`Workflow/Nodes/`)
Reusable workflow steps organized by category:

- **BaseNodes** - Abstract base classes for all node types
  - `InputNodeBase` - Nodes that accept user input
  - `ActionNodeBase` - Nodes that perform operations
  - `UINodeBase` - Nodes that display information
  - `UtilityNodeBase` - Control flow nodes
  - `OutputNodeBase` - Final output nodes

## Node Categories

### Input Nodes
Accept user input and provide filtering/searching.

**Examples:**
- `SearchPromptsNode` - Search and filter prompts
- `TextInputNode` - Single text input
- `FillPlaceholderNode` - Fill template placeholders

### Action Nodes
Perform operations and transformations.

**Examples:**
- `ExecuteLLMNode` - Execute prompt through LLM
- `CopyToClipboardNode` - Copy text to clipboard
- `PasteNode` - Paste text to active window

### UI Nodes
Display information to user.

**Examples:**
- `ShowTextPanelNode` - Display text in TextDisplayPanel
- `ShowActionsNode` - Display action list
- `ShowNotificationNode` - Show toast notification

### Utility Nodes
Control flow and data transformation.

**Examples:**
- `ConditionalNode` - Branch based on conditions
- `LoopNode` - Iterate through items
- `TransformNode` - Transform data

### Output Nodes
Final output and side effects.

**Examples:**
- `CloseCommandPaletteNode` - Close the palette
- `RecordHistoryNode` - Record action in history

## Creating a Custom Node

### Step 1: Choose Base Class

```csharp
public class MyInputNode : InputNodeBase
{
    public override string Name => "My Input Node";
    public override NodeUIType UIType => NodeUIType.TextInput;
    public override string HintText => "Enter your value";

    public MyInputNode(IServiceProvider services) : base(services)
    {
    }

    public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
    {
        // Get input from context (set by UI)
        var input = context.GetOrDefault<string>("userInput", "");
        
        // Process input
        context.Set("processedInput", input.ToUpperInvariant());
        
        return Task.FromResult(WorkflowResult.CreateSuccess(context));
    }
}
```

### Step 2: Register the Node

```csharp
registry.RegisterNode("MyInputNode", typeof(MyInputNode));
```

### Step 3: Use in Workflow

```csharp
var workflow = new Workflow
{
    Id = "my-workflow",
    EntryNodeId = "input",
    Nodes = new List<WorkflowNodeDefinition>
    {
        new() { Id = "input", NodeType = "MyInputNode" }
    }
};
```

## Creating a Workflow

### Example: Simple Copy Workflow

```csharp
var copyWorkflow = new Workflow
{
    Id = "quick-copy",
    Name = "Quick Copy",
    Description = "Copy a prompt to clipboard",
    Icon = "📋",
    EntryNodeId = "search",
    Nodes = new List<WorkflowNodeDefinition>
    {
        new() { Id = "search", NodeType = "SearchPromptsNode" },
        new() { Id = "copy", NodeType = "CopyToClipboardNode" },
        new() { Id = "close", NodeType = "CloseCommandPaletteNode" }
    },
    Connections = new Dictionary<string, string>
    {
        ["search"] = "copy",
        ["copy"] = "close"
    }
};

registry.RegisterWorkflow(copyWorkflow);
```

### Example: Complex Multi-Step Workflow

```csharp
var fillPlaceholdersWorkflow = new Workflow
{
    Id = "fill-placeholders",
    Name = "Fill Placeholders and Execute",
    Description = "Fill template variables and execute prompt",
    Icon = "📝",
    EntryNodeId = "search-prompts",
    Nodes = new List<WorkflowNodeDefinition>
    {
        new() { Id = "search-prompts", NodeType = "SearchPromptsNode" },
        new() { Id = "show-actions", NodeType = "ShowActionsNode" },
        new() { 
            Id = "check-placeholders", 
            NodeType = "ConditionalNode",
            Configuration = new() { ["condition"] = "hasPlaceholders" }
        },
        new() { 
            Id = "fill-loop", 
            NodeType = "LoopNode",
            Configuration = new() { ["itemsKey"] = "placeholders" }
        },
        new() { Id = "fill-placeholder", NodeType = "FillPlaceholderNode" },
        new() { Id = "apply-values", NodeType = "ApplyPlaceholdersNode" },
        new() { Id = "execute-llm", NodeType = "ExecuteLLMNode" },
        new() { Id = "copy", NodeType = "CopyToClipboardNode" },
        new() { Id = "close", NodeType = "CloseCommandPaletteNode" }
    },
    Connections = new Dictionary<string, string>
    {
        ["search-prompts"] = "show-actions",
        ["show-actions"] = "check-placeholders",
        ["check-placeholders"] = "fill-loop",
        ["fill-loop"] = "fill-placeholder",
        ["fill-placeholder"] = "fill-loop",
        ["fill-loop-exit"] = "apply-values",
        ["apply-values"] = "execute-llm",
        ["execute-llm"] = "copy",
        ["copy"] = "close"
    }
};
```

## Creating a Plugin

Plugins package workflows and nodes together:

```csharp
public class CalculatorPlugin : IWorkflowPlugin
{
    public string PluginId => "community.calculator";
    public string Name => "Calculator Workflow";
    public Version Version => new Version(1, 0, 0);
    
    public IEnumerable<Workflow> GetWorkflows()
    {
        yield return new Workflow
        {
            Id = "calculator",
            Name = "Calculator",
            Description = "Perform mathematical calculations",
            Icon = "🔢",
            Nodes = new List<WorkflowNodeDefinition>
            {
                new() { Id = "input", NodeType = "TextInputNode" },
                new() { Id = "calculate", NodeType = "CalculateNode" },
                new() { Id = "show-result", NodeType = "ShowTextPanelNode" },
                new() { Id = "copy", NodeType = "CopyToClipboardNode" }
            },
            Connections = new Dictionary<string, string>
            {
                ["input"] = "calculate",
                ["calculate"] = "show-result",
                ["show-result"] = "copy"
            },
            EntryNodeId = "input"
        };
    }
    
    public IEnumerable<(string, Type)> GetNodes()
    {
        yield return ("CalculateNode", typeof(CalculateNode));
    }
}

// Register the plugin
registry.RegisterPlugin(new CalculatorPlugin());
```

## Dependency Injection

The workflow system uses dependency injection for loose coupling:

```csharp
// Setup (done in Program.cs)
ServiceConfiguration.Configure();

// Get services in nodes
public class MyNode : ActionNodeBase
{
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    
    public MyNode(IServiceProvider services) : base(services)
    {
        _logger = services.GetRequiredService<ILogger>();
        _settings = services.GetRequiredService<AppSettings>();
    }
    
    protected override async Task<WorkflowResult> PerformActionAsync(WorkflowContext context)
    {
        _logger.Information("Executing MyNode");
        // Use services...
        return WorkflowResult.CreateSuccess(context);
    }
}
```

## Navigation and Back Button

The navigation stack automatically tracks node history:

```csharp
// In WorkflowEngine
public NavigationFrame? NavigateBack()
{
    var previousFrame = _navigationStack.Pop();
    if (previousFrame != null)
    {
        _currentContext = previousFrame.Context;
        // Restore previous node...
    }
    return previousFrame;
}

// In CommandPaletteForm
private void HandleEscape()
{
    var previousFrame = _engine.NavigateBack();
    if (previousFrame != null)
    {
        // Restore UI to previous state
        _currentNode = _registry.CreateNode(previousFrame.NodeId);
        _context = previousFrame.Context;
        await ExecuteCurrentNode();
    }
    else
    {
        // At start of workflow, close palette
        Hide();
    }
}
```

## Benefits

### ✅ Extensibility
- Add workflows without modifying CommandPaletteForm
- Create workflows by composing existing nodes
- Third-party plugins can add new workflows

### ✅ Reusability
- Nodes reused across workflows
- Built-in components available to any workflow
- Common patterns (filter, loop, transform) as utilities

### ✅ Testability
- Nodes tested in isolation
- Workflows tested without UI
- Mock services injected via IServiceProvider

### ✅ Maintainability
- Clear separation of concerns
- Smaller, focused classes
- Easy to understand data flow

### ✅ Flexibility
- Simple (1 node) or complex (20+ nodes) workflows
- Conditional branching, loops, transformations
- Dynamic workflow generation possible

## Next Steps

See the following documentation for more details:

- [Node Reference](NodeReference.md) - Complete list of built-in nodes
- [Workflow Examples](WorkflowExamples.md) - Example workflows
- [Plugin Development Guide](PluginDevelopment.md) - Creating plugins
- [Testing Guide](TestingGuide.md) - Testing workflows and nodes
