// This file is deprecated. The improve prompt functionality now uses
// the system prompts configuration with 'improvement' usage type.
// See prompt-resolver.ts for the implementation.

export const IMPROVE_PROMPT_SYSTEM_PROMPT = ""

export function getImprovePromptSystemPrompt(): string {
  // This function is no longer used. The improve functionality now uses
  // resolveImprovementSystemPrompt from prompt-resolver.ts
  return IMPROVE_PROMPT_SYSTEM_PROMPT
}
