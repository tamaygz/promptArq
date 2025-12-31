# List Item Rendering System

## Overview

The command palette uses a template-based rendering system that is fully integrated with the workflow architecture. This system allows nodes to control how their items are displayed using predefined templates while maintaining consistent visual patterns across the application.

## Architecture

The rendering system is built on three key interfaces and classes:

### 1. `ItemRenderTemplate` Enum

Defines available rendering templates:

- **Standard**: Icon on left (spans both rows), Row 1: main text, Row 2: hint/explanation
- **Badge**: Colored badge on left, Row 1: title, Row 2: description
- **Simple**: Single line text, centered vertically
- **Detailed**: Icon on left, Row 1: title + metadata (right-aligned), Row 2: description
- **Custom**: Node provides complete custom rendering logic

### 2. `ItemRenderData` Class

Contains all data needed to render a list item:

```csharp
public class ItemRenderData
{
    public string MainText { get; set; }              // Primary text (required)
    public string? SecondaryText { get; set; }        // Secondary text (optional)
    public string? Icon { get; set; }                 // Icon/emoji (optional)
    public string? BadgeText { get; set; }            // Badge text (optional)
    public Color? BadgeColor { get; set; }            // Badge color (optional)
    public Color? ItemColor { get; set; }             // Custom item color (optional)
    public string? MetadataText { get; set; }         // Right-aligned metadata (optional)
    public ItemRenderTemplate Template { get; set; }  // Template to use
    public object? OriginalItem { get; set; }         // Original item reference
}
```

### 3. `INodeItemRenderer` Interface

Nodes implement this interface to control item rendering:

```csharp
public interface INodeItemRenderer
{
    // Get render data for an item
    ItemRenderData GetItemRenderData(object item);
    
    // Optional: Custom rendering for Complex template
    bool CustomRenderItem(Graphics graphics, Rectangle bounds, object item, bool isSelected);
}
```

### 4. `WorkflowItemRenderer` Class

Renders items based on templates:

```csharp
public class WorkflowItemRenderer
{
    public WorkflowItemRenderer(Theme theme);
    public void RenderItem(Graphics g, Rectangle bounds, ItemRenderData renderData, bool isSelected);
}
```

## Usage

### Basic Usage (Using Base Implementation)

All `InputNodeBase` classes automatically implement `INodeItemRenderer` with a default `GetItemRenderData()` that uses the Standard template:

```csharp
public class MyNode : InputNodeBase
{
    // No need to override GetItemRenderData() - default Standard template is used
    
    public override string GetDisplayText(object item) => "Item Title";
    public override string GetSecondaryText(object item) => "Item Description";
    public override string GetIcon(object item) => "📝";
}
```

### Custom Template Usage

Override `GetItemRenderData()` to use different templates:

```csharp
public class SearchPromptsNode : InputNodeBase
{
    public override ItemRenderData GetItemRenderData(object item)
    {
        if (item is PromptInfo prompt)
        {
            return new ItemRenderData
            {
                MainText = prompt.Title,
                SecondaryText = prompt.Description,
                BadgeText = prompt.ProjectName.Substring(0, 3).ToUpper(),
                BadgeColor = Color.FromArgb(100, 150, 200),
                Template = ItemRenderTemplate.Badge,
                OriginalItem = item
            };
        }
        
        // Fallback to default
        return base.GetItemRenderData(item);
    }
}
```

### Advanced: Custom Rendering

For complete control, override `CustomRenderItem()`:

```csharp
public class AdvancedNode : InputNodeBase
{
    public override ItemRenderData GetItemRenderData(object item)
    {
        return new ItemRenderData
        {
            Template = ItemRenderTemplate.Custom,
            OriginalItem = item
        };
    }
    
    public override bool CustomRenderItem(Graphics g, Rectangle bounds, object item, bool isSelected)
    {
        // Your custom rendering code here
        g.DrawString("Custom", Font, Brushes.White, bounds);
        return true; // Return true to indicate custom rendering was performed
    }
}
```

## Templates in Detail

### Standard Template

**Use Case**: Actions, commands, generic items with icons

**Layout**:
```
[Icon]  Main Text
        Secondary Text
```

**Example**:
```csharp
new ItemRenderData
{
    Icon = "📋",
    MainText = "Copy to Clipboard",
    SecondaryText = "Copy the content to your clipboard",
    Template = ItemRenderTemplate.Standard
}
```

### Badge Template

**Use Case**: Items with categories, projects, or types

**Layout**:
```
[BADGE] Main Text
        Secondary Text
```

**Example**:
```csharp
new ItemRenderData
{
    BadgeText = "SYS",
    BadgeColor = Color.FromArgb(150, 100, 200),
    MainText = "System Prompt",
    SecondaryText = "A system-level prompt template",
    Template = ItemRenderTemplate.Badge
}
```

### Simple Template

**Use Case**: Single-line items, placeholders, instructions

**Layout**:
```
Main Text (centered vertically)
```

**Example**:
```csharp
new ItemRenderData
{
    MainText = "Enter value for: username",
    Template = ItemRenderTemplate.Simple
}
```

### Detailed Template

**Use Case**: Items with metadata, timestamps, or additional info

**Layout**:
```
[Icon]  Main Text                    Metadata
        Secondary Text
```

**Example**:
```csharp
new ItemRenderData
{
    Icon = "📄",
    MainText = "Document Title",
    SecondaryText = "Document description or preview",
    MetadataText = "⭐ Recent",
    Template = ItemRenderTemplate.Detailed
}
```

## Best Practices

### 1. Choose the Right Template

- **Standard**: Default for most items, especially actions with icons
- **Badge**: When items belong to categories or projects
- **Simple**: For instructions, prompts, or single-line entries
- **Detailed**: When metadata (like timestamps or status) is important
- **Custom**: Only when none of the templates fit your needs

### 2. Consistent Visual Language

- Use consistent badge colors for similar item types
- Use meaningful icons (emojis work great!)
- Keep main text concise and descriptive
- Use secondary text for additional context

### 3. Performance Considerations

- The default implementation is optimized for performance
- Custom rendering should avoid heavy operations
- Cache computed values when possible

### 4. Accessibility

- Ensure sufficient contrast between text and background
- Don't rely solely on color to convey information
- Provide meaningful text for screen readers

## Integration with Workflow System

The rendering system is fully integrated with the workflow architecture:

1. **Nodes Control Rendering**: Each node decides how its items are rendered
2. **Template-Based**: Consistent visual patterns across all workflows
3. **Extensible**: Easy to add new templates or custom rendering
4. **Theme-Aware**: Automatically uses current theme colors
5. **Backward Compatible**: Falls back to legacy rendering for old code

## Example: Complete Node Implementation

```csharp
public class SelectFileNode : InputNodeBase
{
    public override string Name => "Select File";
    public override NodeUIType UIType => NodeUIType.ItemList;
    public override string HintText => "Select a file to process";
    
    public override IEnumerable<object> GetItems(WorkflowContext context)
    {
        var files = context.Get<List<FileInfo>>("files");
        return files;
    }
    
    public override ItemRenderData GetItemRenderData(object item)
    {
        if (item is FileInfo file)
        {
            var icon = file.Extension switch
            {
                ".txt" => "📄",
                ".pdf" => "📕",
                ".jpg" => "🖼️",
                _ => "📎"
            };
            
            var sizeKB = file.Length / 1024;
            var metadata = $"{sizeKB} KB";
            
            return new ItemRenderData
            {
                Icon = icon,
                MainText = file.Name,
                SecondaryText = file.DirectoryName,
                MetadataText = metadata,
                Template = ItemRenderTemplate.Detailed,
                OriginalItem = item
            };
        }
        
        return base.GetItemRenderData(item);
    }
}
```

## Future Enhancements

Potential future additions to the rendering system:

1. **Multi-column Template**: Display items in a grid layout
2. **Progress Template**: Show progress bars for long-running operations
3. **Image Template**: Display thumbnail images alongside text
4. **Hierarchical Template**: Support for nested/indented items
5. **Animation Support**: Subtle animations for state changes

## Summary

The list item rendering system provides:

- ✅ **Consistent UI** - Predefined templates ensure visual consistency
- ✅ **Flexibility** - Nodes can choose templates or provide custom rendering
- ✅ **Extensibility** - Easy to add new templates
- ✅ **Theme Integration** - Automatically uses theme colors
- ✅ **Clean Architecture** - Aligns with workflow system design
- ✅ **Backward Compatibility** - Works with existing code

The system makes it easy for developers to create beautiful, consistent UIs without worrying about low-level rendering details.
