import { useState, useEffect } from 'react'
import { useStorage } from '@/hooks/use-storage'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { hasLLMFeatures } from '@/lib/spark-gateway'
import { createLLMPrompt, executeLLM, hasLLMSupport } from '@/lib/spark-utils'
import { isSparkEnvironment } from '@/lib/storage-adapter'
import { hasGitHubModelsSupport } from '@/lib/github-models-client'
import { initiateGitHubLogin } from '@/lib/github-auth'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Copy, Check, MagicWand, Play, Info, CaretDown, CaretRight } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { Placeholder, extractPlaceholders, replacePlaceholders, replaceProjectVariables } from '@/lib/placeholder-utils'
import { Card } from '@/components/ui/card'
import { Prompt, Project, Category, Tag, SystemPrompt } from '@/lib/types'
import { resolveSystemPrompt } from '@/lib/prompt-resolver'
import { Separator } from '@/components/ui/separator'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'

type PlaceholderDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  content: string
  prompt?: Prompt
  project?: Project
  category?: Category
  tags?: Tag[]
  systemPrompts?: SystemPrompt[]
}

export function PlaceholderDialog({ open, onOpenChange, content, prompt, project, category, tags = [], systemPrompts = [] }: PlaceholderDialogProps) {
  const [savedPlaceholderValues, setSavedPlaceholderValues] = useStorage<Record<string, string>>('placeholder-values', {})
  const [placeholderNames, setPlaceholderNames] = useState<string[]>([])
  const [placeholderValues, setPlaceholderValues] = useState<Record<string, string>>({})
  const [generatedPrompt, setGeneratedPrompt] = useState('')
  const [copied, setCopied] = useState(false)
  const [executing, setExecuting] = useState(false)
  const [executionResult, setExecutionResult] = useState('')
  const [usedSystemPrompt, setUsedSystemPrompt] = useState('')
  const [resultCopied, setResultCopied] = useState(false)
  const [showExecutionDialog, setShowExecutionDialog] = useState(false)
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
      // First replace project variables in content
      const contentWithProjectVars = replaceProjectVariables(content, project?.variables || {})
      const names = extractPlaceholders(contentWithProjectVars)
      setPlaceholderNames(names)
      
      const initialValues: Record<string, string> = {}
      const saved = savedPlaceholderValues || {}
      names.forEach(name => {
        initialValues[name] = saved[name] || ''
      })
      setPlaceholderValues(initialValues)
      setGeneratedPrompt('')
      setCopied(false)
      setExecuting(false)
      setExecutionResult('')
      setUsedSystemPrompt('')
      setResultCopied(false)
      setShowExecutionDialog(false)
      
      const computedPromptId = getComputedSystemPromptId()
      setComputedSystemPromptId(computedPromptId)
      setSelectedSystemPromptId(computedPromptId)
    }
  }, [open, content, savedPlaceholderValues, prompt, project, category, tags, systemPrompts])

  // Auto-generate prompt whenever placeholder values change
  useEffect(() => {
    if (open && placeholderNames.length > 0) {
      // First replace project variables in content
      const contentWithProjectVars = replaceProjectVariables(content, project?.variables || {})
      const placeholders: Placeholder[] = placeholderNames.map(name => ({
        name,
        value: placeholderValues[name] || ''
      }))
      const result = replacePlaceholders(contentWithProjectVars, placeholders)
      setGeneratedPrompt(result)
    }
  }, [placeholderValues, placeholderNames, content, open, project])

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(generatedPrompt)
      setCopied(true)
      toast.success('Copied to clipboard')
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      toast.error('Failed to copy to clipboard')
    }
  }

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
    if (!generatedPrompt) {
      toast.error('Fill all placeholders first')
      return
    }

    // Check if any LLM service is available
    if (!hasLLMSupport()) {
      toast.error('AI features require either Spark environment or GitHub authentication', {
        action: !isSparkEnvironment() && !hasGitHubModelsSupport() ? {
          label: 'Log in',
          onClick: () => initiateGitHubLogin()
        } : undefined
      })
      return
    }

    setExecuting(true)
    setExecutionResult('')
    setUsedSystemPrompt('')

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

      setUsedSystemPrompt(systemPromptText)

      const executionPrompt = systemPromptText 
        ? createLLMPrompt`${systemPromptText}

${generatedPrompt}`
        : createLLMPrompt`${generatedPrompt}`

      const result = await executeLLM(executionPrompt, 'gpt-4o-mini', false)
      
      if (!result) {
        throw new Error('No response from AI service')
      }
      
      setExecutionResult(result.trim())
      setShowExecutionDialog(true)
      toast.success('Prompt executed successfully')
    } catch (error: any) {
      const errorMessage = error?.message || 'Failed to execute prompt'
      toast.error(errorMessage, {
        action: error?.message?.includes('authentication') ? {
          label: 'Log in',
          onClick: () => initiateGitHubLogin()
        } : undefined
      })
      console.error(error)
      setExecutionResult(`Error: ${errorMessage}. Please try again.`)
      setShowExecutionDialog(true)
    } finally {
      setExecuting(false)
    }
  }

  const handleValueChange = (name: string, value: string) => {
    setPlaceholderValues(prev => ({
      ...prev,
      [name]: value
    }))
    
    setSavedPlaceholderValues(current => ({
      ...(current || {}),
      [name]: value
    }))
  }

  const allFilled = placeholderNames.every(name => placeholderValues[name]?.trim())

  const getSystemPromptLabel = (id: string): string => {
    if (id === 'none') return 'None'
    if (id === 'default') return 'Default'
    const sp = systemPrompts.find(s => s.id === id)
    return sp?.name || 'Unknown'
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-6xl w-[90vw] max-h-[90vh] flex flex-col overflow-hidden">
          <DialogHeader className="shrink-0">
            <DialogTitle>Fill Placeholders</DialogTitle>
            <DialogDescription>
              Enter values for each placeholder to generate your prompt
            </DialogDescription>
          </DialogHeader>

          <ScrollArea className="flex-1 -mx-8 px-8">
            <div className="flex flex-col gap-6 pb-2">
              {placeholderNames.length === 0 ? (
                <div className="flex items-center justify-center py-16 text-center">
                  <div>
                    <p className="text-muted-foreground mb-2">No placeholders found in this prompt</p>
                    <p className="text-sm text-muted-foreground">
                      Add placeholders using <code className="bg-muted px-2 py-1 rounded text-xs">
                        {'{{placeholder_name}}'}
                      </code> syntax
                    </p>
                  </div>
                </div>
              ) : (
                <>
                  <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                    {placeholderNames.map((name, index) => (
                      <div key={name} className="flex flex-col gap-2.5">
                        <Label htmlFor={`placeholder-${index}`} className="text-sm font-medium">
                          {name}
                        </Label>
                        <Textarea
                          id={`placeholder-${index}`}
                          value={placeholderValues[name] || ''}
                          onChange={(e) => handleValueChange(name, e.target.value)}
                          placeholder={`Enter value for ${name}...`}
                          rows={3}
                          className="text-sm"
                        />
                      </div>
                    ))}
                  </div>

                  <div className="flex flex-col gap-4 pt-4 border-t">
                    <div className="flex items-end gap-3">
                      <div className="flex-1 flex flex-col gap-2.5">
                        <Label htmlFor="system-prompt-select" className="text-sm font-medium">
                          System Prompt for Execution
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
                        disabled={executing || !allFilled || !generatedPrompt}
                        className="h-11 px-8"
                      >
                        <Play size={16} weight={executing ? "fill" : "bold"} />
                        {executing ? 'Executing...' : 'Execute'}
                      </Button>
                    </div>
                  </div>

                  <Card className="p-6 space-y-4 border-2 border-primary/20">
                      <div className="flex items-center justify-between">
                        <Label className="text-sm font-semibold">Generated Prompt</Label>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={handleCopy}
                          className="gap-2"
                        >
                          {copied ? (
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
                      <ScrollArea className="h-64 w-full rounded-md border p-4">
                        <pre className="text-sm font-mono whitespace-pre-wrap break-words">
                          {generatedPrompt}
                        </pre>
                      </ScrollArea>
                    </Card>
                </>
              )}
            </div>
          </ScrollArea>
        </DialogContent>
      </Dialog>

      <Dialog open={showExecutionDialog} onOpenChange={setShowExecutionDialog}>
        <DialogContent className="max-w-5xl w-[90vw] max-h-[90vh] flex flex-col overflow-hidden">
          <DialogHeader className="shrink-0">
            <DialogTitle>Execution Result</DialogTitle>
            <DialogDescription>
              Result from executing your prompt with the LLM
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4 flex-1 min-h-0">
            <div className="flex flex-col gap-4 flex-1 min-h-0">
              <div className="flex items-center justify-between shrink-0">
                <Label className="text-sm font-semibold">Response</Label>
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
              
              <ScrollArea className="flex-1 rounded-md border bg-background">
                <div className="p-4">
                  <pre className="text-sm whitespace-pre-wrap break-words">
                    {executionResult}
                  </pre>
                </div>
              </ScrollArea>
            </div>

            {usedSystemPrompt && (
              <Collapsible defaultOpen={false} className="border-t pt-4 shrink-0">
                <CollapsibleTrigger asChild>
                  <Button variant="ghost" className="w-full justify-between p-2 h-auto hover:bg-muted/50">
                    <div className="flex items-center gap-2">
                      <Info size={16} className="text-primary" />
                      <Label className="text-sm font-medium cursor-pointer">System Prompt Used</Label>
                    </div>
                    <CaretDown size={16} className="text-muted-foreground transition-transform duration-200 [[data-state=closed]_&]:rotate-[-90deg]" />
                  </Button>
                </CollapsibleTrigger>
                <CollapsibleContent className="pt-3">
                  <div className="rounded-md border p-4 bg-muted/30">
                    <pre className="text-xs text-muted-foreground whitespace-pre-wrap break-words">
                      {usedSystemPrompt}
                    </pre>
                  </div>
                </CollapsibleContent>
              </Collapsible>
            )}
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
