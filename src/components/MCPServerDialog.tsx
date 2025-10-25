import { useState, useMemo } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Prompt, Project, Category, Tag } from '@/lib/types'
import { Copy, Check, GitBranch } from '@phosphor-icons/react'
import { toast } from 'sonner'

type MCPServerDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  prompts: Prompt[]
  projects: Project[]
  categories: Category[]
  tags: Tag[]
}

export function MCPServerDialog({ open, onOpenChange, prompts, projects, categories, tags }: MCPServerDialogProps) {
  const [copiedEndpoint, setCopiedEndpoint] = useState(false)
  const [copiedConfig, setCopiedConfig] = useState(false)

  const exposedPrompts = prompts.filter(p => p.exposedToMCP && !p.isArchived)
  
  const promptsByProject = useMemo(() => {
    const grouped = new Map<string, Prompt[]>()
    
    exposedPrompts.forEach(prompt => {
      const projectId = prompt.projectId
      if (!grouped.has(projectId)) {
        grouped.set(projectId, [])
      }
      grouped.get(projectId)!.push(prompt)
    })
    
    return grouped
  }, [exposedPrompts])

  const mcpEndpoint = `${window.location.origin}/mcp`
  
  const mcpConfig = {
    mcpServers: {
      arqioly: {
        command: "npx",
        args: ["-y", "@arqioly/mcp-server"],
        env: {
          ARQIOLY_URL: window.location.origin,
          ARQIOLY_TOKEN: "your-api-token-here"
        }
      }
    }
  }

  const handleCopyEndpoint = () => {
    navigator.clipboard.writeText(mcpEndpoint)
    setCopiedEndpoint(true)
    toast.success('MCP endpoint copied to clipboard')
    setTimeout(() => setCopiedEndpoint(false), 2000)
  }

  const handleCopyConfig = () => {
    navigator.clipboard.writeText(JSON.stringify(mcpConfig, null, 2))
    setCopiedConfig(true)
    toast.success('MCP config copied to clipboard')
    setTimeout(() => setCopiedConfig(false), 2000)
  }

  const getCategoryName = (categoryId: string) => {
    return categories.find(c => c.id === categoryId)?.name || 'Uncategorized'
  }

  const getTagsForPrompt = (tagIds: string[]) => {
    return tags.filter(t => tagIds.includes(t.id))
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[85vh]">
        <DialogHeader>
          <DialogTitle className="text-2xl">MCP Server Configuration</DialogTitle>
          <DialogDescription>
            Expose prompts to AI agents via the Model Context Protocol
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-8">
          <div className="space-y-6">
            <div className="flex flex-col gap-3">
              <Label className="text-sm font-medium">MCP Server Endpoint</Label>
              <div className="flex gap-2">
                <Input 
                  value={mcpEndpoint} 
                  readOnly 
                  className="font-mono text-sm bg-muted/30"
                />
                <Button 
                  variant="outline" 
                  size="sm"
                  onClick={handleCopyEndpoint}
                >
                  {copiedEndpoint ? <Check size={16} /> : <Copy size={16} />}
                  {copiedEndpoint ? 'Copied' : 'Copy'}
                </Button>
              </div>
            </div>

            <div className="flex flex-col gap-3">
              <Label className="text-sm font-medium">Claude Desktop Configuration</Label>
              <div className="relative">
                <pre className="bg-muted/30 p-5 rounded-lg text-xs font-mono overflow-x-auto border">
                  {JSON.stringify(mcpConfig, null, 2)}
                </pre>
                <Button 
                  variant="outline" 
                  size="sm"
                  className="absolute top-3 right-3"
                  onClick={handleCopyConfig}
                >
                  {copiedConfig ? <Check size={14} /> : <Copy size={14} />}
                  {copiedConfig ? 'Copied' : 'Copy'}
                </Button>
              </div>
              <p className="text-xs text-muted-foreground">
                Add this configuration to your <code className="bg-muted px-1.5 py-0.5 rounded">claude_desktop_config.json</code> file
              </p>
            </div>
          </div>

          <Separator />

          <div className="flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold">
                Exposed Prompts ({exposedPrompts.length})
              </h3>
              {exposedPrompts.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  No prompts exposed yet. Enable MCP in the prompt editor.
                </p>
              )}
            </div>

            {exposedPrompts.length > 0 && (
              <ScrollArea className="h-[280px] pr-4">
                <div className="flex flex-col gap-8">
                  {Array.from(promptsByProject.entries()).map(([projectId, projectPrompts]) => {
                    const project = projects.find(p => p.id === projectId)
                    if (!project) return null

                    return (
                      <div key={projectId} className="flex flex-col gap-4">
                        <div className="flex items-center gap-3">
                          <div 
                            className="w-3 h-3 rounded-full" 
                            style={{ backgroundColor: project.color }}
                          />
                          <h4 className="font-semibold text-base">{project.name}</h4>
                          <Badge variant="secondary" className="text-xs">
                            {projectPrompts.length} {projectPrompts.length === 1 ? 'prompt' : 'prompts'}
                          </Badge>
                        </div>
                        
                        <div className="flex flex-col gap-3 ml-6">
                          {projectPrompts.map(prompt => (
                            <Card key={prompt.id} className="p-4 hover:bg-muted/50 transition-colors">
                              <div className="flex flex-col gap-2.5">
                                <div className="flex items-start justify-between gap-3">
                                  <div className="flex-1">
                                    <h5 className="font-medium text-sm">{prompt.title}</h5>
                                    {prompt.description && (
                                      <p className="text-xs text-muted-foreground mt-1.5">
                                        {prompt.description}
                                      </p>
                                    )}
                                  </div>
                                  <Badge variant="outline" className="text-xs shrink-0">
                                    {getCategoryName(prompt.categoryId)}
                                  </Badge>
                                </div>
                                
                                {prompt.tags.length > 0 && (
                                  <div className="flex flex-wrap gap-1.5">
                                    {getTagsForPrompt(prompt.tags).map(tag => (
                                      <Badge
                                        key={tag.id}
                                        variant="outline"
                                        className="text-xs px-2 py-0.5"
                                        style={{
                                          borderColor: tag.color,
                                          color: tag.color
                                        }}
                                      >
                                        {tag.name}
                                      </Badge>
                                    ))}
                                  </div>
                                )}
                              </div>
                            </Card>
                          ))}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </ScrollArea>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
