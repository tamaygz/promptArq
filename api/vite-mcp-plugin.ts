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
 * In production Spark environment, this would use the BASE_KV_SERVICE_URL
 * For development, we need to handle this differently
 */
async function getPromptsFromKV(): Promise<any[]> {
  // In a real Spark deployment, the KV service would be available
  // For now, return empty array during development
  // The actual implementation would use fetch to the KV service endpoint
  
  // Example of what the real implementation would look like:
  // const kvServiceUrl = process.env.BASE_KV_SERVICE_URL || 'http://localhost:8080/kv'
  // const response = await fetch(`${kvServiceUrl}/prompts`)
  // return await response.json()
  
  return []
}
