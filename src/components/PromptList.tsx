import { Prompt, Project, Category, Tag } from '@/lib/types'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { Clock } from '@phosphor-icons/react'

type PromptListProps = {
  prompts: Prompt[]
  projects: Project[]
  categories: Category[]
  tags: Tag[]
  selectedPromptId: string | null
  onSelectPrompt: (id: string) => void
}

export function PromptList({ 
  prompts, 
  projects, 
  categories, 
  tags, 
  selectedPromptId, 
  onSelectPrompt 
}: PromptListProps) {
  if (prompts.length === 0) {
    return (
      <div className="p-12 text-center text-muted-foreground text-sm">
        No prompts found. Create your first prompt to get started.
      </div>
    )
  }

  const getProject = (id: string) => projects.find(p => p.id === id)
  const getCategory = (id: string) => categories.find(c => c.id === id)
  const getTag = (id: string) => tags.find(t => t.id === id)

  const formatDate = (timestamp: number) => {
    const date = new Date(timestamp)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 1) return 'Just now'
    if (diffMins < 60) return `${diffMins}m ago`
    if (diffHours < 24) return `${diffHours}h ago`
    if (diffDays < 7) return `${diffDays}d ago`
    return date.toLocaleDateString()
  }

  return (
    <div className="flex flex-col gap-3 p-6">
      {prompts.map(prompt => {
        const project = getProject(prompt.projectId)
        const category = getCategory(prompt.categoryId)
        const promptTags = prompt.tags.map(getTag).filter(Boolean) as Tag[]

        return (
          <Card
            key={prompt.id}
            className={cn(
              "p-5 cursor-pointer transition-all hover:shadow-md border-l-4",
              selectedPromptId === prompt.id 
                ? "border-l-primary bg-accent" 
                : "border-l-transparent hover:border-l-primary/30"
            )}
            onClick={() => onSelectPrompt(prompt.id)}
          >
            <div className="flex flex-col gap-3">
              <div className="flex items-start justify-between gap-3">
                <h3 className="font-medium text-base line-clamp-1">{prompt.title}</h3>
                <div className="flex items-center gap-1.5 text-xs text-muted-foreground shrink-0">
                  <Clock size={14} />
                  {formatDate(prompt.updatedAt)}
                </div>
              </div>
              
              {prompt.description && (
                <p className="text-sm text-muted-foreground line-clamp-2">
                  {prompt.description}
                </p>
              )}

              <div className="flex flex-wrap items-center gap-2 mt-1">
                {project && (
                  <Badge 
                    variant="outline" 
                    className="text-xs px-2.5 py-0.5"
                    style={{ borderColor: project.color, color: project.color }}
                  >
                    {project.name}
                  </Badge>
                )}
                {category && (
                  <Badge variant="secondary" className="text-xs px-2.5 py-0.5">
                    {category.name}
                  </Badge>
                )}
                {promptTags.slice(0, 3).map(tag => (
                  <Badge
                    key={tag.id}
                    variant="outline"
                    className="text-xs px-2.5 py-0.5"
                    style={{ borderColor: tag.color, color: tag.color }}
                  >
                    {tag.name}
                  </Badge>
                ))}
                {promptTags.length > 3 && (
                  <Badge variant="outline" className="text-xs px-2.5 py-0.5">
                    +{promptTags.length - 3}
                  </Badge>
                )}
              </div>
            </div>
          </Card>
        )
      })}
    </div>
  )
}
