/**
 * MCP (Model Context Protocol) API endpoint
 * This module provides MCP-compliant JSON-RPC endpoints for exposing prompts
 */

export interface MCPPrompt {
  name: string
  description?: string
  arguments?: Array<{
    name: string
    description?: string
    required?: boolean
  }>
}

export interface MCPMessage {
  role: 'user' | 'assistant'
  content: {
    type: 'text' | 'resource'
    text?: string
    resource?: {
      uri: string
      text: string
      mimeType: string
    }
  }
}

export interface MCPPromptResponse {
  description?: string
  messages: MCPMessage[]
}

export interface MCPListPromptsResponse {
  prompts: MCPPrompt[]
}

export interface MCPGetPromptRequest {
  name: string
  arguments?: Record<string, string>
}

/**
 * Convert a Prompt to MCP format
 */
export function convertToMCPPrompt(prompt: any): MCPPrompt {
  // Extract placeholder names from content
  const placeholders = extractPlaceholders(prompt.content)
  
  return {
    name: `prompt-${prompt.id}`,
    description: prompt.description || prompt.title,
    arguments: placeholders.map(placeholder => ({
      name: placeholder,
      description: `Value for ${placeholder}`,
      required: false
    }))
  }
}

/**
 * Extract placeholder names from prompt content
 */
function extractPlaceholders(content: string): string[] {
  const regex = /\{\{([^}]+)\}\}/g
  const matches: string[] = []
  let match

  while ((match = regex.exec(content)) !== null) {
    const placeholder = match[1].trim()
    if (!matches.includes(placeholder)) {
      matches.push(placeholder)
    }
  }

  return matches
}

/**
 * Replace placeholders in prompt content with provided arguments
 */
export function replacePlaceholders(content: string, args?: Record<string, string>): string {
  if (!args) return content
  
  let result = content
  Object.entries(args).forEach(([key, value]) => {
    const regex = new RegExp(`\\{\\{\\s*${key}\\s*\\}\\}`, 'g')
    result = result.replace(regex, value)
  })
  
  return result
}

/**
 * Generate MCP prompt response
 */
export function generateMCPPromptResponse(prompt: any, args?: Record<string, string>): MCPPromptResponse {
  const content = replacePlaceholders(prompt.content, args)
  
  return {
    description: prompt.description || prompt.title,
    messages: [
      {
        role: 'user',
        content: {
          type: 'text',
          text: content
        }
      }
    ]
  }
}
