import { Prompt, PromptVersion, Project, Category, Tag } from './types'

export function exportPrompt(
  prompt: Prompt,
  versions: PromptVersion[],
  project?: Project,
  category?: Category,
  tags?: Tag[]
) {
  const data = {
    prompt: {
      id: prompt.id,
      title: prompt.title,
      description: prompt.description,
      content: prompt.content,
      project: project?.name,
      category: category?.name,
      tags: tags?.map(t => t.name) || [],
      createdBy: prompt.createdBy,
      createdAt: new Date(prompt.createdAt).toISOString(),
      updatedAt: new Date(prompt.updatedAt).toISOString(),
    },
    versions: versions.map(v => ({
      versionNumber: v.versionNumber,
      content: v.content,
      changeNote: v.changeNote,
      createdBy: v.createdBy,
      createdAt: new Date(v.createdAt).toISOString(),
    })),
  }

  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${prompt.title.replace(/[^a-z0-9]/gi, '_').toLowerCase()}_${Date.now()}.json`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

export function exportAllPrompts(
  prompts: Prompt[],
  versions: PromptVersion[],
  projects: Project[],
  categories: Category[],
  tags: Tag[]
) {
  const data = {
    exportedAt: new Date().toISOString(),
    prompts: prompts.map(prompt => {
      const project = projects.find(p => p.id === prompt.projectId)
      const category = categories.find(c => c.id === prompt.categoryId)
      const promptTags = tags.filter(t => prompt.tags.includes(t.id))
      const promptVersions = versions.filter(v => v.promptId === prompt.id)

      return {
        id: prompt.id,
        title: prompt.title,
        description: prompt.description,
        content: prompt.content,
        project: project?.name,
        category: category?.name,
        tags: promptTags.map(t => t.name),
        createdBy: prompt.createdBy,
        createdAt: new Date(prompt.createdAt).toISOString(),
        updatedAt: new Date(prompt.updatedAt).toISOString(),
        isArchived: prompt.isArchived,
        versions: promptVersions.map(v => ({
          versionNumber: v.versionNumber,
          content: v.content,
          changeNote: v.changeNote,
          createdBy: v.createdBy,
          createdAt: new Date(v.createdAt).toISOString(),
        })),
      }
    }),
    projects: projects.map(p => ({ name: p.name, description: p.description })),
    categories: categories.map(c => ({ name: c.name, description: c.description })),
    tags: tags.map(t => ({ name: t.name, color: t.color })),
  }

  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `arqioly_export_${Date.now()}.json`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}
