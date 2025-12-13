import { PromptVersion, Project, Category } from '@/lib/types'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { cn } from '@/lib/utils'

type VersionDiffProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  oldVersion: PromptVersion
  newVersion: PromptVersion
  projects: Project[]
  categories: Category[]
}

type DiffFieldProps = {
  label: string
  oldValue: string | boolean | string[] | undefined
  newValue: string | boolean | string[] | undefined
}

function arraysEqual(a: string[] | undefined, b: string[] | undefined): boolean {
  if (a === b) return true
  if (!a || !b) return false
  if (a.length !== b.length) return false
  return a.every((val, idx) => val === b[idx])
}

function valuesEqual(oldValue: string | boolean | string[] | undefined, newValue: string | boolean | string[] | undefined): boolean {
  if (Array.isArray(oldValue) && Array.isArray(newValue)) {
    return arraysEqual(oldValue, newValue)
  }
  return oldValue === newValue
}

function DiffField({ label, oldValue, newValue }: DiffFieldProps) {
  const formatValue = (value: string | boolean | string[] | undefined) => {
    if (value === undefined) return '(not tracked)'
    if (typeof value === 'boolean') return value ? 'Yes' : 'No'
    if (Array.isArray(value)) return value.length > 0 ? value.join(', ') : '(none)'
    return value || '(empty)'
  }

  const hasChanged = !valuesEqual(oldValue, newValue)

  if (!hasChanged) return null

  return (
    <div className="grid grid-cols-2 gap-4 mb-3">
      <div className="space-y-1">
        <div className="text-xs font-semibold text-muted-foreground">{label}</div>
        <div className="text-sm p-2 rounded bg-muted/30 border">
          {formatValue(oldValue)}
        </div>
      </div>
      <div className="space-y-1">
        <div className="text-xs font-semibold text-muted-foreground">{label}</div>
        <div className="text-sm p-2 rounded bg-primary/5 border">
          {formatValue(newValue)}
        </div>
      </div>
    </div>
  )
}

export function VersionDiff({ open, onOpenChange, oldVersion, newVersion, projects, categories }: VersionDiffProps) {
  const formatDate = (timestamp: number) => {
    return new Date(timestamp).toLocaleString()
  }

  // Helper function to get project name by ID
  const getProjectName = (projectId: string | undefined) => {
    if (!projectId) return undefined
    const project = projects.find(p => p.id === projectId)
    return project?.name
  }

  // Helper function to get category name by ID
  const getCategoryName = (categoryId: string | undefined) => {
    if (!categoryId) return undefined
    const category = categories.find(c => c.id === categoryId)
    return category?.name
  }

  // Check each field individually for better readability and maintainability
  const titleChanged = (oldVersion.title !== undefined || newVersion.title !== undefined) && oldVersion.title !== newVersion.title
  const descriptionChanged = (oldVersion.description !== undefined || newVersion.description !== undefined) && oldVersion.description !== newVersion.description
  const projectChanged = (oldVersion.projectId !== undefined || newVersion.projectId !== undefined) && oldVersion.projectId !== newVersion.projectId
  const categoryChanged = (oldVersion.categoryId !== undefined || newVersion.categoryId !== undefined) && oldVersion.categoryId !== newVersion.categoryId
  const tagsChanged = (oldVersion.tags !== undefined || newVersion.tags !== undefined) && !arraysEqual(oldVersion.tags, newVersion.tags)
  const archivedChanged = (oldVersion.isArchived !== undefined || newVersion.isArchived !== undefined) && oldVersion.isArchived !== newVersion.isArchived
  const mcpChanged = (oldVersion.exposedToMCP !== undefined || newVersion.exposedToMCP !== undefined) && oldVersion.exposedToMCP !== newVersion.exposedToMCP
  const llmChanged = (oldVersion.execute_llm !== undefined || newVersion.execute_llm !== undefined) && oldVersion.execute_llm !== newVersion.execute_llm
  
  const hasMetadataChanges = titleChanged || descriptionChanged || projectChanged || categoryChanged || tagsChanged || archivedChanged || mcpChanged || llmChanged

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl w-[80vw] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Version Comparison</DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <Badge variant="secondary">v{oldVersion.versionNumber}</Badge>
              <span className="text-xs text-muted-foreground">
                {formatDate(oldVersion.createdAt)}
              </span>
            </div>
            <p className="text-sm text-muted-foreground">{oldVersion.changeNote}</p>
          </div>

          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <Badge variant="default">v{newVersion.versionNumber}</Badge>
              <span className="text-xs text-muted-foreground">
                {formatDate(newVersion.createdAt)}
              </span>
            </div>
            <p className="text-sm text-muted-foreground">{newVersion.changeNote}</p>
          </div>
        </div>

        {hasMetadataChanges && (
          <div className="space-y-2">
            <h3 className="text-sm font-semibold">Metadata Changes</h3>
            <DiffField label="Title" oldValue={oldVersion.title} newValue={newVersion.title} />
            <DiffField label="Description" oldValue={oldVersion.description} newValue={newVersion.description} />
            <DiffField label="Project" oldValue={getProjectName(oldVersion.projectId)} newValue={getProjectName(newVersion.projectId)} />
            <DiffField label="Category" oldValue={getCategoryName(oldVersion.categoryId)} newValue={getCategoryName(newVersion.categoryId)} />
            <DiffField label="Tags" oldValue={oldVersion.tags} newValue={newVersion.tags} />
            <DiffField label="Archived" oldValue={oldVersion.isArchived} newValue={newVersion.isArchived} />
            <DiffField label="Exposed to MCP" oldValue={oldVersion.exposedToMCP} newValue={newVersion.exposedToMCP} />
            <DiffField label="Execute as LLM" oldValue={oldVersion.execute_llm} newValue={newVersion.execute_llm} />
          </div>
        )}

        <div>
          <h3 className="text-sm font-semibold mb-2">Content Changes</h3>
          <div className="grid grid-cols-2 gap-4 h-[400px]">
            <ScrollArea className="h-full border rounded-lg">
              <pre className="p-4 text-xs font-mono whitespace-pre-wrap bg-muted/30">
                {oldVersion.content}
              </pre>
            </ScrollArea>

            <ScrollArea className="h-full border rounded-lg">
              <pre className="p-4 text-xs font-mono whitespace-pre-wrap bg-primary/5">
                {newVersion.content}
              </pre>
            </ScrollArea>
          </div>
        </div>

        <div className="flex items-center justify-center gap-4 text-xs text-muted-foreground">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-muted/30 border" />
            <span>Old Version</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-primary/5 border" />
            <span>New Version</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
