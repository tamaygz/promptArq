/**
 * Spark Gateway Module
 * 
 * Provides safe access to Spark functionality with automatic fallback
 * for non-Spark environments. This module ensures no errors occur when
 * Spark is not available.
 */

import { isSparkEnvironment } from './storage-adapter';

/**
 * Safely load Spark if available
 * This prevents errors in standalone mode
 */
export function initializeSpark(): void {
  if (!isSparkEnvironment()) {
    // Create a stub window.spark to prevent errors
    if (typeof window !== 'undefined' && !window.spark) {
      (window as any).spark = {
        user: async () => null,
        llm: async () => null,
        llmPrompt: () => '',
        kv: {
          get: async () => null,
          set: async () => {},
          delete: async () => {},
          keys: async () => []
        }
      };
    }
    return;
  }

  // In Spark environment, import the actual Spark module
  try {
    import('@github/spark/spark');
  } catch (error) {
    console.warn('Failed to load Spark module:', error);
  }
}

/**
 * Check if LLM features are available
 */
export function hasLLMFeatures(): boolean {
  return isSparkEnvironment() && 
         typeof window !== 'undefined' &&
         typeof window.spark?.llm === 'function';
}

/**
 * Check if user features are available
 */
export function hasUserFeatures(): boolean {
  return isSparkEnvironment() && 
         typeof window !== 'undefined' &&
         typeof window.spark?.user === 'function';
}

/**
 * Get feature availability status
 */
export function getFeatureStatus() {
  const sparkAvailable = isSparkEnvironment();
  
  return {
    spark: sparkAvailable,
    llm: hasLLMFeatures(),
    user: hasUserFeatures(),
    kv: sparkAvailable && typeof window?.spark?.kv !== 'undefined',
    mode: sparkAvailable ? 'spark' : 'standalone'
  };
}
