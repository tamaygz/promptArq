import { Project, Category, Tag } from './types'
import { DEFAULT_CATEGORIES } from './default-categories'
import { DEFAULT_TAGS } from './default-tags'

export function createDefaultProject(): Project {
  return {
    id: `project-${Date.now()}`,
    name: 'General',
    description: 'General purpose prompts and templates',
    color: '#3b82f6'
  }
}

export function createDefaultCategories(projectId: string): Category[] {
  return DEFAULT_CATEGORIES.map((cat, index) => ({
    id: `category-${Date.now()}-${index}`,
    projectId,
    name: cat.name,
    description: cat.description
  }))
}

export function createDefaultTags(): Tag[] {
  return DEFAULT_TAGS.map((tag, index) => ({
    id: `tag-${Date.now()}-${index}`,
    name: tag.name,
    color: tag.color
  }))
}

export function initializeDefaults(): {
  project: Project
  categories: Category[]
  tags: Tag[]
} {
  const project = createDefaultProject()
  const categories = createDefaultCategories(project.id)
  const tags = createDefaultTags()

  return { project, categories, tags }
}
