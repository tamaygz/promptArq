/**
 * Storage Adapter - Abstraction layer for persistence
 * 
 * Automatically detects if running in Spark environment and uses:
 * - Spark KV store when in Spark
 * - localStorage when not in Spark (browser fallback)
 * - SQLite when in Node.js environment (server-side)
 */

// Check if running in Spark environment
// We need to check more than just the existence of window.spark
// because in dev mode, the Spark plugin creates window.spark but it's not functional
export function isSparkEnvironment(): boolean {
  // If no window or spark object, definitely not Spark
  if (typeof window === 'undefined' || typeof window.spark === 'undefined') {
    return false;
  }
  
  // Check if we have the KV API
  if (typeof window.spark.kv === 'undefined') {
    return false;
  }
  
  // If running on localhost in dev mode, assume NOT Spark (primary check)
  // This takes precedence over other checks
  const hostname = window.location.hostname;
  
  if (hostname === 'localhost' || hostname === '127.0.0.1') {
    return false;
  }
  
  // If the URL contains github.app domain, it's Spark
  if (hostname.includes('github.app')) {
    return true;
  }
  
  // Check for Spark-specific environment indicators
  // GITHUB_RUNTIME_PERMANENT_NAME is defined by Spark runtime
  if (typeof GITHUB_RUNTIME_PERMANENT_NAME !== 'undefined' && GITHUB_RUNTIME_PERMANENT_NAME) {
    return true;
  }
  
  // Default to false for safety (use fallback storage)
  return false;
}

// Storage interface that both adapters implement
export interface StorageAdapter {
  keys(): Promise<string[]>;
  get<T>(key: string): Promise<T | undefined>;
  set<T>(key: string, value: T): Promise<void>;
  delete(key: string): Promise<void>;
}

// Spark KV adapter - uses the native Spark key-value store
class SparkKVAdapter implements StorageAdapter {
  async keys(): Promise<string[]> {
    return window.spark.kv.keys();
  }

  async get<T>(key: string): Promise<T | undefined> {
    return window.spark.kv.get<T>(key);
  }

  async set<T>(key: string, value: T): Promise<void> {
    return window.spark.kv.set(key, value);
  }

  async delete(key: string): Promise<void> {
    return window.spark.kv.delete(key);
  }
}

// LocalStorage adapter - uses browser localStorage for local persistence
// This is used when not in Spark environment (e.g., local development)
class LocalStorageAdapter implements StorageAdapter {
  private prefix = 'promptarq_';

  constructor() {
    console.log('LocalStorage persistence initialized');
  }

  async keys(): Promise<string[]> {
    const keys: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith(this.prefix)) {
        keys.push(key.substring(this.prefix.length));
      }
    }
    return keys;
  }

  async get<T>(key: string): Promise<T | undefined> {
    try {
      const item = localStorage.getItem(this.prefix + key);
      if (item === null) return undefined;
      return JSON.parse(item) as T;
    } catch (error) {
      console.error(`Failed to parse value for key "${key}":`, error);
      return undefined;
    }
  }

  async set<T>(key: string, value: T): Promise<void> {
    try {
      const serialized = JSON.stringify(value);
      localStorage.setItem(this.prefix + key, serialized);
    } catch (error) {
      console.error(`Failed to set value for key "${key}":`, error);
      throw error;
    }
  }

  async delete(key: string): Promise<void> {
    localStorage.removeItem(this.prefix + key);
  }
}

// SQLite adapter - uses SQLite for Node.js environments
// This would be used for server-side rendering or Electron apps
class SQLiteAdapter implements StorageAdapter {
  private dbPromise: Promise<any> | null = null;
  private dbPath: string;

  constructor() {
    this.dbPath = 'promptarq.db';
    console.log('SQLite persistence will be initialized on first use');
  }

  private async getDatabase() {
    if (!this.dbPromise) {
      this.dbPromise = this.initDatabase();
    }
    return this.dbPromise;
  }

  private async initDatabase() {
    try {
      // Dynamically import better-sqlite3 only when needed
      const DatabaseModule = await import('better-sqlite3');
      const Database = DatabaseModule.default;
      
      const db = new Database(this.dbPath);
      
      // Create table if it doesn't exist
      db.exec(`
        CREATE TABLE IF NOT EXISTS kv_store (
          key TEXT PRIMARY KEY,
          value TEXT NOT NULL,
          updated_at INTEGER DEFAULT (strftime('%s', 'now'))
        )
      `);
      
      console.log(`SQLite persistence initialized at: ${this.dbPath}`);
      return db;
    } catch (error) {
      console.error('Failed to initialize SQLite database:', error);
      throw error;
    }
  }

  async keys(): Promise<string[]> {
    const db = await this.getDatabase();
    const stmt = db.prepare('SELECT key FROM kv_store');
    const rows = stmt.all() as { key: string }[];
    return rows.map(row => row.key);
  }

  async get<T>(key: string): Promise<T | undefined> {
    const db = await this.getDatabase();
    const stmt = db.prepare('SELECT value FROM kv_store WHERE key = ?');
    const row = stmt.get(key) as { value: string } | undefined;
    
    if (!row) return undefined;
    
    try {
      return JSON.parse(row.value) as T;
    } catch (error) {
      console.error(`Failed to parse value for key "${key}":`, error);
      return undefined;
    }
  }

  async set<T>(key: string, value: T): Promise<void> {
    const db = await this.getDatabase();
    const serialized = JSON.stringify(value);
    const stmt = db.prepare(`
      INSERT INTO kv_store (key, value, updated_at) 
      VALUES (?, ?, strftime('%s', 'now'))
      ON CONFLICT(key) 
      DO UPDATE SET value = excluded.value, updated_at = excluded.updated_at
    `);
    
    stmt.run(key, serialized);
  }

  async delete(key: string): Promise<void> {
    const db = await this.getDatabase();
    const stmt = db.prepare('DELETE FROM kv_store WHERE key = ?');
    stmt.run(key);
  }
}

// Global storage instance
let storageInstance: StorageAdapter | null = null;

// Detect if we're in a Node.js environment (SSR, Electron, etc.)
function isNodeEnvironment(): boolean {
  return typeof process !== 'undefined' && 
         process.versions != null && 
         process.versions.node != null &&
         typeof window === 'undefined';
}

// Get the appropriate storage adapter
export function getStorageAdapter(): StorageAdapter {
  if (storageInstance) {
    return storageInstance;
  }

  if (isSparkEnvironment()) {
    console.log('✨ Using Spark KV store for persistence');
    storageInstance = new SparkKVAdapter();
  } else if (isNodeEnvironment()) {
    console.log('💾 Using SQLite for persistence (Node.js environment)');
    storageInstance = new SQLiteAdapter();
  } else {
    console.log('💾 Using LocalStorage for persistence (browser fallback)');
    storageInstance = new LocalStorageAdapter();
  }

  return storageInstance;
}

// Export convenience methods
export const storage = {
  get adapter() {
    return getStorageAdapter();
  },
  
  isSparkMode: isSparkEnvironment,
  
  async keys() {
    return getStorageAdapter().keys();
  },
  
  async get<T>(key: string) {
    return getStorageAdapter().get<T>(key);
  },
  
  async set<T>(key: string, value: T) {
    return getStorageAdapter().set(key, value);
  },
  
  async delete(key: string) {
    return getStorageAdapter().delete(key);
  }
};
