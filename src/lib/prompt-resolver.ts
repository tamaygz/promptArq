import { SystemPrompt, Prompt, Project, Category, Tag } from './types'

const DEFAULT_SYSTEM_PROMPT = `You are an expert at improving LLM prompts. Your task is to analyze prompts and suggest improved versions that are:
- Clearer and more specific
- Better structured
- More effective at eliciting desired responses
- Following best practices for prompt engineering

Maintain the original intent while enhancing clarity, structure, and effectiveness.`

export function resolveSystemPrompt(
  prompt: Prompt | undefined,
  project: Project | undefined,
  category: Category | undefined,
  tags: Tag[],
  systemPrompts: SystemPrompt[]
): string {
  if (!prompt) return DEFAULT_SYSTEM_PROMPT

  const promptOverride = systemPrompts.find(
    sp => sp.scopeType === 'prompt' && sp.scopeId === prompt.id
  )
  if (promptOverride) return promptOverride.content

  if (project) {
    const projectPrompt = systemPrompts.find(
      sp => sp.scopeType === 'project' && sp.scopeId === project.id
    )
    if (projectPrompt) return projectPrompt.content
  }

  if (category) {
    const categoryPrompt = systemPrompts.find(
      sp => sp.scopeType === 'category' && sp.scopeId === category.id
    )
    if (categoryPrompt) return categoryPrompt.content
  }

  if (tags.length > 0) {
    const tagPrompts = systemPrompts
      .filter(sp => sp.scopeType === 'tag' && tags.some(t => t.id === sp.scopeId))
      .sort((a, b) => b.priority - a.priority || b.createdAt - a.createdAt)
    
    if (tagPrompts.length > 0) return tagPrompts[0].content
  }

  const teamPrompt = systemPrompts.find(sp => sp.scopeType === 'team' && !sp.scopeId)
  if (teamPrompt) return teamPrompt.content

  return DEFAULT_SYSTEM_PROMPT
}
