# Persistence Layer Documentation

## Overview

The promptArq application now includes an intelligent persistence layer that automatically detects the runtime environment and uses the appropriate storage backend.

## Storage Backends

The application supports three storage backends:

### 1. Spark KV Store (Primary - Spark Environment)
- **When**: Running in GitHub Spark runtime
- **Detection**: Checks for `window.spark.kv` availability
- **Storage**: Uses Spark's built-in key-value store
- **Benefits**: 
  - Native Spark integration
  - User-scoped data isolation
  - Automatic cloud persistence
  - No configuration needed

### 2. LocalStorage (Browser Fallback)
- **When**: Running in browser but not in Spark environment
- **Detection**: Browser environment without Spark
- **Storage**: Browser's localStorage API
- **Benefits**:
  - Works in any modern browser
  - No server-side dependencies
  - Instant read/write operations
  - Good for development and testing
- **Limitations**:
  - Limited to ~5-10MB storage
  - Data is browser-specific
  - Can be cleared by user

### 3. SQLite (Node.js Environment)
- **When**: Running in Node.js (SSR, Electron, server-side)
- **Detection**: Node.js process detected
- **Storage**: SQLite database file (`promptarq.db`)
- **Benefits**:
  - Unlimited storage capacity
  - High performance
  - File-based persistence
  - Works offline
- **Requirements**:
  - Node.js environment
  - `better-sqlite3` package installed

## Architecture

### Environment Detection

```typescript
// Check if running in Spark
function isSparkEnvironment(): boolean {
  return typeof window !== 'undefined' && 
         typeof window.spark !== 'undefined' &&
         typeof window.spark.kv !== 'undefined';
}

// Check if running in Node.js
function isNodeEnvironment(): boolean {
  return typeof process !== 'undefined' && 
         process.versions != null && 
         process.versions.node != null &&
         typeof window === 'undefined';
}
```

### Storage Adapter Interface

All storage backends implement the same interface:

```typescript
interface StorageAdapter {
  keys(): Promise<string[]>;
  get<T>(key: string): Promise<T | undefined>;
  set<T>(key: string, value: T): Promise<void>;
  delete(key: string): Promise<void>;
}
```

### Usage in Components

The `useStorage` hook is a drop-in replacement for `@github/spark/hooks`'s `useKV`:

```typescript
import { useStorage } from '@/hooks/use-storage';

function MyComponent() {
  const [data, setData] = useStorage<MyType>('my-key', defaultValue);
  
  // Use exactly like useState
  setData(newValue);
  setData(prev => ({ ...prev, updated: true }));
}
```

## Migration from useKV

The migration is straightforward:

**Before:**
```typescript
import { useKV } from '@github/spark/hooks';

const [prompts, setPrompts] = useKV<Prompt[]>('prompts', []);
```

**After:**
```typescript
import { useStorage } from '@/hooks/use-storage';

const [prompts, setPrompts] = useStorage<Prompt[]>('prompts', []);
```

## Spark-Specific Features

Some features are only available in Spark environment:

### User Authentication

```typescript
import { getSparkUser } from '@/lib/spark-utils';

const user = await getSparkUser();
// Returns SparkUser in Spark, mock user in non-Spark
```

### LLM Integration

```typescript
import { hasLLMSupport, executeLLM } from '@/lib/spark-utils';

if (hasLLMSupport()) {
  const response = await executeLLM(prompt, modelName);
}
```

## Development

### Running Locally (Non-Spark)

When running locally with `npm run dev`, the app will:
1. Detect it's not in Spark environment
2. Use LocalStorage for persistence
3. Use a mock user for development
4. Disable LLM features (returns null)

```bash
npm install
npm run dev
```

### Running in Spark

When deployed to GitHub Spark, the app will:
1. Detect Spark environment automatically
2. Use Spark KV store for persistence
3. Use real Spark user authentication
4. Enable full LLM features

## File Structure

```
src/
├── lib/
│   ├── storage-adapter.ts      # Core storage abstraction
│   └── spark-utils.ts          # Spark-specific utilities
├── hooks/
│   └── use-storage.ts          # React hook for storage
└── App.tsx                     # Updated to use useStorage
```

## Console Messages

The app logs which storage backend is being used:

- `✨ Using Spark KV store for persistence` - In Spark
- `💾 Using LocalStorage for persistence (browser fallback)` - In browser
- `💾 Using SQLite for persistence (Node.js environment)` - In Node.js

## Data Compatibility

All three storage backends use JSON serialization, ensuring data is compatible across environments. You can:

1. Develop locally with LocalStorage
2. Export data to JSON
3. Import data in Spark environment
4. Data structure remains identical

## Security Considerations

### Spark KV
- User-scoped data isolation
- Automatic encryption at rest
- Secure by default

### LocalStorage
- Client-side only
- No encryption
- Visible in browser DevTools
- **Do not store sensitive data**

### SQLite
- File-based storage
- No built-in encryption
- Secure file permissions recommended
- Good for development/testing

## Troubleshooting

### Issue: "Database not initialized"
**Solution**: Ensure `better-sqlite3` is installed: `npm install`

### Issue: LocalStorage quota exceeded
**Solution**: LocalStorage has ~5-10MB limit. Use Spark environment for production.

### Issue: Data not persisting
**Check**: Browser console for storage backend message and any errors

## Future Enhancements

Potential improvements:
- IndexedDB adapter for larger browser storage
- Remote database support (PostgreSQL, MySQL)
- Data synchronization across storage backends
- Encrypted local storage option
- Import/export utilities

## Testing

Basic functionality test:

```bash
# The app will automatically detect and use the right storage
npm run dev

# Check browser console for:
# "💾 Using LocalStorage for persistence (browser fallback)"
```

## Summary

The persistence layer provides:
- ✅ Automatic environment detection
- ✅ Seamless Spark KV integration
- ✅ Browser fallback with LocalStorage
- ✅ Node.js support with SQLite
- ✅ Unified API across all backends
- ✅ Drop-in replacement for useKV
- ✅ Zero configuration required
