/**
 * Windows App API
 * 
 * This module exposes a clean API for the Windows desktop app to interact with the web app.
 * All business logic stays in the web app - the Windows app is just a thin client.
 */

import type { Prompt, Project, Category, Tag, SystemPrompt } from './types'
import { resolveSystemPrompt } from './prompt-resolver'
import { createLLMPrompt, executeLLM, hasLLMSupport } from './spark-utils'

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

export interface ExecutionResult {
  success: boolean
  result?: string
  error?: string
}

/**
 * Extract placeholder names from prompt content
 */
function extractPlaceholders(content: string): string[] {
  const regex = /\{\{([^}]+)\}\}/g
  const matches = content.matchAll(regex)
  const placeholders = Array.from(matches, m => m[1].trim())
  return Array.from(new Set(placeholders)) // Remove duplicates
}

/**
 * Fill placeholders in content with provided values
 */
function fillPlaceholders(content: string, values: Record<string, string>): string {
  let filled = content
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
        const content = p.content
        const placeholders = extractPlaceholders(content)

        return {
          id: p.id,
          title: p.title,
          description: p.description,
          content: content,
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
      const placeholders = extractPlaceholders(prompt.content)

      return {
        id: prompt.id,
        title: prompt.title,
        description: prompt.description,
        content: prompt.content,
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
     * SYNCHRONOUS - all data is already in memory
     */
    getPlaceholders(promptId: string): string[] {
      const prompt = prompts.find(p => p.id === promptId)
      if (!prompt) return []
      return extractPlaceholders(prompt.content)
    },

    /**
     * Fill placeholders in a prompt with provided values
     * SYNCHRONOUS - all data is already in memory
     */
    fillContent(promptId: string, values: Record<string, string>): string | null {
      const prompt = prompts.find(p => p.id === promptId)
      if (!prompt) return null
      return fillPlaceholders(prompt.content, values)
    },

    /**
     * Execute a prompt (either direct or through LLM)
     * If content is provided, uses that instead of fetching from prompt
     */
    async executePrompt(promptId: string, content?: string): Promise<ExecutionResult> {
      const prompt = prompts.find(p => p.id === promptId)
      if (!prompt) {
        return { success: false, error: 'Prompt not found' }
      }

      const finalContent = content || prompt.content

      // If execute_llm is false, return content directly (no LLM processing)
      if (!prompt.execute_llm) {
        return { success: true, result: finalContent }
      }

      // LLM execution path
      try {
        // Check if LLM support is available
        if (!hasLLMSupport()) {
          return {
            success: false,
            error: 'AI features require either Spark environment or GitHub authentication'
          }
        }

        // Resolve system prompt based on hierarchy (prompt → project → category → tag → team)
        const project = projects.find(p => p.id === prompt.projectId)
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
          return {
            success: false,
            error: 'No response from AI service'
          }
        }

        return {
          success: true,
          result: result.trim()
        }
      } catch (error) {
        console.error('LLM execution error:', error)
        return {
          success: false,
          error: error instanceof Error ? error.message : 'Failed to execute prompt'
        }
      }
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
