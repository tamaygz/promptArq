# MCP Server Setup Guide

This guide explains how to connect arqioly to Claude Desktop or other MCP (Model Context Protocol) clients.

## What is MCP?

The Model Context Protocol (MCP) allows AI assistants like Claude to access external tools and data sources. By connecting arqioly through MCP, Claude can directly access and use your prompts.

## Prerequisites

- arqioly application running (this app)
- Claude Desktop app installed
- Prompts marked as "Expose to MCP" in arqioly

## Setup Instructions

### 1. Enable MCP on Your Prompts

1. Open a prompt in arqioly
2. Scroll to the bottom of the editor
3. Toggle "Expose to MCP Server"
4. Save the prompt

Only prompts with MCP exposure enabled will be available to Claude.

### 2. Get Your MCP Configuration

1. Click the "MCP Server" button in the arqioly header
2. Review the list of exposed prompts
3. Click "Copy" on the recommended configuration

### 3. Configure Claude Desktop

#### macOS

1. Open Finder
2. Press `Cmd + Shift + G` to open "Go to Folder"
3. Enter: `~/Library/Application Support/Claude/`
4. Create or edit the file `claude_desktop_config.json`
5. Paste the MCP configuration you copied

#### Windows

1. Open File Explorer
2. Type in the address bar: `%APPDATA%\Claude\`
3. Create or edit the file `claude_desktop_config.json`
4. Paste the MCP configuration you copied

### 4. Configuration Format

Your configuration should look like this:

```json
{
  "mcpServers": {
    "arqioly-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://your-app-url/api/mcp"]
    }
  }
}
```

**Important**: Replace `https://your-app-url/api/mcp` with your actual arqioly application URL.

### 5. Restart Claude Desktop

After saving the configuration file, completely quit and restart Claude Desktop for the changes to take effect.

## Using Your Prompts in Claude

Once configured, your exposed prompts become available as tools in Claude:

1. Start a conversation in Claude Desktop
2. Claude can now access your prompts automatically
3. Reference prompts by name in your conversations
4. Claude will use the prompt content and variables you defined

## Troubleshooting

### "Package not found at registry" Error

**Solution**: Use the recommended configuration format with `npx` and `@modelcontextprotocol/server-fetch`:

```json
{
  "mcpServers": {
    "arqioly-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "YOUR_ARQIOLY_URL/api/mcp"]
    }
  }
}
```

The `-y` flag tells npx to automatically install the package if it's not found.

### Claude Can't See My Prompts

1. Verify prompts are marked "Expose to MCP" in arqioly
2. Check that the prompts are not archived
3. Ensure your MCP endpoint URL is correct
4. Restart Claude Desktop completely
5. Check Claude's MCP connection status in settings

### Configuration File Issues

- Ensure the JSON is valid (no trailing commas, proper quotes)
- The file must be named exactly `claude_desktop_config.json`
- The file should be in the correct directory for your OS
- You may need to create the `Claude` folder if it doesn't exist

### Testing the Connection

1. Open the MCP Server dialog in arqioly
2. Verify your prompts are listed under "Exposed Prompts"
3. Copy the endpoint URL
4. Test the URL in a browser - it should respond (even if with an error about method/headers)

## Advanced Configuration

### Multiple Servers

You can connect multiple MCP servers by adding them to the config:

```json
{
  "mcpServers": {
    "arqioly-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://your-app-url/api/mcp"]
    },
    "other-server": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://other-url/mcp"]
    }
  }
}
```

### Environment Variables

You can pass environment variables to the MCP server:

```json
{
  "mcpServers": {
    "arqioly-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://your-app-url/api/mcp"],
      "env": {
        "CUSTOM_VAR": "value"
      }
    }
  }
}
```

## Security Notes

- Only prompts explicitly marked for MCP exposure are accessible
- Archived prompts are never exposed through MCP
- The MCP endpoint respects user authentication
- Prompts remain private to your account

## Need Help?

If you're still having issues:

1. Check the arqioly logs for any errors
2. Verify your network connection
3. Ensure you're using the latest version of Claude Desktop
4. Review the MCP protocol documentation at https://modelcontextprotocol.io

## Learn More

- [Model Context Protocol Documentation](https://modelcontextprotocol.io)
- [Claude Desktop MCP Guide](https://docs.anthropic.com/claude/docs/model-context-protocol)
