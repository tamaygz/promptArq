/**
 * GitHub Models API Client
 * 
 * Provides integration with GitHub Models API for LLM functionality
 * when not running in Spark environment.
 * 
 * Features:
 * - Token validation and refresh
 * - Rate limiting with request queuing
 * - Usage tracking
 * - Error handling with retry logic
 */

import { getCurrentUser, refreshGitHubToken, getAccessToken } from './github-auth'
import { logTokenUsage } from './token-usage-logger'

export interface GitHubModelsConfig {
  model: string
  temperature: number
  maxTokens: number
}

export interface GitHubModelsResponse {
  id: string
  choices: Array<{
    message: {
      role: string
      content: string
    }
    finish_reason: string
  }>
  usage: {
    prompt_tokens: number
    completion_tokens: number
    total_tokens: number
  }
}

export interface ModelInfo {
  name: string
  displayName: string
  publisher: string
  available: boolean
}

// GitHub Models API endpoint
const GITHUB_MODELS_API = 'https://models.inference.ai.azure.com/chat/completions'

// Rate limiting configuration
const RATE_LIMIT = {
  maxRequestsPerMinute: 50,
  maxRequestsPerHour: 500,
  retryAfterMs: 60000 // 1 minute default
}

// Request queue for rate limiting
interface QueuedRequest {
  resolve: (value: string) => void
  reject: (error: any) => void
  prompt: string
  config: GitHubModelsConfig
  systemPrompt?: string
  retryCount: number
}

let requestQueue: QueuedRequest[] = []
let isProcessingQueue = false
let requestTimestamps: number[] = []

/**
 * Check if GitHub Models API is available
 * Requires valid GitHub token (from OAuth or environment variable)
 */
export function hasGitHubModelsSupport(): boolean {
  // Check if we have a token from any source
  const token = getAccessToken()
  return !!token
}

/**
 * Initialize user data when using environment variable token
 * This should be called on app startup
 */
export async function initializeEnvToken(): Promise<void> {
  const { isUsingEnvToken, fetchUserWithEnvToken } = await import('./github-auth')
  
  if (isUsingEnvToken()) {
    await fetchUserWithEnvToken()
  }
}

/**
 * Get available models from GitHub Models API
 */
export async function getAvailableModels(): Promise<ModelInfo[]> {
  // These are the known GitHub Models as of Dec 2024
  // In production, this could be fetched from an API endpoint
  return [
    { name: 'gpt-4o', displayName: 'GPT-4o', publisher: 'OpenAI', available: true },
    { name: 'gpt-4o-mini', displayName: 'GPT-4o Mini', publisher: 'OpenAI', available: true },
    { name: 'gpt-4-turbo', displayName: 'GPT-4 Turbo', publisher: 'OpenAI', available: true },
    { name: 'gpt-3.5-turbo', displayName: 'GPT-3.5 Turbo', publisher: 'OpenAI', available: true },
    { name: 'o1-preview', displayName: 'o1 Preview', publisher: 'OpenAI', available: true },
    { name: 'o1-mini', displayName: 'o1 Mini', publisher: 'OpenAI', available: true },
    { name: 'Phi-3-medium-128k-instruct', displayName: 'Phi-3 Medium', publisher: 'Microsoft', available: true },
    { name: 'Phi-3-mini-128k-instruct', displayName: 'Phi-3 Mini', publisher: 'Microsoft', available: true },
    { name: 'Mistral-large', displayName: 'Mistral Large', publisher: 'Mistral AI', available: true },
    { name: 'Mistral-small', displayName: 'Mistral Small', publisher: 'Mistral AI', available: true },
    { name: 'Mistral-Nemo', displayName: 'Mistral Nemo', publisher: 'Mistral AI', available: true }
  ]
}

/**
 * Validate if a model is available in GitHub Models
 */
export async function isModelAvailable(modelName: string): Promise<boolean> {
  const models = await getAvailableModels()
  return models.some(m => m.name === modelName && m.available)
}

/**
 * Check current rate limit status
 */
function checkRateLimit(): { allowed: boolean; retryAfter?: number } {
  const now = Date.now()
  
  // Clean up old timestamps (older than 1 hour)
  requestTimestamps = requestTimestamps.filter(ts => now - ts < 3600000)
  
  // Check per-minute limit
  const recentRequests = requestTimestamps.filter(ts => now - ts < 60000)
  if (recentRequests.length >= RATE_LIMIT.maxRequestsPerMinute) {
    const oldestRecent = Math.min(...recentRequests)
    const retryAfter = 60000 - (now - oldestRecent)
    return { allowed: false, retryAfter }
  }
  
  // Check per-hour limit
  if (requestTimestamps.length >= RATE_LIMIT.maxRequestsPerHour) {
    const oldestRequest = Math.min(...requestTimestamps)
    const retryAfter = 3600000 - (now - oldestRequest)
    return { allowed: false, retryAfter }
  }
  
  return { allowed: true }
}

/**
 * Record a request timestamp for rate limiting
 */
function recordRequest() {
  requestTimestamps.push(Date.now())
}

/**
 * Process the request queue
 */
async function processQueue() {
  if (isProcessingQueue || requestQueue.length === 0) {
    return
  }
  
  isProcessingQueue = true
  
  while (requestQueue.length > 0) {
    const rateLimit = checkRateLimit()
    
    if (!rateLimit.allowed) {
      // Wait before processing next request
      await new Promise(resolve => setTimeout(resolve, rateLimit.retryAfter || 1000))
      continue
    }
    
    const request = requestQueue.shift()
    if (!request) continue
    
    try {
      const result = await executeGitHubModelsLLMDirect(
        request.prompt,
        request.config,
        request.systemPrompt
      )
      request.resolve(result)
    } catch (error: any) {
      if (error.status === 429 && request.retryCount < 3) {
        // Re-queue with incremented retry count
        requestQueue.push({
          ...request,
          retryCount: request.retryCount + 1
        })
        
        // Wait before retrying (exponential backoff)
        const backoffMs = Math.min(1000 * Math.pow(2, request.retryCount), 30000)
        await new Promise(resolve => setTimeout(resolve, backoffMs))
      } else {
        request.reject(error)
      }
    }
  }
  
  isProcessingQueue = false
}

/**
 * Execute LLM request via GitHub Models API with rate limiting
 */
export async function executeGitHubModelsLLM(
  prompt: string,
  config: GitHubModelsConfig,
  systemPrompt?: string
): Promise<string> {
  return new Promise((resolve, reject) => {
    requestQueue.push({
      resolve,
      reject,
      prompt,
      config,
      systemPrompt,
      retryCount: 0
    })
    
    processQueue()
  })
}

/**
 * Execute LLM request directly (called by queue processor)
 */
async function executeGitHubModelsLLMDirect(
  prompt: string,
  config: GitHubModelsConfig,
  systemPrompt?: string
): Promise<string> {
  // Get token from either source (env var or OAuth)
  let token = getAccessToken()
  
  if (!token) {
    throw new Error('GitHub token not found. Please log in or set VITE_GITHUB_TOKEN environment variable.')
  }
  
  // Check if model is available
  const modelAvailable = await isModelAvailable(config.model)
  if (!modelAvailable) {
    throw new Error(
      `Model "${config.model}" is not available in GitHub Models. Please select a supported model.`
    )
  }
  
  // Record this request for rate limiting
  recordRequest()
  
  // Build messages array with optional system prompt
  const messages: Array<{ role: string; content: string }> = []
  
  if (systemPrompt) {
    messages.push({
      role: 'system',
      content: systemPrompt
    })
  }
  
  messages.push({
    role: 'user',
    content: prompt
  })
  
  const requestBody = {
    messages,
    model: config.model,
    temperature: config.temperature,
    max_tokens: config.maxTokens
  }
  
  try {
    const response = await fetch(GITHUB_MODELS_API, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(requestBody)
    })
    
    // Handle token expiration
    if (response.status === 401) {
      console.log('Token expired, attempting refresh...')
      const refreshed = await refreshGitHubToken()
      
      if (refreshed) {
        token = localStorage.getItem('github_access_token')
        // Retry with new token
        const retryResponse = await fetch(GITHUB_MODELS_API, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify(requestBody)
        })
        
        if (!retryResponse.ok) {
          throw {
            status: retryResponse.status,
            message: 'Authentication failed after token refresh'
          }
        }
        
        const data: GitHubModelsResponse = await retryResponse.json()
        
        // Log usage
        logTokenUsage({
          model: config.model,
          promptTokens: data.usage.prompt_tokens,
          completionTokens: data.usage.completion_tokens,
          totalTokens: data.usage.total_tokens,
          timestamp: Date.now()
        })
        
        return data.choices[0].message.content
      } else {
        throw {
          status: 401,
          message: 'GitHub token expired. Please log in again.'
        }
      }
    }
    
    // Handle rate limiting
    if (response.status === 429) {
      const retryAfter = response.headers.get('Retry-After')
      throw {
        status: 429,
        message: 'Rate limit exceeded',
        retryAfter: retryAfter ? parseInt(retryAfter) * 1000 : RATE_LIMIT.retryAfterMs
      }
    }
    
    // Handle other errors
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}))
      throw {
        status: response.status,
        message: errorData.error?.message || `API request failed with status ${response.status}`
      }
    }
    
    const data: GitHubModelsResponse = await response.json()
    
    // Log usage
    logTokenUsage({
      model: config.model,
      promptTokens: data.usage.prompt_tokens,
      completionTokens: data.usage.completion_tokens,
      totalTokens: data.usage.total_tokens,
      timestamp: Date.now()
    })
    
    return data.choices[0].message.content
  } catch (error: any) {
    // Re-throw with structured error
    if (error.status) {
      throw error
    }
    
    throw {
      status: 500,
      message: error.message || 'Network error occurred'
    }
  }
}

/**
 * Get current rate limit status for UI display
 */
export function getCurrentRateLimitStatus() {
  const now = Date.now()
  const recentRequests = requestTimestamps.filter(ts => now - ts < 60000)
  const hourlyRequests = requestTimestamps.filter(ts => now - ts < 3600000)
  
  return {
    requestsPerMinute: recentRequests.length,
    maxRequestsPerMinute: RATE_LIMIT.maxRequestsPerMinute,
    requestsPerHour: hourlyRequests.length,
    maxRequestsPerHour: RATE_LIMIT.maxRequestsPerHour,
    queueLength: requestQueue.length
  }
}
