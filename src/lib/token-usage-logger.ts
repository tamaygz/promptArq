/**
 * Token Usage Logger
 * 
 * Tracks GitHub Models API usage including:
 * - Request counts
 * - Token consumption (prompt + completion)
 * - Cost estimation
 * - Historical data
 */

export interface TokenUsageEntry {
  model: string
  promptTokens: number
  completionTokens: number
  totalTokens: number
  timestamp: number
}

export interface UsageStats {
  totalRequests: number
  totalPromptTokens: number
  totalCompletionTokens: number
  totalTokens: number
  byModel: Record<string, {
    requests: number
    promptTokens: number
    completionTokens: number
    totalTokens: number
  }>
  last24Hours: {
    requests: number
    tokens: number
  }
  last7Days: {
    requests: number
    tokens: number
  }
}

const STORAGE_KEY = 'github_models_usage'
const MAX_ENTRIES = 1000 // Keep last 1000 requests

/**
 * Log a token usage entry
 */
export function logTokenUsage(entry: TokenUsageEntry): void {
  try {
    const existing = getUsageHistory()
    existing.push(entry)
    
    // Keep only recent entries
    const trimmed = existing.slice(-MAX_ENTRIES)
    
    localStorage.setItem(STORAGE_KEY, JSON.stringify(trimmed))
  } catch (error) {
    console.error('Failed to log token usage:', error)
  }
}

/**
 * Get all usage history
 */
export function getUsageHistory(): TokenUsageEntry[] {
  try {
    const data = localStorage.getItem(STORAGE_KEY)
    if (!data) return []
    
    const entries = JSON.parse(data) as TokenUsageEntry[]
    
    // Clean up entries older than 30 days
    const thirtyDaysAgo = Date.now() - (30 * 24 * 60 * 60 * 1000)
    return entries.filter(e => e.timestamp > thirtyDaysAgo)
  } catch (error) {
    console.error('Failed to get usage history:', error)
    return []
  }
}

/**
 * Get aggregated usage statistics
 */
export function getUsageStats(): UsageStats {
  const entries = getUsageHistory()
  const now = Date.now()
  const last24Hours = now - (24 * 60 * 60 * 1000)
  const last7Days = now - (7 * 24 * 60 * 60 * 1000)
  
  const stats: UsageStats = {
    totalRequests: entries.length,
    totalPromptTokens: 0,
    totalCompletionTokens: 0,
    totalTokens: 0,
    byModel: {},
    last24Hours: {
      requests: 0,
      tokens: 0
    },
    last7Days: {
      requests: 0,
      tokens: 0
    }
  }
  
  for (const entry of entries) {
    // Overall stats
    stats.totalPromptTokens += entry.promptTokens
    stats.totalCompletionTokens += entry.completionTokens
    stats.totalTokens += entry.totalTokens
    
    // By model
    if (!stats.byModel[entry.model]) {
      stats.byModel[entry.model] = {
        requests: 0,
        promptTokens: 0,
        completionTokens: 0,
        totalTokens: 0
      }
    }
    stats.byModel[entry.model].requests++
    stats.byModel[entry.model].promptTokens += entry.promptTokens
    stats.byModel[entry.model].completionTokens += entry.completionTokens
    stats.byModel[entry.model].totalTokens += entry.totalTokens
    
    // Time-based stats
    if (entry.timestamp > last24Hours) {
      stats.last24Hours.requests++
      stats.last24Hours.tokens += entry.totalTokens
    }
    
    if (entry.timestamp > last7Days) {
      stats.last7Days.requests++
      stats.last7Days.tokens += entry.totalTokens
    }
  }
  
  return stats
}

/**
 * Get rate limit status based on recent usage
 */
export function getRateLimitStatus() {
  const entries = getUsageHistory()
  const now = Date.now()
  const lastMinute = now - 60000
  const lastHour = now - 3600000
  
  const recentMinute = entries.filter(e => e.timestamp > lastMinute).length
  const recentHour = entries.filter(e => e.timestamp > lastHour).length
  
  return {
    lastMinute: recentMinute,
    lastHour: recentHour,
    approaching: recentMinute > 40 || recentHour > 400 // 80% of typical limits
  }
}

/**
 * Clear usage history (for privacy/reset)
 */
export function clearUsageHistory(): void {
  try {
    localStorage.removeItem(STORAGE_KEY)
  } catch (error) {
    console.error('Failed to clear usage history:', error)
  }
}

/**
 * Export usage data as CSV
 */
export function exportUsageAsCSV(): string {
  const entries = getUsageHistory()
  
  const headers = ['Timestamp', 'Model', 'Prompt Tokens', 'Completion Tokens', 'Total Tokens']
  const rows = entries.map(e => [
    new Date(e.timestamp).toISOString(),
    e.model,
    e.promptTokens.toString(),
    e.completionTokens.toString(),
    e.totalTokens.toString()
  ])
  
  const csv = [
    headers.join(','),
    ...rows.map(row => row.join(','))
  ].join('\n')
  
  return csv
}

/**
 * Get usage summary for display
 */
export function getUsageSummary() {
  const stats = getUsageStats()
  
  return {
    totalRequests: stats.totalRequests,
    totalTokens: stats.totalTokens,
    todayRequests: stats.last24Hours.requests,
    todayTokens: stats.last24Hours.tokens,
    weekRequests: stats.last7Days.requests,
    weekTokens: stats.last7Days.tokens,
    topModel: Object.entries(stats.byModel)
      .sort((a, b) => b[1].requests - a[1].requests)[0]?.[0] || 'N/A',
    avgTokensPerRequest: stats.totalRequests > 0 
      ? Math.round(stats.totalTokens / stats.totalRequests) 
      : 0
  }
}
