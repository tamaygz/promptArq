/**
 * Windows App API
 * 
 * This module exposes a clean API for the Windows desktop app to interact with the web app.
 * All business logic stays in the web app - the Windows app is just a thin client.
 * 
 * COMMUNICATION PATTERNS:
 * 
 * 1. Synchronous Methods (getPrompts, getPlaceholders, fillContent):
 *    - Return values directly from JavaScript
 *    - C# calls ExecuteScriptAsync and gets return value
 *    - Used for fast, in-memory operations
 * 
 * 2. Async Methods (executePrompt):
 *    - Cannot use ExecuteScriptAsync return value (returns Promise object as {})
 *    - Uses WebView2 message passing: window.chrome.webview.postMessage()
 *    - C# receives result via WebMessageReceived event handler
 *    - Used for operations requiring async work (HTTP requests, LLM calls)
 */

import type { Prompt, Project, Category, Tag, SystemPrompt } from './types'
import { resolveSystemPrompt } from './prompt-resolver'
import { createLLMPrompt, executeLLM, hasLLMSupport } from './spark-utils'
import { replaceProjectVariables } from './placeholder-utils'

export interface PromptMetadata {
  id: string
  title: string
  description: string
  content: string
  projectId: string
  projectName: string
  categoryId: string
  categoryName: string
  tags: string[]
  isArchived: boolean
  hasPlaceholders: boolean
  placeholders: string[]
  executeLLM: boolean
}

/**
 * Execution result format used in message passing
 * 
 * This is NOT a return type but the message format sent via:
 * window.chrome.webview.postMessage({ type: 'executeResult', ...ExecutionResult })
 */
export interface ExecutionResult {
  success: boolean
  result?: string
  error?: string
}

/**
 * Extract placeholder names from prompt content (excluding project variables)
 */
function extractPlaceholders(content: string): string[] {
  const regex = /\{\{([^}]+)\}\}/g
  const matches = content.matchAll(regex)
  const placeholders = Array.from(matches, m => m[1].trim())
  return Array.from(new Set(placeholders)) // Remove duplicates
}

/**
 * Fill placeholders in content with provided values
 * This is a helper function used by fillContent() to handle the complete replacement workflow
 * @param content - The prompt content with placeholders and project variables
 * @param values - User-provided values for manual placeholders
 * @param projectVariables - Project-level variables to auto-replace (handled first)
 */
function fillPlaceholders(content: string, values: Record<string, string>, projectVariables?: Record<string, string>): string {
  // Step 1: Replace project variables {{{var}}} first (automatic replacement)
  let filled = replaceProjectVariables(content, projectVariables || {})
  
  // Step 2: Replace user placeholders {{placeholder}} (manual values provided)
  for (const [key, value] of Object.entries(values)) {
    const regex = new RegExp(`\\{\\{\\s*${key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\s*\\}\\}`, 'gi')
    filled = filled.replace(regex, value)
  }
  return filled
}

/**
 * Initialize the Windows App API
 * Called from App.tsx to provide access to application state
 */
export function initWindowsAppAPI(
  prompts: Prompt[],
  projects: Project[],
  categories: Category[],
  tags: Tag[],
  systemPrompts: SystemPrompt[],
  dataLoaded: boolean = false
) {
  const api = {
    /**
     * Get prompts with optional search query
     * Returns metadata only (no sensitive data)
     * SYNCHRONOUS - all data is already in memory
     */
    getPrompts(searchQuery?: string): PromptMetadata[] {
      console.log(`[WindowsAPI] getPrompts called:`, {
        totalPrompts: prompts?.length ?? 0,
        dataLoaded,
        isArray: Array.isArray(prompts),
        searchQuery
      })
      
      // Handle case where prompts is undefined (should never happen after initialization)
      if (!prompts) {
        console.warn('[WindowsAPI] ⚠️ prompts is undefined - API may not be initialized yet')
        return []
      }

      // If data hasn't loaded yet and prompts is empty, indicate loading state
      if (!dataLoaded && prompts.length === 0) {
        console.log('[WindowsAPI] 🔄 Data still loading from storage, returning empty array (will retry)')
        return []
      }
      
      let filtered = prompts.filter(p => !p.isArchived)
      console.log(`[WindowsAPI] ✅ Returning ${filtered.length} non-archived prompts from ${prompts.length} total`)

      if (searchQuery && searchQuery.trim()) {
        const query = searchQuery.toLowerCase()
        filtered = filtered.filter(p =>
          p.title.toLowerCase().includes(query) ||
          p.description.toLowerCase().includes(query) ||
          p.content.toLowerCase().includes(query) ||
          p.tags.some(t => t.toLowerCase().includes(query))
        )
      }

      return filtered.map(p => {
        const project = projects.find(proj => proj.id === p.projectId)
        const category = categories.find(cat => cat.id === p.categoryId)
        // Replace project variables in content before extracting placeholders
        const contentWithProjectVars = replaceProjectVariables(p.content, project?.variables || {})
        const placeholders = extractPlaceholders(contentWithProjectVars)

        return {
          id: p.id,
          title: p.title,
          description: p.description,
          content: contentWithProjectVars,
          projectId: p.projectId,
          projectName: project?.name || '',
          categoryId: p.categoryId,
          categoryName: category?.name || '',
          tags: p.tags,
          isArchived: p.isArchived,
          hasPlaceholders: placeholders.length > 0,
          placeholders: placeholders,
          executeLLM: p.execute_llm || false
        }
      })
    },

    /**
     * Get a single prompt by ID
     * SYNCHRONOUS - all data is already in memory
     */
    getPrompt(promptId: string): PromptMetadata | null {
      const prompt = prompts.find(p => p.id === promptId)
      if (!prompt) return null

      const project = projects.find(proj => proj.id === prompt.projectId)
      const category = categories.find(cat => cat.id === prompt.categoryId)
      // Replace project variables in content before extracting placeholders
      const contentWithProjectVars = replaceProjectVariables(prompt.content, project?.variables || {})
      const placeholders = extractPlaceholders(contentWithProjectVars)

      return {
        id: prompt.id,
        title: prompt.title,
        description: prompt.description,
        content: contentWithProjectVars,
        projectId: prompt.projectId,
        projectName: project?.name || '',
        categoryId: prompt.categoryId,
        categoryName: category?.name || '',
        tags: prompt.tags,
        isArchived: prompt.isArchived,
        hasPlaceholders: placeholders.length > 0,
        placeholders: placeholders,
        executeLLM: prompt.execute_llm || false
      }
    },

    /**
     * Get placeholders for a prompt
     * 
     * SYNCHRONOUS - Returns via ExecuteScriptAsync return value
     * All data is already in memory (regex extraction only)
     * Note: Returns only user placeholders, project variables are auto-replaced
     */
    getPlaceholders(promptId: string): string[] {
      const prompt = prompts.find(p => p.id === promptId)
      if (!prompt) return []
      const project = projects.find(p => p.id === prompt.projectId)
      // Replace project variables first, then extract remaining placeholders
      const contentWithProjectVars = replaceProjectVariables(prompt.content, project?.variables || {})
      return extractPlaceholders(contentWithProjectVars)
    },

    /**
     * Fill placeholders in a prompt with provided values
     * 
     * SYNCHRONOUS - Returns via ExecuteScriptAsync return value
     * All data is already in memory (string replacement only)
     * Note: Project variables are replaced automatically before user placeholders
     */
    fillContent(promptId: string, values: Record<string, string>): string | null {
      const prompt = prompts.find(p => p.id === promptId)
      if (!prompt) return null
      const project = projects.find(p => p.id === prompt.projectId)
      return fillPlaceholders(prompt.content, values, project?.variables)
    },

    /**
     * Execute a prompt (either direct or through LLM)
     * 
     * ASYNC - Uses message passing via window.chrome.webview.postMessage()
     * Cannot use return value because ExecuteScriptAsync doesn't await Promises
     * If content is provided, uses that instead of fetching from prompt
     * 
     * NOTE: For Windows app, this uses message passing instead of return value
     * because ExecuteScriptAsync cannot handle async functions
     */
    executePrompt(promptId: string, content?: string): void {
      // Start async execution and post result via message passing
      (async () => {
        try {
          const prompt = prompts.find(p => p.id === promptId)
          if (!prompt) {
            window.chrome?.webview?.postMessage({
              type: 'executeResult',
              success: false,
              error: 'Prompt not found'
            })
            return
          }

          const project = projects.find(p => p.id === prompt.projectId)
          
          // Replace project variables in content before any processing
          let finalContent = content || prompt.content
          finalContent = replaceProjectVariables(finalContent, project?.variables || {})

          // If execute_llm is false, return content directly (no LLM processing)
          if (!prompt.execute_llm) {
            window.chrome?.webview?.postMessage({
              type: 'executeResult',
              success: true,
              result: finalContent
            })
            return
          }

          // LLM execution path
          // Check if LLM support is available
          if (!hasLLMSupport()) {
            window.chrome?.webview?.postMessage({
              type: 'executeResult',
              success: false,
              error: 'AI features require either Spark environment or GitHub authentication'
            })
            return
          }

          // Resolve system prompt based on hierarchy (prompt → project → category → tag → team)
          const category = categories.find(c => c.id === prompt.categoryId)
          const promptTags = tags.filter(t => prompt.tags.includes(t.id))

          const systemPromptText = resolveSystemPrompt(
            prompt,
            project,
            category,
            promptTags,
            systemPrompts
          )

          // Create execution prompt with system prompt if available
          const executionPrompt = systemPromptText
            ? createLLMPrompt`${systemPromptText}

${finalContent}`
            : createLLMPrompt`${finalContent}`

          // Execute via LLM (this handles both Spark and GitHub Models)
          const result = await executeLLM(executionPrompt, 'gpt-4o-mini', false)

          if (!result) {
            window.chrome?.webview?.postMessage({
              type: 'executeResult',
              success: false,
              error: 'No response from AI service'
            })
            return
          }

          window.chrome?.webview?.postMessage({
            type: 'executeResult',
            success: true,
            result: result.trim()
          })
        } catch (error) {
          console.error('LLM execution error:', error)
          window.chrome?.webview?.postMessage({
            type: 'executeResult',
            success: false,
            error: error instanceof Error ? error.message : 'Failed to execute prompt'
          })
        }
      })()
    }
  }

  // Expose API to window object for Windows app to access
  ;(window as any).windowsAppAPI = api

  const loadStatus = dataLoaded ? '✅ LOADED' : '🔄 LOADING'
  console.log(`[WindowsAPI] ${loadStatus} - API initialized with`, prompts.length, 'prompts,', projects.length, 'projects,', systemPrompts.length, 'system prompts')
}

// Initialize with empty data immediately to ensure API exists even if React hasn't rendered
// This prevents errors if Windows app tries to call API before React initialization
if (typeof window !== 'undefined') {
  initWindowsAppAPI([], [], [], [], [])
  console.log('[WindowsAPI] 🔄 Pre-initialized with empty data (will be replaced by React)')
}
