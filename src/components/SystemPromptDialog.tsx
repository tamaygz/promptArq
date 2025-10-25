import { useState } from 'react'
import { SystemPrompt, Project, Category, Tag } from '@/lib/types'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Plus, Trash, FileCode } from '@phosphor-icons/react'
import { toast } from 'sonner'

type SystemPromptDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  systemPrompts: SystemPrompt[]
  projects: Project[]
  categories: Category[]
  tags: Tag[]
  onUpdate: (prompts: SystemPrompt[]) => void
}

export function SystemPromptDialog({
  open,
  onOpenChange,
  systemPrompts,
  projects,
  categories,
  tags,
  onUpdate,
}: SystemPromptDialogProps) {
  const [name, setName] = useState('')
  const [content, setContent] = useState('')
  const [scopeType, setScopeType] = useState<SystemPrompt['scopeType']>('team')
  const [scopeId, setScopeId] = useState<string>('')
  const [priority, setPriority] = useState(0)

  const handleAdd = () => {
    if (!name.trim()) {
      toast.error('Name is required')
      return
    }
    if (!content.trim()) {
      toast.error('Content is required')
      return
    }

    const newPrompt: SystemPrompt = {
      id: `system-prompt-${Date.now()}`,
      name: name.trim(),
      content: content.trim(),
      scopeType,
      scopeId: scopeType === 'team' ? undefined : scopeId || undefined,
      priority,
      createdBy: 'user',
      createdAt: Date.now(),
      updatedAt: Date.now(),
    }

    onUpdate([...systemPrompts, newPrompt])
    setName('')
    setContent('')
    setScopeType('team')
    setScopeId('')
    setPriority(0)
    toast.success('System prompt created')
  }

  const handleDelete = (id: string) => {
    onUpdate(systemPrompts.filter(sp => sp.id !== id))
    toast.success('System prompt deleted')
  }

  const getScopeName = (sp: SystemPrompt) => {
    if (sp.scopeType === 'team') return 'Team Default'
    if (sp.scopeType === 'project') {
      const project = projects.find(p => p.id === sp.scopeId)
      return project ? `Project: ${project.name}` : 'Project'
    }
    if (sp.scopeType === 'category') {
      const category = categories.find(c => c.id === sp.scopeId)
      return category ? `Category: ${category.name}` : 'Category'
    }
    if (sp.scopeType === 'tag') {
      const tag = tags.find(t => t.id === sp.scopeId)
      return tag ? `Tag: ${tag.name}` : 'Tag'
    }
    return sp.scopeType
  }

  const getAvailableScopes = () => {
    switch (scopeType) {
      case 'project':
        return projects.map(p => ({ id: p.id, name: p.name }))
      case 'category':
        return categories.map(c => ({ id: c.id, name: c.name }))
      case 'tag':
        return tags.map(t => ({ id: t.id, name: t.name }))
      default:
        return []
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl">
        <DialogHeader>
          <DialogTitle>System Prompts</DialogTitle>
          <DialogDescription>
            Configure system prompts that guide the AI when improving prompts. Precedence: Prompt {'>'} Project {'>'} Category {'>'} Tag {'>'} Team Default
          </DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-2 gap-6">
          <div className="space-y-4">
            <Card className="p-4">
              <div className="flex items-center gap-2 mb-4">
                <FileCode size={20} className="text-primary" />
                <h3 className="font-semibold">New System Prompt</h3>
              </div>
              
              <div className="flex flex-col gap-3">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="sp-name">Name</Label>
                  <Input
                    id="sp-name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="e.g., Email Generation Assistant"
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="sp-scope">Scope</Label>
                  <Select value={scopeType} onValueChange={(v) => {
                    setScopeType(v as SystemPrompt['scopeType'])
                    setScopeId('')
                  }}>
                    <SelectTrigger id="sp-scope">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="team">Team Default</SelectItem>
                      <SelectItem value="project">Project</SelectItem>
                      <SelectItem value="category">Category</SelectItem>
                      <SelectItem value="tag">Tag</SelectItem>
                      <SelectItem value="prompt">Specific Prompt</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {scopeType !== 'team' && scopeType !== 'prompt' && (
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="sp-target">Target</Label>
                    <Select value={scopeId} onValueChange={setScopeId}>
                      <SelectTrigger id="sp-target">
                        <SelectValue placeholder={`Select ${scopeType}`} />
                      </SelectTrigger>
                      <SelectContent>
                        {getAvailableScopes().map(item => (
                          <SelectItem key={item.id} value={item.id}>
                            {item.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                )}

                {scopeType === 'tag' && (
                  <div className="flex flex-col gap-2">
                    <Label htmlFor="sp-priority">Priority</Label>
                    <Input
                      id="sp-priority"
                      type="number"
                      value={priority}
                      onChange={(e) => setPriority(parseInt(e.target.value) || 0)}
                      placeholder="Higher = higher priority"
                    />
                  </div>
                )}

                <div className="flex flex-col gap-2">
                  <Label htmlFor="sp-content">System Prompt Content</Label>
                  <Textarea
                    id="sp-content"
                    value={content}
                    onChange={(e) => setContent(e.target.value)}
                    placeholder="You are an expert at..."
                    rows={8}
                    className="font-mono text-sm"
                  />
                </div>

                <Button onClick={handleAdd} className="w-full">
                  <Plus size={16} weight="bold" />
                  Create System Prompt
                </Button>
              </div>
            </Card>
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">Configured System Prompts</h3>
              <Badge variant="secondary">{systemPrompts.length}</Badge>
            </div>
            
            <ScrollArea className="h-[500px]">
              <div className="flex flex-col gap-3 pr-4">
                {systemPrompts.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground py-8">
                    No system prompts configured yet.
                  </div>
                ) : (
                  systemPrompts
                    .sort((a, b) => {
                      const scopeOrder = { prompt: 0, project: 1, category: 2, tag: 3, team: 4 }
                      return scopeOrder[a.scopeType] - scopeOrder[b.scopeType]
                    })
                    .map(sp => (
                      <Card key={sp.id} className="p-4">
                        <div className="flex items-start justify-between mb-2">
                          <div className="flex-1">
                            <h4 className="font-medium text-sm mb-1">{sp.name}</h4>
                            <Badge variant="outline" className="text-xs mb-2">
                              {getScopeName(sp)}
                            </Badge>
                            {sp.scopeType === 'tag' && sp.priority > 0 && (
                              <Badge variant="secondary" className="text-xs ml-1">
                                Priority: {sp.priority}
                              </Badge>
                            )}
                          </div>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDelete(sp.id)}
                          >
                            <Trash size={16} />
                          </Button>
                        </div>
                        <p className="text-xs text-muted-foreground font-mono line-clamp-3 bg-muted/50 p-2 rounded">
                          {sp.content}
                        </p>
                      </Card>
                    ))
                )}
              </div>
            </ScrollArea>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
