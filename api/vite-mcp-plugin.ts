/**
 * Vite plugin to handle MCP API endpoints
 */
import type { Plugin, ViteDevServer } from 'vite'
import type { IncomingMessage, ServerResponse } from 'http'
import { convertToMCPPrompt, generateMCPPromptResponse } from './mcp'

interface MCPRequest {
  jsonrpc: '2.0'
  id?: string | number
  method: string
  params?: any
}

interface MCPResponse {
  jsonrpc: '2.0'
  id?: string | number
  result?: any
  error?: {
    code: number
    message: string
    data?: any
  }
}

export function mcpApiPlugin(): Plugin {
  return {
    name: 'mcp-api-plugin',
    configureServer(server: ViteDevServer) {
      server.middlewares.use(async (req: IncomingMessage, res: ServerResponse, next) => {
        // Only handle /api/mcp requests
        if (!req.url?.startsWith('/api/mcp')) {
          return next()
        }

        // Set CORS headers
        res.setHeader('Access-Control-Allow-Origin', '*')
        res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS')
        res.setHeader('Access-Control-Allow-Headers', 'Content-Type')

        // Handle OPTIONS preflight
        if (req.method === 'OPTIONS') {
          res.statusCode = 200
          res.end()
          return
        }

        // Handle GET requests with simple HTML response
        if (req.method === 'GET') {
          res.setHeader('Content-Type', 'application/json')
          res.statusCode = 200
          res.end(JSON.stringify({
            name: 'promptArq MCP Server',
            version: '1.0.0',
            description: 'Model Context Protocol server for promptArq prompts',
            capabilities: {
              prompts: {}
            }
          }, null, 2))
          return
        }

        // Handle POST requests for JSON-RPC
        if (req.method === 'POST') {
          let body = ''
          req.on('data', chunk => {
            body += chunk.toString()
          })

          req.on('end', async () => {
            try {
              const mcpRequest: MCPRequest = JSON.parse(body)
              const response = await handleMCPRequest(mcpRequest)
              
              res.setHeader('Content-Type', 'application/json')
              res.statusCode = 200
              res.end(JSON.stringify(response, null, 2))
            } catch (error) {
              const errorResponse: MCPResponse = {
                jsonrpc: '2.0',
                id: null,
                error: {
                  code: -32700,
                  message: 'Parse error',
                  data: error instanceof Error ? error.message : 'Unknown error'
                }
              }
              
              res.setHeader('Content-Type', 'application/json')
              res.statusCode = 400
              res.end(JSON.stringify(errorResponse, null, 2))
            }
          })
          return
        }

        // Unsupported method
        res.statusCode = 405
        res.end('Method not allowed')
      })
    }
  }
}

async function handleMCPRequest(request: MCPRequest): Promise<MCPResponse> {
  const { method, params, id } = request

  switch (method) {
    case 'initialize':
      return {
        jsonrpc: '2.0',
        id,
        result: {
          protocolVersion: '2024-11-05',
          capabilities: {
            prompts: {}
          },
          serverInfo: {
            name: 'promptArq',
            version: '1.0.0'
          }
        }
      }

    case 'prompts/list':
      try {
        const prompts = await getPromptsFromKV()
        const mcpPrompts = prompts
          .filter((p: any) => p.exposedToMCP && !p.isArchived)
          .map((p: any) => convertToMCPPrompt(p))
        
        return {
          jsonrpc: '2.0',
          id,
          result: {
            prompts: mcpPrompts
          }
        }
      } catch (error) {
        return {
          jsonrpc: '2.0',
          id,
          error: {
            code: -32603,
            message: 'Internal error',
            data: error instanceof Error ? error.message : 'Failed to fetch prompts'
          }
        }
      }

    case 'prompts/get':
      if (!params?.name) {
        return {
          jsonrpc: '2.0',
          id,
          error: {
            code: -32602,
            message: 'Invalid params: name is required'
          }
        }
      }

      try {
        const prompts = await getPromptsFromKV()
        const promptId = params.name.replace('prompt-', '')
        const prompt = prompts.find((p: any) => p.id === promptId && p.exposedToMCP && !p.isArchived)
        
        if (!prompt) {
          return {
            jsonrpc: '2.0',
            id,
            error: {
              code: -32602,
              message: 'Prompt not found or not exposed to MCP'
            }
          }
        }

        const response = generateMCPPromptResponse(prompt, params.arguments)
        return {
          jsonrpc: '2.0',
          id,
          result: response
        }
      } catch (error) {
        return {
          jsonrpc: '2.0',
          id,
          error: {
            code: -32603,
            message: 'Internal error',
            data: error instanceof Error ? error.message : 'Failed to get prompt'
          }
        }
      }

    default:
      return {
        jsonrpc: '2.0',
        id,
        error: {
          code: -32601,
          message: `Method not found: ${method}`
        }
      }
  }
}

/**
 * Fetch prompts from Spark KV store
 * Uses the Spark runtime KV service API
 */
async function getPromptsFromKV(): Promise<any[]> {
  try {
    // Check if we're in development mode
    if (process.env.NODE_ENV === 'development') {
      // In development, return demo/mock prompts for testing
      console.log('[MCP] Development mode - using demo prompts')
      return getDemoPrompts()
    }
    
    // In production Spark runtime, fetch from KV service
    // This would be the actual implementation:
    const kvServiceUrl = process.env.BASE_KV_SERVICE_URL || '/api/kv'
    const response = await fetch(`${kvServiceUrl}/prompts`)
    
    if (!response.ok) {
      throw new Error(`KV service returned ${response.status}`)
    }
    
    const data = await response.json()
    return Array.isArray(data) ? data : []
  } catch (error) {
    console.error('[MCP] Error fetching prompts from KV:', error)
    // Fallback to demo prompts on error
    return getDemoPrompts()
  }
}

/**
 * Get demo prompts for development/testing
 */
function getDemoPrompts(): any[] {
  return [
    {
      id: 'demo-1',
      title: 'Code Review Assistant',
      description: 'Helps review code for best practices and potential issues',
      content: 'Please review the following {{language}} code and provide feedback on:\n1. Code quality and best practices\n2. Potential bugs or issues\n3. Performance improvements\n4. Security concerns\n\nCode:\n{{code}}',
      projectId: 'demo-project',
      categoryId: 'demo-category',
      tags: ['code-review', 'development'],
      exposedToMCP: true,
      isArchived: false,
      createdBy: 'demo',
      createdAt: Date.now(),
      updatedAt: Date.now()
    },
    {
      id: 'demo-2',
      title: 'Technical Documentation Writer',
      description: 'Generates technical documentation from code or specifications',
      content: 'Generate comprehensive technical documentation for:\n\nTopic: {{topic}}\nAudience: {{audience}}\nFormat: {{format}}\n\nInclude:\n- Overview and purpose\n- Key features\n- Usage examples\n- API reference (if applicable)\n- Best practices',
      projectId: 'demo-project',
      categoryId: 'demo-category',
      tags: ['documentation', 'technical-writing'],
      exposedToMCP: true,
      isArchived: false,
      createdBy: 'demo',
      createdAt: Date.now(),
      updatedAt: Date.now()
    },
    {
      id: 'demo-3',
      title: 'Bug Report Analyzer',
      description: 'Analyzes bug reports and suggests solutions',
      content: 'Analyze the following bug report and provide:\n1. Root cause analysis\n2. Potential solutions\n3. Steps to reproduce (if missing)\n4. Priority assessment\n\nBug Report:\n{{bug_report}}',
      projectId: 'demo-project',
      categoryId: 'demo-category',
      tags: ['debugging', 'analysis'],
      exposedToMCP: true,
      isArchived: false,
      createdBy: 'demo',
      createdAt: Date.now(),
      updatedAt: Date.now()
    }
  ]
}
