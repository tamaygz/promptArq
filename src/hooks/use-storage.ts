/**
 * useStorage - Universal storage hook
 * 
 * Works with both Spark KV and SQLite backends.
 * Drop-in replacement for @github/spark/hooks useKV.
 */

import { useState, useEffect, useCallback } from 'react';
import { isSparkEnvironment, getStorageAdapter } from '@/lib/storage-adapter';
import { useKV as useSparkKV } from '@github/spark/hooks';

/**
 * Universal storage hook that works in both Spark and non-Spark environments
 * 
 * @param key - Storage key
 * @param defaultValue - Default value if key doesn't exist
 * @returns [value, setValue] tuple, same as useKV
 */
export function useStorage<T>(
  key: string,
  defaultValue: T
): [T, (value: T | ((current: T) => T)) => void] {
  // If in Spark environment, use the native useKV hook
  if (isSparkEnvironment()) {
    return useSparkKV<T>(key, defaultValue);
  }

  // Otherwise, use our custom implementation with SQLite
  const [value, setValue] = useState<T>(defaultValue);
  const [isLoaded, setIsLoaded] = useState(false);

  // Load initial value from storage
  useEffect(() => {
    const loadValue = async () => {
      try {
        const storage = getStorageAdapter();
        const storedValue = await storage.get<T>(key);
        
        if (storedValue !== undefined) {
          setValue(storedValue);
        }
      } catch (error) {
        console.error(`Failed to load value for key "${key}":`, error);
      } finally {
        setIsLoaded(true);
      }
    };

    loadValue();
  }, [key]);

  // Update function that persists to storage
  const updateValue = useCallback(
    (newValue: T | ((current: T) => T)) => {
      setValue((currentValue) => {
        const nextValue =
          typeof newValue === 'function'
            ? (newValue as (current: T) => T)(currentValue)
            : newValue;

        // Persist to storage asynchronously
        const storage = getStorageAdapter();
        storage.set(key, nextValue).catch((error) => {
          console.error(`Failed to persist value for key "${key}":`, error);
        });

        return nextValue;
      });
    },
    [key]
  );

  return [value, updateValue];
}

// Re-export for convenience
export { isSparkEnvironment, getStorageAdapter, storage } from '@/lib/storage-adapter';
