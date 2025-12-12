/**
 * Spark utility functions
 * 
 * Provides safe access to Spark-specific functionality with fallbacks
 * for non-Spark environments.
 */

import { isSparkEnvironment } from './storage-adapter';
import { getCurrentUser as getGitHubUser } from './github-auth';
import { 
  hasGitHubModelsSupport, 
  executeGitHubModelsLLM,
  type GitHubModelsConfig
} from './github-models-client';
import type { ModelConfig } from './types';

export interface SparkUser {
  avatarUrl: string;
  email: string;
  id: string;
  isOwner: boolean;
  login: string;
}

/**
 * Get the current user information
 * Returns user from Spark environment or GitHub OAuth
 * Returns null if not authenticated
 */
export async function getSparkUser(): Promise<SparkUser | null> {
  if (!isSparkEnvironment()) {
    // In non-Spark mode, use GitHub OAuth
    const githubUser = getGitHubUser();
    if (githubUser) {
      return {
        avatarUrl: githubUser.avatarUrl,
        email: githubUser.email,
        id: githubUser.id,
        isOwner: githubUser.isOwner,
        login: githubUser.login
      };
    }
    
    // No authenticated user
    return null;
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
 * Returns true if either Spark or GitHub Models is available
 */
export function hasLLMSupport(): boolean {
  const sparkAvailable = isSparkEnvironment() && 
                        typeof window.spark?.llm === 'function';
  const githubModelsAvailable = hasGitHubModelsSupport();
  
  return sparkAvailable || githubModelsAvailable;
}

/**
 * Execute an LLM prompt
 * Tries Spark first, then falls back to GitHub Models API
 * Returns null if LLM is not available
 */
export async function executeLLM(
  prompt: string,
  modelName?: string,
  jsonMode?: boolean,
  modelConfig?: ModelConfig
): Promise<string | null> {
  // Priority 1: Try Spark environment if available
  const sparkAvailable = isSparkEnvironment() && 
                        typeof window.spark?.llm === 'function';
  
  if (sparkAvailable) {
    try {
      return await window.spark.llm(prompt, modelName, jsonMode);
    } catch (error) {
      console.error('Spark LLM failed, trying GitHub Models fallback:', error);
      // Fall through to GitHub Models if Spark fails
    }
  }

  // Priority 2: Try GitHub Models API
  if (hasGitHubModelsSupport()) {
    try {
      // Use modelConfig if provided, otherwise use defaults
      const config: GitHubModelsConfig = {
        model: modelConfig?.modelName || modelName || 'gpt-4o-mini',
        temperature: modelConfig?.temperature ?? 0.7,
        maxTokens: modelConfig?.maxTokens ?? 2000
      };
      
      return await executeGitHubModelsLLM(prompt, config);
    } catch (error: any) {
      console.error('GitHub Models LLM failed:', error);
      
      // Provide specific error message to user
      if (error.status === 401) {
        throw new Error('GitHub authentication expired. Please log in again.');
      } else if (error.status === 429) {
        throw new Error('Rate limit exceeded. Please try again in a moment.');
      } else if (error.message) {
        throw new Error(error.message);
      } else {
        throw new Error('AI service unavailable. Please try again later.');
      }
    }
  }

  console.warn('LLM not available in this environment');
  return null;
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
