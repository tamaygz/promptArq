import { ModelConfig, Prompt, Project, Category, Tag } from './types'

const DEFAULT_MODEL_CONFIG: Omit<ModelConfig, 'id' | 'createdBy' | 'createdAt' | 'scopeType'> = {
  name: 'Default',
  provider: 'openai',
  modelName: 'gpt-4o-mini',
  temperature: 0.7,
  maxTokens: 2000
}

export function resolveModelConfig(
  prompt: Prompt | undefined,
  project: Project | undefined,
  category: Category | undefined,
  tags: Tag[],
  modelConfigs: ModelConfig[]
): ModelConfig {
  if (!prompt) {
    return {
      ...DEFAULT_MODEL_CONFIG,
      id: 'default',
      scopeType: 'team',
      createdBy: 'system',
      createdAt: Date.now()
    }
  }

  const promptConfig = modelConfigs.find(
    mc => mc.scopeType === 'prompt' && mc.scopeId === prompt.id
  )
  if (promptConfig) return promptConfig

  if (project) {
    const projectConfig = modelConfigs.find(
      mc => mc.scopeType === 'project' && mc.scopeId === project.id
    )
    if (projectConfig) return projectConfig
  }

  if (category) {
    const categoryConfig = modelConfigs.find(
      mc => mc.scopeType === 'category' && mc.scopeId === category.id
    )
    if (categoryConfig) return categoryConfig
  }

  if (tags.length > 0) {
    const tagConfigsWithPriority = modelConfigs
      .filter(mc => mc.scopeType === 'tag' && tags.some(t => t.id === mc.scopeId))
      .sort((a, b) => b.createdAt - a.createdAt)
    
    if (tagConfigsWithPriority.length > 0) return tagConfigsWithPriority[0]
  }

  const teamConfig = modelConfigs.find(mc => mc.scopeType === 'team' && !mc.scopeId)
  if (teamConfig) return teamConfig

  return {
    ...DEFAULT_MODEL_CONFIG,
    id: 'default',
    scopeType: 'team',
    createdBy: 'system',
    createdAt: Date.now()
  }
}
