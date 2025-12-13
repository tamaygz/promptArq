import { SystemPrompt, Prompt, Project, Category, Tag } from './types'
import { getDefaultSystemPromptByCategory } from './default-system-prompts'

const DEFAULT_SYSTEM_PROMPT = `You are a helpful AI assistant. Follow the user's instructions carefully and provide accurate, relevant responses.`

const DEFAULT_IMPROVEMENT_PROMPT = `You are an expert in prompt design and human-AI communication.
Your task is to read the user's prompt, infer their underlying goal, and rewrite the prompt so that it clearly and effectively guides an advanced language model toward that goal.

Follow these steps in your reasoning, but only output the improved prompts and short commentary — not the steps themselves:

Understand the intent — Read the prompt as if you were the user. What are they truly trying to achieve or produce? Infer this even if the original wording is vague or incomplete.

Clarify context and purpose — Identify what context is missing (audience, tone, style, format, constraints, evaluation criteria) and subtly include it in your improved versions.

Adapt to task type —

If it's about writing or communication, improve clarity, structure, audience awareness, and tone.

If it's about coding or technical tasks, specify desired behavior, input/output examples, constraints, and performance or style expectations.

If it's creative or analytical, enrich the framing, add perspective or creative direction, and balance freedom with focus.

Enhance precision and creativity — Remove ambiguity, strengthen the call to action, and ensure the result invites the model to think deeply, not mechanically.

Offer range — Provide two or three improved versions that vary in tone and focus (e.g., concise, detailed, or imaginative). Each should make the user's intention unmistakable.

Explain briefly — After the rewrites, give a short paragraph (max 5 sentences) explaining what you inferred about the goal and how your improvements help achieve it.

Your output should be natural text — no technical formatting, no lists of steps — just the refined prompts and a short explanation of how they better serve the user's intent.`

function resolveSystemPromptByUsage(
  prompt: Prompt | undefined,
  project: Project | undefined,
  category: Category | undefined,
  tags: Tag[],
  systemPrompts: SystemPrompt[],
  usage: 'execution' | 'improvement',
  defaultPrompt: string
): string {
  if (!prompt) return defaultPrompt

  const filteredPrompts = systemPrompts.filter(sp => sp.usage === usage)

  const promptOverride = filteredPrompts.find(
    sp => sp.scopeType === 'prompt' && sp.scopeId === prompt.id
  )
  if (promptOverride) return promptOverride.content

  if (project) {
    const projectPrompt = filteredPrompts.find(
      sp => sp.scopeType === 'project' && sp.scopeId === project.id
    )
    if (projectPrompt) return projectPrompt.content
  }

  if (category) {
    const categoryPrompt = filteredPrompts.find(
      sp => sp.scopeType === 'category' && sp.scopeId === category.id
    )
    if (categoryPrompt) return categoryPrompt.content
    
    if (usage === 'execution') {
      const defaultCategoryPrompt = getDefaultSystemPromptByCategory(category.name)
      if (defaultCategoryPrompt) return defaultCategoryPrompt.content
    }
  }

  if (tags.length > 0) {
    const tagPrompts = filteredPrompts
      .filter(sp => sp.scopeType === 'tag' && tags.some(t => t.id === sp.scopeId))
      .sort((a, b) => b.priority - a.priority || b.createdAt - a.createdAt)
    
    if (tagPrompts.length > 0) return tagPrompts[0].content
  }

  const teamPrompt = filteredPrompts.find(sp => sp.scopeType === 'team' && !sp.scopeId)
  if (teamPrompt) return teamPrompt.content

  return defaultPrompt
}

export function resolveSystemPrompt(
  prompt: Prompt | undefined,
  project: Project | undefined,
  category: Category | undefined,
  tags: Tag[],
  systemPrompts: SystemPrompt[]
): string {
  return resolveSystemPromptByUsage(prompt, project, category, tags, systemPrompts, 'execution', DEFAULT_SYSTEM_PROMPT)
}

export function resolveImprovementSystemPrompt(
  prompt: Prompt | undefined,
  project: Project | undefined,
  category: Category | undefined,
  tags: Tag[],
  systemPrompts: SystemPrompt[]
): string {
  return resolveSystemPromptByUsage(prompt, project, category, tags, systemPrompts, 'improvement', DEFAULT_IMPROVEMENT_PROMPT)
}
