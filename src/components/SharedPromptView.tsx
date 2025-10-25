import { useEffect, useState } from 'react'
import { useKV } from '@github/spark/hooks'
import { Prompt, PromptVersion, Project, Category, Tag, SharedPrompt } from '@/lib/types'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Copy, Check, X } from '@phosphor-icons/react'
import { toast } from 'sonner'
import logoIcon from '@/assets/images/logo_icon_boxed.png'

type SharedPromptViewProps = {
  shareToken: string
  onClose: () => void
}

export function SharedPromptView({ shareToken, onClose }: SharedPromptViewProps) {
  const [prompts] = useKV<Prompt[]>('prompts', [])
  const [versions] = useKV<PromptVersion[]>('prompt-versions', [])
  const [projects] = useKV<Project[]>('projects', [])
  const [categories] = useKV<Category[]>('categories', [])
  const [tags] = useKV<Tag[]>('tags', [])
  const [sharedPrompts] = useKV<SharedPrompt[]>('shared-prompts', [])
  
  const [copied, setCopied] = useState(false)

  const sharedPrompt = sharedPrompts?.find(sp => sp.shareToken === shareToken)
  const prompt = prompts?.find(p => p.id === sharedPrompt?.promptId)
  
  const project = projects?.find(p => p.id === prompt?.projectId)
  const category = categories?.find(c => c.id === prompt?.categoryId)
  const promptTags = tags?.filter(t => prompt?.tags.includes(t.id)) || []
  const promptVersions = versions?.filter(v => v.promptId === prompt?.id) || []
  const latestVersion = promptVersions[promptVersions.length - 1]

  const handleCopyContent = () => {
    if (prompt) {
      navigator.clipboard.writeText(prompt.content)
      setCopied(true)
      toast.success('Prompt content copied to clipboard')
      setTimeout(() => setCopied(false), 2000)
    }
  }

  if (!sharedPrompt || !prompt) {
    return (
      <div className="h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <img src={logoIcon} alt="arqioly logo" className="w-16 h-16 rounded-2xl mx-auto mb-4" />
          <h2 className="text-xl font-semibold mb-2">Prompt Not Found</h2>
          <p className="text-muted-foreground mb-4">
            This shared prompt link is invalid or has expired.
          </p>
          <Button onClick={onClose}>Go Back</Button>
        </div>
      </div>
    )
  }

  const formatDate = (timestamp: number) => {
    return new Date(timestamp).toLocaleString()
  }

  return (
    <div className="h-screen flex flex-col bg-background">
      <header className="border-b border-border bg-card px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <img src={logoIcon} alt="arqioly logo" className="w-8 h-8 rounded-lg" />
            <h1 className="text-xl font-semibold tracking-tight">arqioly</h1>
            <Badge variant="outline" className="ml-2">Shared View</Badge>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose}>
            <X size={20} />
            Close
          </Button>
        </div>
      </header>

      <ScrollArea className="flex-1">
        <div className="max-w-4xl mx-auto p-8">
          <div className="flex flex-col gap-6">
            <div>
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h1 className="text-3xl font-bold mb-2">{prompt.title}</h1>
                  <p className="text-muted-foreground">{prompt.description}</p>
                </div>
              </div>

              <div className="flex flex-wrap gap-3 mb-6">
                {project && (
                  <Badge variant="outline" style={{ borderColor: project.color, color: project.color }}>
                    {project.name}
                  </Badge>
                )}
                {category && (
                  <Badge variant="secondary">
                    {category.name}
                  </Badge>
                )}
                {promptTags.map(tag => (
                  <Badge
                    key={tag.id}
                    variant="outline"
                    style={{ borderColor: tag.color, color: tag.color }}
                  >
                    {tag.name}
                  </Badge>
                ))}
              </div>
            </div>

            <Separator />

            <div>
              <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-semibold">Prompt Content</h2>
                <Button size="sm" variant="outline" onClick={handleCopyContent}>
                  {copied ? <Check size={16} /> : <Copy size={16} />}
                  {copied ? 'Copied!' : 'Copy Content'}
                </Button>
              </div>
              <Card className="p-4">
                <pre className="font-mono text-sm whitespace-pre-wrap break-words">
                  {prompt.content}
                </pre>
              </Card>
            </div>

            {latestVersion && (
              <div className="text-sm text-muted-foreground">
                <div className="flex items-center gap-2">
                  <span>Version {latestVersion.versionNumber}</span>
                  <span>•</span>
                  <span>Last updated {formatDate(prompt.updatedAt)}</span>
                  <span>•</span>
                  <span>by {prompt.createdBy}</span>
                </div>
                {latestVersion.changeNote && (
                  <div className="mt-1">
                    Change: {latestVersion.changeNote}
                  </div>
                )}
              </div>
            )}

            {promptVersions.length > 0 && (
              <>
                <Separator />
                <div>
                  <h2 className="text-lg font-semibold mb-3">Version History</h2>
                  <div className="flex flex-col gap-2">
                    {promptVersions.slice().reverse().map(version => (
                      <Card key={version.id} className="p-3">
                        <div className="flex items-center justify-between mb-1">
                          <Badge variant="secondary" className="text-xs">
                            v{version.versionNumber}
                          </Badge>
                          <span className="text-xs text-muted-foreground">
                            {formatDate(version.createdAt)}
                          </span>
                        </div>
                        <p className="text-sm">{version.changeNote}</p>
                        <p className="text-xs text-muted-foreground mt-1">
                          by {version.createdBy}
                        </p>
                      </Card>
                    ))}
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </ScrollArea>
    </div>
  )
}
