/**
 * HTTP Storage Adapter - Connects to LocalStorageServer running on localhost:5001
 * This allows WebView2 and browser to share the same SQLite database
 */

import { StorageAdapter } from './storage-adapter'

class HttpStorageAdapter implements StorageAdapter {
  private baseUrl = 'http://localhost:5001'
  private prefix = 'promptarq_'

  constructor() {
    console.log('?? HTTP Storage Adapter initialized (connecting to local storage server)')
    this.checkHealth()
  }

  private async checkHealth(): Promise<void> {
    try {
      const response = await fetch(`${this.baseUrl}/health`)
      const data = await response.json()
      console.log('? Storage server connected:', data)
    } catch (error) {
      console.warn('?? Storage server not reachable - data will not persist across browser/app')
    }
  }

  async keys(): Promise<string[]> {
    try {
      const response = await fetch(`${this.baseUrl}/keys`)
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      const keys = await response.json()
      // Remove prefix from keys
      return keys.map((k: string) => k.startsWith(this.prefix) ? k.substring(this.prefix.length) : k)
    } catch (error) {
      console.error('Failed to get keys from storage server:', error)
      return []
    }
  }

  async get<T>(key: string): Promise<T | undefined> {
    try {
      const fullKey = this.prefix + key
      const response = await fetch(`${this.baseUrl}/get?key=${encodeURIComponent(fullKey)}`)
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      const text = await response.text()
      if (!text || text === 'null') return undefined
      return JSON.parse(text) as T
    } catch (error) {
      console.error(`Failed to get key "${key}" from storage server:`, error)
      return undefined
    }
  }

  async set<T>(key: string, value: T): Promise<void> {
    try {
      const fullKey = this.prefix + key
      const json = JSON.stringify(value)
      const response = await fetch(`${this.baseUrl}/set?key=${encodeURIComponent(fullKey)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: json
      })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
    } catch (error) {
      console.error(`Failed to set key "${key}" in storage server:`, error)
      throw error
    }
  }

  async delete(key: string): Promise<void> {
    try {
      const fullKey = this.prefix + key
      const response = await fetch(`${this.baseUrl}/delete?key=${encodeURIComponent(fullKey)}`, {
        method: 'DELETE'
      })
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
    } catch (error) {
      console.error(`Failed to delete key "${key}" from storage server:`, error)
    }
  }
}

export { HttpStorageAdapter }
