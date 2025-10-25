export type DefaultCategory = {
  name: string
  description: string
}

export const DEFAULT_CATEGORIES: DefaultCategory[] = [
  {
    name: 'Social Media',
    description: 'Content creation, engagement, and community management for social platforms'
  },
  {
    name: 'Marketing',
    description: 'Marketing copy, campaigns, positioning, and messaging'
  },
  {
    name: 'Developer',
    description: 'Code generation, debugging, and technical problem-solving'
  },
  {
    name: 'Software Architect',
    description: 'System design, architecture decisions, and technical leadership'
  },
  {
    name: 'QA',
    description: 'Test planning, quality assurance, and testing strategies'
  },
  {
    name: 'Business Strategy',
    description: 'Strategic planning, business analysis, and decision-making'
  },
  {
    name: 'Acquisition',
    description: 'User acquisition, growth marketing, and customer acquisition strategies'
  }
]

export function getDefaultCategories(): DefaultCategory[] {
  return DEFAULT_CATEGORIES
}

export function getCategoryByName(name: string): DefaultCategory | undefined {
  return DEFAULT_CATEGORIES.find(
    cat => cat.name.toLowerCase() === name.toLowerCase()
  )
}
