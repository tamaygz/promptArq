# promptArq - LLM Prompt Management

A comprehensive prompt management system for LLMs with versioning, AI-powered improvements, team collaboration, and MCP (Model Context Protocol) support.

## Features

- **Prompt Management**: Create, edit, and organize prompts with rich metadata
- **Version Control**: Track changes with full version history and diff visualization
- **Projects & Categories**: Organize prompts into projects with customizable categories
- **Tags**: Flexible tagging system with color-coded labels
- **AI-Powered Improvements**: Get intelligent suggestions to enhance your prompts
- **System Prompts**: Reusable system prompts for consistent behavior
- **Model Configurations**: Save and manage model parameters (temperature, max tokens, etc.)
- **MCP Server Integration**: Expose prompts to AI agents via Model Context Protocol
- **Export/Import**: Export all prompts and configurations for backup or sharing
- **User Profiles**: Built-in authentication and user management

## Getting Started

No configuration required! The application uses Spark runtime's built-in authentication and storage.

### Authentication

Authentication is handled automatically by the Spark runtime. Simply access the application and you'll be authenticated with your Spark session.

### MCP Server Configuration

To connect your prompts to Claude Desktop or other MCP clients:

1. Click the "MCP Server" button in the header
2. Copy the recommended configuration
3. Add it to your Claude Desktop config file:
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`

The configuration uses the official `@modelcontextprotocol/server-fetch` package and looks like this:

```json
{
  "mcpServers": {
    "promptArq-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://your-app-url/api/mcp"]
    }
  }
}
```

### Creating Your First Prompt

1. Click "New Prompt" in the header
2. Fill in the prompt details:
   - Title and description
   - Select project and category
   - Add tags for organization
   - Write your prompt content with variable placeholders
3. Optionally attach a system prompt or model configuration
4. Save and start using your prompt!

## Key Concepts

### Projects
Projects are top-level containers for organizing related prompts. Each project can have its own categories and color scheme.

### Categories
Categories help organize prompts within a project (e.g., "Data Analysis", "Code Generation", "Content Writing").

### Tags
Tags provide flexible, cross-project organization. Use them to mark prompts by purpose, complexity, or any custom criteria.

### Variables
Use `{{variableName}}` syntax in your prompts to create reusable templates with dynamic values.

### System Prompts
Reusable system-level instructions that define AI behavior. Attach them to prompts for consistent results.

### Model Configurations
Save preferred model settings (temperature, max tokens, top_p) for different use cases.

### MCP Exposure
Enable "Expose to MCP" on individual prompts to make them available to AI agents through the Model Context Protocol.

## Technical Stack

- React + TypeScript
- Tailwind CSS for styling
- shadcn/ui component library
- Framer Motion for animations
- Spark KV for data persistence
- Model Context Protocol for AI agent integration

## Documentation

- [Authentication Setup](./OAUTH_SETUP.md) - Learn about the authentication system
- [Security](./SECURITY.md) - Security policies and practices
- [PRD](./PRD.md) - Product Requirements Document

## License

The Spark Template files and resources from GitHub are licensed under the terms of the MIT license, Copyright GitHub, Inc.
