# Creating Workflows

This guide explains how to create workflows in the PromptArq workflow system.

## Table of Contents
- [What is a Workflow?](#what-is-a-workflow)
- [Workflow Structure](#workflow-structure)
- [Creating a Simple Workflow](#creating-a-simple-workflow)
- [Complex Workflows](#complex-workflows)
- [Workflow Validation](#workflow-validation)
- [Best Practices](#best-practices)

## What is a Workflow?

A workflow is a directed graph of nodes that execute in sequence to accomplish a task. Think of it as a flowchart where each box is a node that performs an action.

**Example workflow:**
```
Search Prompts → Select Action → Execute → Copy → Notify → Close
```

## Workflow Structure

A workflow consists of:

1. **Metadata** - ID, name, description
2. **Nodes** - List of node definitions
3. **Connections** - How nodes link together
4. **Entry Point** - Where execution starts

```csharp
var workflow = new Workflow
{
    Id = "my-workflow",
    Name = "My Workflow",
    Description = "Does something useful",
    EntryNodeId = "start",
    Nodes = new List<WorkflowNodeDefinition>
    {
        new() { Id = "start", NodeType = "MyStartNode" },
        new() { Id = "end", NodeType = "MyEndNode" }
    },
    Connections = new Dictionary<string, string>
    {
        ["start"] = "end"
    }
};
```

## Creating a Simple Workflow

### Example: Quick Copy Workflow

This workflow searches prompts, lets user select one, and copies it to clipboard.

```csharp
var quickCopyWorkflow = new Workflow
{
    Id = "quick-copy",
    Name = "Quick Copy",
    Description = "Search and copy a prompt to clipboard",
    Icon = "📋",
    EntryNodeId = "search",
    
    Nodes = new List<WorkflowNodeDefinition>
    {
        new()
        {
            Id = "search",
            NodeType = "SearchPromptsNode"
        },
        new()
        {
            Id = "select-action",
            NodeType = "SelectActionNode"
        },
        new()
        {
            Id = "check-copy",
            NodeType = "ConditionalNode",
            Configuration = new Dictionary<string, object>
            {
                ["condition"] = "action == Copy"
            }
        },
        new()
        {
            Id = "copy",
            NodeType = "CopyToClipboardNode"
        },
        new()
        {
            Id = "notify",
            NodeType = "ShowNotificationNode",
            Configuration = new Dictionary<string, object>
            {
                ["message"] = "Copied to clipboard!"
            }
        },
        new()
        {
            Id = "close",
            NodeType = "CloseCommandPaletteNode"
        }
    },
    
    Connections = new Dictionary<string, string>
    {
        ["search"] = "select-action",
        ["select-action"] = "check-copy",
        ["check-copy"] = "copy",  // if condition true
        ["copy"] = "notify",
        ["notify"] = "close"
    }
};
```

### Registering the Workflow

```csharp
registry.RegisterWorkflow(quickCopyWorkflow);
```

## Complex Workflows

### Conditional Branching

Use `ConditionalNode` to branch based on context data:

```csharp
new()
{
    Id = "check-has-placeholders",
    NodeType = "ConditionalNode",
    Configuration = new Dictionary<string, object>
    {
        ["condition"] = "hasPlaceholders",
        ["trueNodeId"] = "fill-placeholders",
        ["falseNodeId"] = "execute-directly"
    }
}
```

### Loops

Use `LoopNode` to iterate over collections:

```csharp
new()
{
    Id = "fill-loop",
    NodeType = "LoopNode",
    Configuration = new Dictionary<string, object>
    {
        ["itemsKey"] = "placeholders",
        ["loopBodyNodeId"] = "fill-one",
        ["exitNodeId"] = "apply-values"
    }
},
new()
{
    Id = "fill-one",
    NodeType = "FillPlaceholderNode"
}
```

The loop will:
1. Get items from `context.Get<List>("placeholders")`
2. For each item, execute `fill-one` node
3. When done, move to `apply-values` node

### Multiple Entry Points

Workflows can have sub-workflows or alternative paths:

```csharp
Connections = new Dictionary<string, string>
{
    ["search"] = "action",
    ["action"] = "check-type",
    
    // Branch 1: Copy
    ["check-type-copy"] = "copy",
    ["copy"] = "close",
    
    // Branch 2: Paste
    ["check-type-paste"] = "paste",
    ["paste"] = "close",
    
    // Branch 3: Execute
    ["check-type-execute"] = "execute",
    ["execute"] = "show-result",
    ["show-result"] = "close"
}
```

## Workflow Validation

The workflow system automatically validates workflows on registration:

### Validation Checks

✅ **Structure Validation**
- Non-empty ID and Name
- At least one node
- Valid entry node ID
- No duplicate node IDs

✅ **Connection Validation**
- All source nodes exist
- All target nodes exist
- No orphaned nodes (unreachable from entry)

✅ **Cycle Detection**
- Detects circular dependencies
- Prevents infinite loops

### Example Validation Errors

```csharp
// Missing entry node
var errors = validator.Validate(workflow);
// ["EntryNodeId 'missing' does not match any node in the workflow"]

// Circular dependency
var errors = validator.Validate(workflow);
// ["Workflow contains cycles: node1 → node2 → node3 → node1"]

// Orphaned node
var errors = validator.Validate(workflow);
// ["Workflow contains orphaned nodes (unreachable): node5, node6"]
```

## Node Configuration

Nodes can accept configuration parameters:

```csharp
new WorkflowNodeDefinition
{
    Id = "delay",
    NodeType = "DelayNode",
    Configuration = new Dictionary<string, object>
    {
        ["delayMs"] = 1000,
        ["showProgress"] = true
    }
}
```

The node receives this configuration in its `Configure` method:

```csharp
public override void Configure(Dictionary<string, object>? config)
{
    base.Configure(config);
    if (config != null)
    {
        _delayMs = (int)config["delayMs"];
        _showProgress = (bool)config["showProgress"];
    }
}
```

## Workflow Metadata

Add metadata for better organization:

```csharp
var workflow = new Workflow
{
    Id = "advanced-workflow",
    Name = "Advanced Workflow",
    Description = "Does advanced things",
    Icon = "⚡",
    
    Metadata = new WorkflowMetadata
    {
        Author = "Your Name",
        Version = new Version(1, 0, 0),
        Tags = new[] { "advanced", "utility" },
        RequiredServices = new[] { "IEmailService", "IStorageService" }
    },
    
    // ... nodes and connections
};
```

## Dynamic Next Node

Nodes can dynamically determine the next node:

```csharp
public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
{
    var userChoice = context.Get<string>("choice");
    
    var nextNode = userChoice switch
    {
        "option1" => "node-for-option1",
        "option2" => "node-for-option2",
        _ => "default-node"
    };
    
    return Task.FromResult(WorkflowResult.CreateSuccess(
        context,
        nextNodeId: nextNode
    ));
}
```

## Workflow Switching

Workflows can transition to other workflows:

```csharp
public override Task<WorkflowResult> ExecuteAsync(WorkflowContext context)
{
    // Trigger workflow switch
    context.Set("switchToWorkflow", "other-workflow-id");
    
    return Task.FromResult(WorkflowResult.CreateSuccess(context));
}
```

The engine detects the `switchToWorkflow` flag and transitions automatically.

## Best Practices

### 1. Keep Workflows Focused

Each workflow should accomplish one main task.

✅ Good:
- "Quick Copy" - Copy a prompt
- "Fill Placeholders" - Fill and execute prompt
- "One-Time Prompt" - Create and execute one-time prompt

❌ Bad:
- "Do Everything" - Tries to handle all cases

### 2. Use Descriptive IDs

```csharp
// Good
Id = "search-prompts"
Id = "check-has-placeholders"
Id = "execute-llm"

// Bad
Id = "node1"
Id = "n2"
Id = "x"
```

### 3. Add Comments in Configuration

```csharp
new WorkflowNodeDefinition
{
    Id = "conditional",
    NodeType = "ConditionalNode",
    Configuration = new Dictionary<string, object>
    {
        // Check if user selected "Execute" action
        ["condition"] = "action == Execute",
        ["trueNodeId"] = "execute-llm",
        ["falseNodeId"] = "copy-only"
    }
}
```

### 4. Handle Errors

Include error handling nodes:

```csharp
Nodes = new List<WorkflowNodeDefinition>
{
    new() { Id = "try-execute", NodeType = "ExecuteLLMNode" },
    new() { Id = "on-error", NodeType = "ShowErrorNode" },
    new() { Id = "on-success", NodeType = "ShowSuccessNode" }
}
```

### 5. Provide User Feedback

Always notify users of results:

```csharp
new() { Id = "notify-success", NodeType = "ShowNotificationNode" },
new() { Id = "notify-error", NodeType = "ShowNotificationNode" }
```

### 6. Test Workflows

Create integration tests:

```csharp
[Fact]
public async Task QuickCopyWorkflow_CopiesPromptToClipboard()
{
    // Arrange
    var workflow = CreateQuickCopyWorkflow();
    registry.RegisterWorkflow(workflow);
    
    var context = new WorkflowContext(services);
    context.Set("selectedPrompt", testPrompt);
    context.Set("selectedAction", PromptActionType.Copy);
    
    // Act
    var result = await engine.StartWorkflowAsync("quick-copy", context);
    
    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(testPrompt.Content, Clipboard.GetText());
}
```

### 7. Document Your Workflows

Add clear descriptions:

```csharp
var workflow = new Workflow
{
    Id = "complex-workflow",
    Name = "Complex Workflow",
    Description = @"
        This workflow:
        1. Searches for prompts
        2. Allows user to select one
        3. Checks for placeholders
        4. If placeholders exist, fills them in a loop
        5. Executes the prompt through LLM
        6. Copies result to clipboard
        7. Shows success notification
    ",
    // ...
};
```

## Example: Complete Workflow with Loop

```csharp
var fillPlaceholdersWorkflow = new Workflow
{
    Id = "fill-placeholders",
    Name = "Fill Placeholders and Execute",
    Description = "Fill template variables and execute prompt",
    Icon = "📝",
    EntryNodeId = "search",
    
    Nodes = new List<WorkflowNodeDefinition>
    {
        new() { Id = "search", NodeType = "SearchPromptsNode" },
        new() { Id = "select-action", NodeType = "SelectActionNode" },
        
        // Check if we need to fill placeholders
        new()
        {
            Id = "check-fill",
            NodeType = "ConditionalNode",
            Configuration = new()
            {
                ["condition"] = "action == FillPlaceholders"
            }
        },
        
        // Get list of placeholders
        new() { Id = "get-placeholders", NodeType = "GetPlaceholdersNode" },
        
        // Check if placeholders exist
        new()
        {
            Id = "check-has-placeholders",
            NodeType = "ConditionalNode",
            Configuration = new()
            {
                ["condition"] = "hasPlaceholders"
            }
        },
        
        // Loop through each placeholder
        new()
        {
            Id = "fill-loop",
            NodeType = "LoopNode",
            Configuration = new()
            {
                ["itemsKey"] = "placeholders"
            }
        },
        
        // Fill individual placeholder
        new() { Id = "fill-one", NodeType = "FillPlaceholderNode" },
        
        // Apply all placeholder values
        new() { Id = "apply", NodeType = "ApplyPlaceholdersNode" },
        
        // Select output action
        new() { Id = "output-action", NodeType = "SelectOutputActionNode" },
        
        // Execute through LLM
        new() { Id = "execute", NodeType = "ExecuteLLMNode" },
        
        // Copy result
        new() { Id = "copy", NodeType = "CopyToClipboardNode" },
        
        // Notify user
        new() { Id = "notify", NodeType = "ShowNotificationNode" },
        
        // Close palette
        new() { Id = "close", NodeType = "CloseCommandPaletteNode" }
    },
    
    Connections = new Dictionary<string, string>
    {
        ["search"] = "select-action",
        ["select-action"] = "check-fill",
        ["check-fill"] = "get-placeholders",
        ["get-placeholders"] = "check-has-placeholders",
        ["check-has-placeholders"] = "fill-loop",
        ["fill-loop"] = "fill-one",
        ["fill-one"] = "fill-loop",  // Loop back
        ["fill-loop-exit"] = "apply",
        ["apply"] = "output-action",
        ["output-action"] = "execute",
        ["execute"] = "copy",
        ["copy"] = "notify",
        ["notify"] = "close"
    }
};
```

## Next Steps

- [Creating Custom Nodes](CreatingCustomNodes.md)
- [Plugin Development](PluginDevelopment.md)
- [Available Nodes Reference](AvailableNodes.md)
