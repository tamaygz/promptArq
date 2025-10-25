import { useState, useEffect } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Copy, Check, Play } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { Prompt, Project, Category, Tag, SystemPrompt } from '@/lib/types'
import { resolveSystemPrompt } from '@/lib/prompt-resolver'

type ExecuteDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  content: string
  prompt?: Prompt
  project?: Project
  category?: Category
  tags?: Tag[]
  systemPrompts?: SystemPrompt[]
}

export function ExecuteDialog({ open, onOpenChange, content, prompt, project, category, tags = [], systemPrompts = [] }: ExecuteDialogProps) {
  const [executing, setExecuting] = useState(false)
  const [executionResult, setExecutionResult] = useState('')
  const [resultCopied, setResultCopied] = useState(false)
  const [selectedSystemPromptId, setSelectedSystemPromptId] = useState<string>('')
  const [computedSystemPromptId, setComputedSystemPromptId] = useState<string>('')

  const getComputedSystemPromptId = (): string => {
    if (!prompt) return 'none'

    const promptOverride = systemPrompts.find(
      sp => sp.scopeType === 'prompt' && sp.scopeId === prompt.id
    )
    if (promptOverride) return promptOverride.id

    if (project) {
      const projectPrompt = systemPrompts.find(
        sp => sp.scopeType === 'project' && sp.scopeId === project.id
      )
      if (projectPrompt) return projectPrompt.id
    }

    if (category) {
      const categoryPrompt = systemPrompts.find(
        sp => sp.scopeType === 'category' && sp.scopeId === category.id
      )
      if (categoryPrompt) return categoryPrompt.id
    }

    if (tags.length > 0) {
      const tagPrompts = systemPrompts
        .filter(sp => sp.scopeType === 'tag' && tags.some(t => t.id === sp.scopeId))
        .sort((a, b) => b.priority - a.priority || b.createdAt - a.createdAt)
      
      if (tagPrompts.length > 0) return tagPrompts[0].id
    }

    const teamPrompt = systemPrompts.find(sp => sp.scopeType === 'team' && !sp.scopeId)
    if (teamPrompt) return teamPrompt.id

    return 'default'
  }

  useEffect(() => {
    if (open) {
      setExecuting(false)
      setExecutionResult('')
      setResultCopied(false)
      
      const computedPromptId = getComputedSystemPromptId()
      setComputedSystemPromptId(computedPromptId)
      setSelectedSystemPromptId(computedPromptId)
    }
  }, [open, prompt, project, category, tags, systemPrompts])

  const handleCopyResult = async () => {
    try {
      await navigator.clipboard.writeText(executionResult)
      setResultCopied(true)
      toast.success('Result copied to clipboard')
      setTimeout(() => setResultCopied(false), 2000)
    } catch (err) {
      toast.error('Failed to copy result to clipboard')
    }
  }

  const handleExecute = async () => {
    if (!content.trim()) {
      toast.error('No content to execute')
      return
    }

    setExecuting(true)
    setExecutionResult('')

    try {
      let systemPromptText = ''
      
      if (selectedSystemPromptId === 'none') {
        systemPromptText = ''
      } else if (selectedSystemPromptId === 'default') {
        systemPromptText = resolveSystemPrompt(
          prompt,
          project,
          category,
          tags,
          systemPrompts
        )
      } else {
        const selectedPrompt = systemPrompts.find(sp => sp.id === selectedSystemPromptId)
        if (selectedPrompt) {
          systemPromptText = selectedPrompt.content
        } else {
          systemPromptText = resolveSystemPrompt(
            prompt,
            project,
            category,
            tags,
            systemPrompts
          )
        }
      }

      const executionPrompt = systemPromptText 
        ? window.spark.llmPrompt`${systemPromptText}

${content}`
        : window.spark.llmPrompt`${content}`

      const result = await window.spark.llm(executionPrompt, 'gpt-4o-mini')
      
      setExecutionResult(result.trim())
      toast.success('Prompt executed successfully')
    } catch (error) {
      toast.error('Failed to execute prompt')
      console.error(error)
      setExecutionResult('Error: Failed to execute prompt. Please try again.')
    } finally {
      setExecuting(false)
    }
  }

  const getSystemPromptLabel = (id: string): string => {
    if (id === 'none') return 'None'
    if (id === 'default') return 'Default'
    const sp = systemPrompts.find(s => s.id === id)
    return sp?.name || 'Unknown'
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl w-[90vw] max-h-[90vh] flex flex-col overflow-hidden">
        <DialogHeader className="shrink-0">
          <DialogTitle>Execute Prompt</DialogTitle>
          <DialogDescription>
            Execute your prompt with the LLM
          </DialogDescription>
        </DialogHeader>

        <ScrollArea className="flex-1 -mx-6 px-6">
          <div className="flex flex-col gap-5 pb-2">
            <div className="flex flex-col gap-2.5">
              <Label htmlFor="system-prompt-select" className="text-sm font-medium">
                System Prompt
              </Label>
              <Select
                value={selectedSystemPromptId}
                onValueChange={setSelectedSystemPromptId}
              >
                <SelectTrigger id="system-prompt-select" className="h-11">
                  <SelectValue>
                    {selectedSystemPromptId === computedSystemPromptId && selectedSystemPromptId !== 'none' && selectedSystemPromptId !== 'default' && (
                      <span>{getSystemPromptLabel(selectedSystemPromptId)} <span className="text-xs text-muted-foreground">(computed)</span></span>
                    )}
                    {(selectedSystemPromptId !== computedSystemPromptId || selectedSystemPromptId === 'none' || selectedSystemPromptId === 'default') && (
                      <span>{getSystemPromptLabel(selectedSystemPromptId)}</span>
                    )}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">None</SelectItem>
                  <SelectItem value="default">Default</SelectItem>
                  {systemPrompts.map(sp => (
                    <SelectItem key={sp.id} value={sp.id}>
                      {sp.name}
                      {sp.id === computedSystemPromptId && (
                        <span className="text-xs text-muted-foreground ml-1.5">(computed)</span>
                      )}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <Button
              onClick={handleExecute}
              disabled={executing}
              className="w-full"
            >
              <Play size={16} weight={executing ? "fill" : "bold"} />
              {executing ? 'Executing...' : 'Execute'}
            </Button>

            {executionResult && (
              <div className="flex flex-col gap-4 pt-4 border-t">
                <div className="flex items-center justify-between">
                  <Label className="text-sm font-semibold">Result</Label>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={handleCopyResult}
                    className="gap-2"
                  >
                    {resultCopied ? (
                      <>
                        <Check size={14} weight="bold" />
                        Copied
                      </>
                    ) : (
                      <>
                        <Copy size={14} />
                        Copy
                      </>
                    )}
                  </Button>
                </div>
                
                <div className="rounded-md border p-4 bg-background">
                  <pre className="text-sm whitespace-pre-wrap break-words">
                    {executionResult}
                  </pre>
                </div>
              </div>
            )}
          </div>
        </ScrollArea>
      </DialogContent>
    </Dialog>
  )
}
