# MCP Server Setup Guide

This guide explains how to connect promptarq to Claude Desktop or other MCP (Model Context Protocol) clients.

## What is MCP?

The Model Context Protocol (MCP) allows AI assistants like Claude to access external tools and data sources. By connecting promptarq through MCP, Claude can directly access and use your prompts.

## Prerequisites

- promptarq application running (this app)
- Claude Desktop app installed
- Prompts marked as "Expose to MCP" in promptarq

## Setup Instructions

### 1. Enable MCP on Your Prompts

1. Open a prompt in promptarq
2. Scroll to the bottom of the editor
3. Toggle "Expose to MCP Server"
4. Save the prompt

Only prompts with MCP exposure enabled will be available to Claude.

### 2. Get Your MCP Configuration

1. Click the "MCP Server" button in the promptarq header
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
    "promptarq-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "https://your-app-url/api/mcp"]
    }
  }
}
```

**Important**: Replace `https://your-app-url/api/mcp` with your actual promptarq application URL.

**For Local Development**: If you're running promptarq locally, use `http://localhost:5000/api/mcp`

### 5. Restart Claude Desktop

After saving the configuration file, completely quit and restart Claude Desktop for the changes to take effect.

## How It Works

The promptArq MCP server exposes your prompts through a JSON-RPC 2.0 API that follows the Model Context Protocol specification:

- **GET /api/mcp**: Returns server information and capabilities
- **POST /api/mcp**: Handles JSON-RPC requests for:
  - `initialize`: Establishes connection and capabilities
  - `prompts/list`: Returns all exposed (non-archived) prompts
  - `prompts/get`: Retrieves a specific prompt with placeholder substitution

### Prompt Format

Prompts are automatically converted to MCP format:
- Prompt title → MCP prompt name
- Prompt description → MCP prompt description  
- Placeholders `{{variableName}}` → MCP arguments
- Prompt content → MCP message content

### Example

A prompt with content:
```
Review this {{language}} code:
{{code}}
```

Becomes an MCP prompt with arguments:
- `language` (optional)
- `code` (optional)

## Using Your Prompts in Claude

Once configured, your exposed prompts become available as tools in Claude:

1. Start a conversation in Claude Desktop
2. Claude can now access your prompts automatically
3. Reference prompts by name in your conversations
4. Claude will use the prompt content and variables you defined

## Development & Testing

### Local Testing

When running in development mode (npm run dev), the MCP endpoint automatically serves demo prompts for testing:

1. Code Review Assistant
2. Technical Documentation Writer
3. Bug Report Analyzer

Test the endpoint:
```bash
# List prompts
curl -X POST http://localhost:5000/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"prompts/list"}'

# Get specific prompt
curl -X POST http://localhost:5000/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"prompts/get","params":{"name":"prompt-demo-1","arguments":{"language":"Python","code":"print(123)"}}}'
```

### Production Deployment

When deployed to GitHub Spark production environment, the MCP endpoint will automatically fetch your actual prompts from the Spark KV store.

## Troubleshooting

### "Package not found at registry" Error

**Solution**: Use the recommended configuration format with `npx` and `@modelcontextprotocol/server-fetch`:

```json
{
  "mcpServers": {
    "promptarq-prompts": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-fetch", "YOUR_PROMPTARQ_URL/api/mcp"]
    }
  }
}
```

The `-y` flag tells npx to automatically install the package if it's not found.

### Claude Can't See My Prompts

1. Verify prompts are marked "Expose to MCP" in promptarq
2. Check that the prompts are not archived
3. Ensure your MCP endpoint URL is correct
4. Restart Claude Desktop completely
5. Check Claude's MCP connection status in settings
6. Test the endpoint directly using curl (see examples above)

### Configuration File Issues

- Ensure the JSON is valid (no trailing commas, proper quotes)
- The file must be named exactly `claude_desktop_config.json`
- The file should be in the correct directory for your OS
- You may need to create the `Claude` folder if it doesn't exist

### Testing the Connection

1. Open the MCP Server dialog in promptarq
2. Verify your prompts are listed under "Exposed Prompts"
3. Copy the endpoint URL
4. Test the URL in a browser or with curl:
   ```bash
   curl http://your-app-url/api/mcp
   ```
   Should return server info in JSON format

### CORS Issues

If you encounter CORS errors when testing locally:
- The MCP endpoint includes proper CORS headers
- Claude Desktop uses the fetch-server which handles cross-origin requests
- Direct browser access may show CORS warnings but the MCP client will work

## Advanced Configuration

### Multiple Servers

You can connect multiple MCP servers by adding them to the config:

```json
{
  "mcpServers": {
    "promptarq-prompts": {
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
    "promptarq-prompts": {
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
- The MCP endpoint is read-only (cannot modify prompts)
- Prompts remain private to your account
- The endpoint uses JSON-RPC 2.0 for secure communication

## Technical Details

### MCP Protocol Implementation

The promptArq MCP server implements the Model Context Protocol specification:

- **Protocol Version**: 2024-11-05
- **Capabilities**: Prompts
- **Transport**: HTTP with JSON-RPC 2.0
- **Endpoint**: `/api/mcp`

### API Methods

#### initialize
Establishes connection and negotiates capabilities.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {}
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": {
      "prompts": {}
    },
    "serverInfo": {
      "name": "promptArq",
      "version": "1.0.0"
    }
  }
}
```

#### prompts/list
Returns all exposed prompts.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "prompts/list",
  "params": {}
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "prompts": [
      {
        "name": "prompt-abc123",
        "description": "Prompt description",
        "arguments": [
          {
            "name": "variable_name",
            "description": "Value for variable_name",
            "required": false
          }
        ]
      }
    ]
  }
}
```

#### prompts/get
Retrieves a specific prompt with arguments filled in.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "prompts/get",
  "params": {
    "name": "prompt-abc123",
    "arguments": {
      "variable_name": "value"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "description": "Prompt description",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Prompt content with variables replaced..."
        }
      }
    ]
  }
}
```

## Need Help?

If you're still having issues:

1. Check the promptArq logs for any errors
2. Verify your network connection
3. Ensure you're using the latest version of Claude Desktop
4. Test the MCP endpoint directly with curl
5. Review the MCP protocol documentation at https://modelcontextprotocol.io

## Learn More

- [Model Context Protocol Documentation](https://modelcontextprotocol.io)
- [Claude Desktop MCP Guide](https://docs.anthropic.com/claude/docs/model-context-protocol)
- [MCP Server Fetch Package](https://www.npmjs.com/package/@modelcontextprotocol/server-fetch)
