# MCP API Testing Guide

This document provides instructions for testing the MCP (Model Context Protocol) API implementation in promptArq.

## Quick Test

The fastest way to verify the MCP endpoint is working:

```bash
# Test server info
curl http://localhost:5000/api/mcp

# Test prompts list
curl -X POST http://localhost:5000/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"prompts/list"}'
```

## Complete Integration Test

A comprehensive test script is available that verifies all MCP functionality:

```bash
#!/bin/bash
# Save as test-mcp.sh

BASE_URL="http://localhost:5000/api/mcp"

echo "Testing MCP Integration..."

# 1. Server Info
echo "1. GET /api/mcp"
curl -s "$BASE_URL" | jq .

# 2. Initialize
echo -e "\n2. Initialize"
curl -s -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}' | jq .

# 3. List Prompts
echo -e "\n3. List Prompts"
curl -s -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"prompts/list"}' | jq .

# 4. Get Prompt
echo -e "\n4. Get Prompt"
curl -s -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":3,"method":"prompts/get","params":{"name":"prompt-demo-1","arguments":{"language":"Python","code":"def test(): pass"}}}' | jq .
```

## Demo Prompts

In development mode, three demo prompts are available for testing:

### 1. Code Review Assistant (`prompt-demo-1`)
Reviews code for best practices and potential issues.

**Arguments:**
- `language` - Programming language (e.g., "Python", "JavaScript")
- `code` - Code to review

**Example:**
```bash
curl -X POST http://localhost:5000/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "id":1,
    "method":"prompts/get",
    "params":{
      "name":"prompt-demo-1",
      "arguments":{
        "language":"Python",
        "code":"def hello():\n    print(\"hello\")"
      }
    }
  }'
```

### 2. Technical Documentation Writer (`prompt-demo-2`)
Generates technical documentation.

**Arguments:**
- `topic` - Documentation topic
- `audience` - Target audience
- `format` - Documentation format

**Example:**
```bash
curl -X POST http://localhost:5000/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "id":2,
    "method":"prompts/get",
    "params":{
      "name":"prompt-demo-2",
      "arguments":{
        "topic":"REST API",
        "audience":"developers",
        "format":"markdown"
      }
    }
  }'
```

### 3. Bug Report Analyzer (`prompt-demo-3`)
Analyzes bug reports and suggests solutions.

**Arguments:**
- `bug_report` - Bug report content

**Example:**
```bash
curl -X POST http://localhost:5000/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "id":3,
    "method":"prompts/get",
    "params":{
      "name":"prompt-demo-3",
      "arguments":{
        "bug_report":"App crashes when clicking submit button"
      }
    }
  }'
```

## Testing with Claude Desktop

To test with Claude Desktop:

1. **Create the config file:**
   - macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - Windows: `%APPDATA%\Claude\claude_desktop_config.json`

2. **Add this configuration:**
   ```json
   {
     "mcpServers": {
       "promptarq-local": {
         "command": "npx",
         "args": [
           "-y",
           "@modelcontextprotocol/server-fetch",
           "http://localhost:5000/api/mcp"
         ]
       }
     }
   }
   ```

3. **Restart Claude Desktop**

4. **Verify in Claude:**
   - The prompts should appear as available tools
   - Try using them in a conversation

## Expected Responses

### GET /api/mcp
```json
{
  "name": "promptArq MCP Server",
  "version": "1.0.0",
  "description": "Model Context Protocol server for promptArq prompts",
  "capabilities": {
    "prompts": {}
  }
}
```

### initialize
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

### prompts/list
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "prompts": [
      {
        "name": "prompt-demo-1",
        "description": "Helps review code for best practices and potential issues",
        "arguments": [
          {
            "name": "language",
            "description": "Value for language",
            "required": false
          },
          {
            "name": "code",
            "description": "Value for code",
            "required": false
          }
        ]
      }
      // ... more prompts
    ]
  }
}
```

### prompts/get
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "description": "Helps review code for best practices and potential issues",
    "messages": [
      {
        "role": "user",
        "content": {
          "type": "text",
          "text": "Please review the following Python code and provide feedback on:\n1. Code quality and best practices\n2. Potential bugs or issues\n3. Performance improvements\n4. Security concerns\n\nCode:\ndef hello():\n    print(\"hello\")"
        }
      }
    ]
  }
}
```

## Troubleshooting

### Port Already in Use
If port 5000 is in use, check `vite.config.ts` or set a different port:
```bash
PORT=3000 npm run dev
```

### CORS Errors
The MCP endpoint includes proper CORS headers. If you see CORS errors:
- They're expected when accessing directly from a browser
- The `@modelcontextprotocol/server-fetch` package handles CORS correctly
- Use curl or the MCP client for testing

### No Prompts Returned
In development mode, demo prompts should always be returned. If you see an empty list:
- Check the server logs for errors
- Verify the endpoint is responding: `curl http://localhost:5000/api/mcp`
- Restart the dev server

## Production Deployment

When deployed to GitHub Spark production:
- The endpoint will automatically fetch from the Spark KV store
- Only prompts marked "Expose to MCP" will be available
- Replace `localhost:5000` with your production URL
- Demo prompts won't be shown in production

## Reference

- MCP Specification: https://modelcontextprotocol.io
- Claude Desktop Config: https://docs.anthropic.com/claude/docs/model-context-protocol
- Server Fetch Package: https://www.npmjs.com/package/@modelcontextprotocol/server-fetch
