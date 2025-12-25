# Creating Custom Workflow Nodes

This guide explains how to create custom nodes for the PromptArq workflow system.

## Table of Contents
- [Node Architecture](#node-architecture)
- [Creating a Simple Node](#creating-a-simple-node)
- [Creating a UI Node](#creating-a-ui-node)
- [Node Categories](#node-categories)
- [Best Practices](#best-practices)

## Node Architecture

Every workflow node implements the `IWorkflowNode` interface:

```csharp
public interface IWorkflowNode
{
    string Id { get; }
    string Name { get; }
    Task<WorkflowResult> ExecuteAsync(WorkflowContext context);
}
```

Nodes that need to display UI also implement `INodeUIProvider`:

```csharp
public interface INodeUIProvider
{
    NodeUIType UIType { get; }
    string HintText { get; }
    bool ReadOnly { get; }
    IEnumerable<object> GetItems(WorkflowContext context);
    string GetDisplayText(object item);
    string GetSecondaryText(object item);
    string GetIcon(object item);
    Color? GetItemColor(object item);
}
```

## Creating a Simple Node

Here's a simple action node that transforms text to uppercase:

```csharp
using PromptArqApp.Workflow.Core;
using PromptArqApp.Workflow.Nodes;

namespace MyApp.Workflows
{
    public class UppercaseNode : ActionNodeBase
    {
        public override string Name => "Uppercase";

        public UppercaseNode(IServiceProvider services) : base(services) { }

        public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
        {
            // Get input from context
            var input = context.GetOrDefault<string>("text", "");
            
            // Transform
            var output = input.ToUpper();
            
            // Store result in context
            context.Set("text", output);
            
            // Return success
            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }
    }
}
```

## Creating a UI Node

Here's a node that displays a list of options:

```csharp
public class SelectColorNode : InputNodeBase
{
    private readonly List<string> _colors = new() { "Red", "Green", "Blue", "Yellow" };

    public override string Name => "Select Color";
    public override NodeUIType UIType => NodeUIType.ItemList;
    public override string HintText => "Select a color";
    public override bool ReadOnly => false;

    public SelectColorNode(IServiceProvider services) : base(services) { }

    public override IEnumerable<object> GetItems(WorkflowContext context)
    {
        var query = context.GetOrDefault<string>("searchQuery", "").ToLower();
        
        return string.IsNullOrEmpty(query)
            ? _colors
            : _colors.Where(c => c.ToLower().Contains(query));
    }

    public override string GetDisplayText(object item)
    {
        return item?.ToString() ?? "";
    }

    public override string GetSecondaryText(object item)
    {
        return $"Choose {item}";
    }

    public override string GetIcon(object item)
    {
        return item?.ToString() switch
        {
            "Red" => "🔴",
            "Green" => "🟢",
            "Blue" => "🔵",
            "Yellow" => "🟡",
            _ => "⚪"
        };
    }

    public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
    {
        // Get selected item from context
        if (context.Has("selectedItem"))
        {
            var selectedColor = context.Get<string>("selectedItem");
            context.Set("color", selectedColor);
            return Task.FromResult(WorkflowResult.CreateSuccess(context));
        }

        // No selection yet - show UI
        return Task.FromResult(WorkflowResult.CreateSuccess(context));
    }
}
```

## Node Categories

### 1. Input Nodes
Accept user input and provide search/filtering.

**Base Class:** `InputNodeBase`

**Example Use Cases:**
- Search and select prompts
- Enter text
- Select from dropdown
- Fill form fields

**Key Methods:**
- `GetItems()` - Return list of items to display
- `GetDisplayText()` - Format item for display
- `ExecuteAsync()` - Handle selection

### 2. Action Nodes
Perform operations and transformations.

**Base Class:** `ActionNodeBase`

**Example Use Cases:**
- Call APIs
- Transform data
- Execute LLM prompts
- File operations

**Key Methods:**
- `ExecuteAsync()` - Perform the action

### 3. UI Nodes
Display information to users.

**Base Class:** `UINodeBase`

**Example Use Cases:**
- Show text panels
- Display notifications
- Show confirmation dialogs

**Key Methods:**
- `GetItems()` - Return display items
- `ExecuteAsync()` - Handle user response

### 4. Utility Nodes
Control flow and data manipulation.

**Base Class:** `UtilityNodeBase`

**Example Use Cases:**
- Conditional branching
- Loops
- Data transformation
- Aggregation

**Key Methods:**
- `ExecuteAsync()` - Execute logic and return next node

### 5. Output Nodes
Final actions and side effects.

**Base Class:** `OutputNodeBase`

**Example Use Cases:**
- Close UI
- Record history
- Send notifications
- Save to file

**Key Methods:**
- `ExecuteAsync()` - Perform final action

## Node Context

Nodes communicate through the `WorkflowContext`:

```csharp
// Store data
context.Set("key", value);

// Retrieve data
var value = context.Get<string>("key");

// Check existence
if (context.Has("key")) { ... }

// Get with default
var value = context.GetOrDefault("key", "default");

// Remove data
context.Remove("key");

// Access services
var service = context.Services.GetService<IMyService>();
```

## Conditional Branching

Nodes can specify the next node to execute:

```csharp
public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
{
    var hasError = context.GetOrDefault<bool>("hasError", false);
    
    // Branch based on condition
    var nextNodeId = hasError ? "error-handler" : "success-node";
    
    return Task.FromResult(WorkflowResult.CreateSuccess(
        context, 
        nextNodeId: nextNodeId
    ));
}
```

## Error Handling

Return error results when something goes wrong:

```csharp
public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
{
    try
    {
        // Do work...
        return Task.FromResult(WorkflowResult.CreateSuccess(context));
    }
    catch (Exception ex)
    {
        return Task.FromResult(WorkflowResult.CreateError(
            context,
            $"Failed to process: {ex.Message}"
        ));
    }
}
```

## Configuration

Nodes can accept configuration:

```csharp
public class DelayNode : UtilityNodeBase
{
    private int _delayMs = 1000;

    public override void Configure(Dictionary<string, object>? config)
    {
        base.Configure(config);
        
        if (config != null && config.TryGetValue("delayMs", out var value))
        {
            _delayMs = Convert.ToInt32(value);
        }
    }

    public override async Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
    {
        await Task.Delay(_delayMs);
        return WorkflowResult.CreateSuccess(context);
    }
}
```

## Best Practices

### 1. Single Responsibility
Each node should do one thing well.

✅ Good:
```csharp
public class FetchUserNode : ActionNodeBase { ... }
public class ValidateUserNode : UtilityNodeBase { ... }
```

❌ Bad:
```csharp
public class FetchAndValidateUserNode : ActionNodeBase { ... }
```

### 2. Use Context Effectively
Store all data in context for sharing between nodes.

```csharp
context.Set("user", user);
context.Set("timestamp", DateTime.UtcNow);
```

### 3. Dependency Injection
Use constructor injection for services:

```csharp
public class EmailNode : ActionNodeBase
{
    private readonly IEmailService _emailService;

    public EmailNode(IServiceProvider services) : base(services)
    {
        _emailService = services.GetRequiredService<IEmailService>();
    }
}
```

### 4. Async/Await
Use async operations for I/O:

```csharp
public override async Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
{
    var result = await _httpClient.GetAsync(url);
    // ...
}
```

### 5. Error Handling
Always handle errors gracefully:

```csharp
try
{
    // Risky operation
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    return WorkflowResult.CreateError(context, ex.Message);
}
```

### 6. Validation
Validate inputs early:

```csharp
public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
{
    if (!context.Has("requiredInput"))
    {
        return Task.FromResult(WorkflowResult.CreateError(
            context,
            "Missing required input"
        ));
    }
    
    // Proceed...
}
```

### 7. Logging
Log important events:

```csharp
_logger.LogInformation("Processing item {ItemId}", itemId);
_logger.LogWarning("Retrying operation, attempt {Attempt}", attempt);
_logger.LogError(ex, "Operation failed");
```

## Testing Nodes

Example unit test:

```csharp
[Fact]
public async Task UppercaseNode_ConvertsToUppercase()
{
    // Arrange
    var services = new ServiceCollection().BuildServiceProvider();
    var node = new UppercaseNode(services);
    var context = new WorkflowContext(services);
    context.Set("text", "hello");

    // Act
    var result = await node.ExecuteAsync(context);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("HELLO", result.Context.Get<string>("text"));
}
```

## Next Steps

- [Creating Workflows Guide](CreatingWorkflows.md)
- [Plugin Development Guide](PluginDevelopment.md)
- [Available Nodes Reference](AvailableNodes.md)
