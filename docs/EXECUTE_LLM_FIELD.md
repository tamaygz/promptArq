# Execute LLM Field Documentation

## Overview

The `execute_llm` field is a boolean flag added to the `Prompt` type that controls whether a prompt should be executed directly or passed through the LLM (Large Language Model) preprocessing/execution pipeline.

## Purpose

This field allows users to differentiate between two types of prompts:

1. **Direct Execution Prompts** (`execute_llm: false`): These prompts are executed directly, typically just copying the content to the clipboard or using it as-is.
2. **LLM-Processed Prompts** (`execute_llm: true`): These prompts are passed through the LLM execution pipeline, potentially combining them with system prompts and processing them through an AI model.

## Default Value

The default value for `execute_llm` is **always `false`**. This applies to:
- Newly created prompts
- Existing prompts that don't have the field (migration)
- Any prompt where the field is undefined or missing

## Implementation Details

### TypeScript Type Definition

Located in `src/lib/types.ts`:

```typescript
export type Prompt = {
  id: string
  title: string
  description: string
  content: string
  projectId: string
  categoryId: string
  tags: string[]
  createdBy: string
  createdAt: number
  updatedAt: number
  isArchived: boolean
  exposedToMCP: boolean
  execute_llm: boolean  // ← New field
}
```

### Migration Logic

The application includes automatic migration logic in `src/App.tsx` to ensure all existing prompts have the `execute_llm` field:

```typescript
// Migration: Ensure all prompts have execute_llm field (default to false)
useEffect(() => {
  if (prompts && prompts.length > 0) {
    const needsMigration = prompts.some(p => p.execute_llm === undefined)
    if (needsMigration) {
      const migratedPrompts = prompts.map(p => ({
        ...p,
        execute_llm: p.execute_llm ?? false
      }))
      setPrompts(migratedPrompts)
      console.log('Migrated prompts to include execute_llm field')
    }
  }
}, []) // Only run once on mount
```

### User Interface

#### Prompt Editor

The `PromptEditor` component (`src/components/PromptEditor.tsx`) includes a checkbox to toggle the `execute_llm` field:

```tsx
<div className="flex items-center gap-3 p-4 md:p-5 bg-muted/30 rounded-lg border border-border">
  <Checkbox 
    id="executeLLM" 
    checked={executeLLM}
    onCheckedChange={(checked) => setExecuteLLM(checked === true)}
  />
  <div className="flex-1">
    <Label htmlFor="executeLLM" className="text-sm font-medium cursor-pointer">
      Execute through LLM
    </Label>
    <p className="text-xs text-muted-foreground mt-1">
      Pass prompt text through LLM preprocessing/execution pipeline when executing
    </p>
  </div>
</div>
```

#### Prompt List

The `PromptList` component (`src/components/PromptList.tsx`) displays a badge for prompts with `execute_llm: true`:

```tsx
{prompt.execute_llm && (
  <Badge variant="secondary" className="text-xs px-1.5 md:px-2 py-0.5 gap-1">
    <Sparkle size={12} />
    <span className="hidden md:inline">LLM</span>
  </Badge>
)}
```

### Execution Logic

The `ExecuteDialog` component (`src/components/ExecuteDialog.tsx`) respects the `execute_llm` flag:

- **If `execute_llm` is `false`**: The prompt content is copied directly to the clipboard without LLM processing.
- **If `execute_llm` is `true`**: The prompt is passed through the LLM execution pipeline with system prompts.

```typescript
const handleExecute = async () => {
  if (!content.trim()) {
    toast.error('No content to execute')
    return
  }

  // Check if execute_llm is false - if so, just copy content directly
  if (prompt && !prompt.execute_llm) {
    try {
      await navigator.clipboard.writeText(content)
      setExecutionResult(content)
      toast.success('Content copied to clipboard (direct execution)')
    } catch (err) {
      setExecutionResult(content)
      toast.success('Content ready (direct execution)')
    }
    return
  }

  // LLM execution logic follows...
}
```

## Windows App Integration

### Data Model

The Windows app `PromptInfo` class (`WindowsApp/PromptAction.cs`) includes the `ExecuteLLM` property:

```csharp
public class PromptInfo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Content { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool IsArchived { get; set; }
    public bool HasPlaceholders { get; set; }
    public bool ExecuteLLM { get; set; }  // ← New field
}
```

### Data Fetching

The `MainForm.cs` fetches the `execute_llm` field from storage and maps it to the `ExecuteLLM` property:

```csharp
result.Add(new PromptInfo
{
    Id = promptElem.TryGetProperty("id", out var promptIdProp) ? promptIdProp.GetString() ?? "" : "",
    Title = promptElem.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
    // ... other properties ...
    ExecuteLLM = promptElem.TryGetProperty("execute_llm", out var execLLM) && execLLM.GetBoolean()
});
```

**Note**: If the `execute_llm` field is missing from the JSON, `TryGetProperty` returns `false`, and the default value of `false` is used for `ExecuteLLM`.

### Execution Behavior

The Windows app should respect the `ExecuteLLM` flag during prompt execution:
- If `ExecuteLLM` is `true`: Route through LLM execution pipeline
- If `ExecuteLLM` is `false`: Execute directly (copy to clipboard, paste directly, etc.)

## Storage and Persistence

### JSON Serialization

The field is always serialized as an explicit boolean value:

```json
{
  "id": "prompt-123",
  "title": "Example Prompt",
  "execute_llm": false
}
```

### Storage Adapters

All storage adapters (Spark KV, LocalStorage, SQLite, HTTP) handle the field transparently:
- When saving: The field is always included
- When loading: Missing values default to `false` via the migration logic

## Testing

A comprehensive test suite is available in `src/lib/execute-llm-field.test.ts`:

```bash
# Run tests
npx tsx src/lib/execute-llm-field.test.ts
```

Tests cover:
1. New prompt creation with default `false` value
2. Migration of old prompts without the field
3. Prompts with `execute_llm: true`
4. Bulk migration of prompt arrays
5. JSON serialization and deserialization

## Backwards Compatibility

The implementation maintains full backwards compatibility:

1. **Missing Field Handling**: Old prompts without the `execute_llm` field are automatically migrated to include it with a default value of `false`.

2. **No Breaking Changes**: Existing prompts continue to work as before since the default value (`false`) preserves the original direct execution behavior.

3. **Storage Compatibility**: The field is optional in storage, with fallback logic to ensure it always has a value when accessed.

## Usage Examples

### Creating a New Prompt with LLM Execution

```typescript
const newPrompt: Prompt = {
  id: `prompt-${Date.now()}`,
  title: 'AI-Enhanced Prompt',
  description: 'This prompt will be processed by LLM',
  content: 'Analyze the following code...',
  projectId: projectId,
  categoryId: categoryId,
  tags: [],
  createdBy: 'user-123',
  createdAt: Date.now(),
  updatedAt: Date.now(),
  isArchived: false,
  exposedToMCP: false,
  execute_llm: true  // Enable LLM execution
}
```

### Creating a Direct Execution Prompt

```typescript
const directPrompt: Prompt = {
  id: `prompt-${Date.now()}`,
  title: 'Template Snippet',
  description: 'Copy this template directly',
  content: 'Dear {{name}},\n\nThank you for...',
  projectId: projectId,
  categoryId: categoryId,
  tags: [],
  createdBy: 'user-123',
  createdAt: Date.now(),
  updatedAt: Date.now(),
  isArchived: false,
  exposedToMCP: false,
  execute_llm: false  // Direct execution (default)
}
```

## Future Enhancements

Potential future enhancements could include:

1. **Conditional LLM Models**: Allow specifying which LLM model to use based on the prompt type
2. **Execution Presets**: Pre-configured execution profiles (direct, LLM-light, LLM-full)
3. **Analytics**: Track usage patterns of direct vs. LLM execution
4. **Batch Processing**: Process multiple prompts with different execution modes

## Security Considerations

- The field itself has no security implications
- LLM execution should still respect authentication and authorization requirements
- System prompts and model configurations should be validated regardless of the `execute_llm` flag

## Performance Considerations

- Direct execution (`execute_llm: false`) is faster as it bypasses LLM processing
- Users can optimize their workflow by setting appropriate values based on prompt needs
- The migration logic runs only once on application start, with minimal performance impact
