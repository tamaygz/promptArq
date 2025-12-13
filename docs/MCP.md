# MCP Integration Guide

## What is MCP?

The Model Context Protocol (MCP) allows AI assistants like Claude to access external tools and data sources. By connecting promptArq through MCP, Claude can directly access and use your prompts.

## Quick Setup

### 1. Enable MCP on Your Prompts

1. Open a prompt in promptArq
2. Scroll to the bottom of the editor
3. Toggle "Expose to MCP Server"
4. Save the prompt

Only prompts with MCP exposure enabled will be available to Claude.

### 2. Get Your MCP Configuration

1. Click the "MCP Server" button in the promptArq header (CPU icon)
2. Review the list of exposed prompts
3. Click "Copy Configuration"
4. Paste into Claude Desktop configuration

### 3. Configure Claude Desktop

#### macOS
```bash
# Configuration file location
~/Library/Application Support/Claude/claude_desktop_config.json
```

#### Windows
```bash
# Configuration file location
%APPDATA%\Claude\claude_desktop_config.json
```

**Create or edit this file and paste the configuration.**

### 4. Restart Claude Desktop

Completely quit and restart Claude Desktop for changes to take effect.

## Configuration Format

Your configuration should look like this:

```json
{
  "mcpServers": {
    "promptarq-prompts": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-fetch",
        "https://your-promptarq-url/api/mcp"
      ]
    }
  }
}
```

**Important:** Replace the URL with your actual promptarq application URL:
- **Spark deployment:** `https://arqioly-prompt-atom--tamaygz.github.app/api/mcp`
- **Local development:** `http://localhost:5173/api/mcp`

## Using Your Prompts in Claude

Once configured, your exposed prompts become available in Claude:

1. Open Claude Desktop
2. Claude can now access your prompts automatically
3. Reference prompts by name in conversations
4. Claude will use the prompt content you defined

## Troubleshooting

### Claude Can't See Prompts

**Check:**
- ✓ Prompts marked "Expose to MCP" in promptArq
- ✓ Prompts are not archived
- ✓ MCP endpoint URL is correct
- ✓ Claude Desktop completely restarted
- ✓ JSON configuration is valid (no trailing commas)

### "Package not found" Error

**Solution:** Use recommended format with `-y` flag:
```json
"args": ["-y", "@modelcontextprotocol/server-fetch", "URL"]
```

The `-y` flag tells npx to automatically install the package.

### Configuration File Issues

- File must be named exactly `claude_desktop_config.json`
- File in correct directory for your OS
- JSON must be valid (use JSON validator)
- Create `Claude` folder if it doesn't exist

### Testing Connection

1. Open MCP Server dialog in promptArq
2. Verify prompts listed under "Exposed Prompts"
3. Copy endpoint URL
4. Test URL in browser (should respond, even if error)

## Advanced Configuration

### Multiple MCP Servers

```json
{
  "mcpServers": {
    "promptarq-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://url1/api/mcp"]
    },
    "other-source": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://url2/mcp"]
    }
  }
}
```

### Environment Variables

```json
{
  "mcpServers": {
    "promptarq-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://url/api/mcp"],
      "env": {
        "CUSTOM_VAR": "value"
      }
    }
  }
}
```

## Security

- Only prompts explicitly marked for MCP are accessible
- Archived prompts never exposed
- MCP endpoint respects authentication
- Prompts remain private to your account

## MCP Endpoints

The MCP server provides these capabilities:

**List Prompts:**
- Returns all exposed prompts
- Filtered by user (if authenticated)
- Excludes archived prompts

**Get Prompt:**
- Fetch specific prompt by ID
- Returns full content and metadata
- Respects MCP exposure flag

**Search Prompts:**
- Search exposed prompts
- Filter by tags, project, category
- Full-text search

## Learn More

- [Model Context Protocol Docs](https://modelcontextprotocol.io)
- [Claude Desktop MCP Guide](https://docs.anthropic.com/claude/docs/model-context-protocol)
- [promptArq Architecture](./Architecture.md)
- [User Guide](./UserGuide.md)
