/**
 * Spark utility functions
 * 
 * Provides safe access to Spark-specific functionality with fallbacks
 * for non-Spark environments.
 */

import { isSparkEnvironment } from './storage-adapter';

export interface SparkUser {
  avatarUrl: string;
  email: string;
  id: string;
  isOwner: boolean;
  login: string;
}

/**
 * Get the current user information
 * Returns null if not in Spark environment or if user fetch fails
 */
export async function getSparkUser(): Promise<SparkUser | null> {
  if (!isSparkEnvironment()) {
    // In non-Spark mode, return a mock user for development
    return {
      avatarUrl: 'https://github.com/github.png',
      email: 'developer@local.dev',
      id: 'local-dev-user',
      isOwner: true,
      login: 'Local Developer'
    };
  }

  try {
    return await window.spark.user();
  } catch (error) {
    console.error('Failed to get Spark user:', error);
    return null;
  }
}

/**
 * Check if LLM functionality is available
 */
export function hasLLMSupport(): boolean {
  return isSparkEnvironment() && 
         typeof window.spark.llm === 'function';
}

/**
 * Execute an LLM prompt
 * Returns null if LLM is not available
 */
export async function executeLLM(
  prompt: string,
  modelName?: string,
  jsonMode?: boolean
): Promise<string | null> {
  if (!hasLLMSupport()) {
    console.warn('LLM not available in this environment');
    return null;
  }

  try {
    return await window.spark.llm(prompt, modelName, jsonMode);
  } catch (error) {
    console.error('Failed to execute LLM:', error);
    return null;
  }
}

/**
 * Create an LLM prompt using template literals
 */
export function createLLMPrompt(
  strings: TemplateStringsArray,
  ...values: any[]
): string {
  if (isSparkEnvironment() && typeof window.spark.llmPrompt === 'function') {
    return window.spark.llmPrompt(strings, ...values);
  }

  // Fallback: manually construct the prompt
  return strings.reduce((result, str, i) => {
    return result + str + (values[i] || '');
  }, '');
}
