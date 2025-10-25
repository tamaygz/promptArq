import { useState, useEffect } from 'react'
import { useKV } from '@github/spark/hooks'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Copy, Check, MagicWand, Play, Info } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { Placeholder, extractPlaceholders, replacePlaceholders } from '@/lib/placeholder-utils'
import { Card } from '@/components/ui/card'
import { Prompt, Project, Category, Tag, SystemPrompt } from '@/lib/types'
import { resolveSystemPrompt } from '@/lib/prompt-resolver'
import { Separator } from '@/components/ui/separator'

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
  const [savedPlaceholderValues, setSavedPlaceholderValues] = useKV<Record<string, string>>('placeholder-values', {})
  const [placeholderNames, setPlaceholderNames] = useState<string[]>([])
  const [placeholderValues, setPlaceholderValues] = useState<Record<string, string>>({})
  const [generatedPrompt, setGeneratedPrompt] = useState('')
  const [copied, setCopied] = useState(false)
  const [executing, setExecuting] = useState(false)
  const [executionResult, setExecutionResult] = useState('')
  const [usedSystemPrompt, setUsedSystemPrompt] = useState('')
  const [resultCopied, setResultCopied] = useState(false)
  const [showExecutionDialog, setShowExecutionDialog] = useState(false)

  useEffect(() => {
    if (open) {
      const names = extractPlaceholders(content)
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
    }
  }, [open, content, savedPlaceholderValues])

  const handleGenerate = () => {
    const placeholders: Placeholder[] = placeholderNames.map(name => ({
      name,
      value: placeholderValues[name] || ''
    }))

    const result = replacePlaceholders(content, placeholders)
    setGeneratedPrompt(result)
    toast.success('Prompt generated successfully')
  }

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
      toast.error('Generate the prompt first')
      return
    }

    setExecuting(true)
    setExecutionResult('')
    setUsedSystemPrompt('')

    try {
      const systemPromptText = resolveSystemPrompt(
        prompt,
        project,
        category,
        tags,
        systemPrompts
      )

      setUsedSystemPrompt(systemPromptText)

      const executionPrompt = window.spark.llmPrompt`${systemPromptText}

${generatedPrompt}`

      const result = await window.spark.llm(executionPrompt, 'gpt-4o-mini')
      
      setExecutionResult(result.trim())
      setShowExecutionDialog(true)
      toast.success('Prompt executed successfully')
    } catch (error) {
      toast.error('Failed to execute prompt')
      console.error(error)
      setExecutionResult('Error: Failed to execute prompt. Please try again.')
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

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-5xl w-[90vw] max-h-[90vh] flex flex-col">
          <DialogHeader>
            <DialogTitle>Fill Placeholders</DialogTitle>
            <DialogDescription>
              Enter values for each placeholder to generate your prompt
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-6 overflow-hidden flex-1 min-h-0">
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
                <ScrollArea className="flex-1 pr-4">
                  <div className="space-y-5">
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
                </ScrollArea>

                <div className="flex items-center gap-3 pt-4 border-t shrink-0">
                  <Button
                    onClick={handleGenerate}
                    disabled={!allFilled}
                    className="flex-1"
                  >
                    <MagicWand size={16} weight="bold" />
                    Generate Prompt
                  </Button>
                  {generatedPrompt && (
                    <Button
                      onClick={handleExecute}
                      disabled={executing}
                      variant="secondary"
                      className="flex-1"
                    >
                      <Play size={16} weight={executing ? "fill" : "bold"} />
                      {executing ? 'Executing...' : 'Execute'}
                    </Button>
                  )}
                </div>

                {generatedPrompt && (
                  <Card className="p-6 space-y-4 border-2 border-primary/20 shrink-0">
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
                )}
              </>
            )}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={showExecutionDialog} onOpenChange={setShowExecutionDialog}>
        <DialogContent className="max-w-4xl w-[90vw] max-h-[90vh] flex flex-col">
          <DialogHeader>
            <DialogTitle>Execution Result</DialogTitle>
            <DialogDescription>
              Result from executing your prompt with the LLM
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4 overflow-hidden flex-1 min-h-0">
            {usedSystemPrompt && (
              <div className="flex items-start gap-2 p-4 bg-muted/50 rounded-md border border-border shrink-0">
                <Info size={16} className="text-primary shrink-0 mt-0.5" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-foreground mb-2">System Prompt Used:</p>
                  <ScrollArea className="max-h-32">
                    <p className="text-xs text-muted-foreground break-words pr-4">
                      {usedSystemPrompt}
                    </p>
                  </ScrollArea>
                </div>
              </div>
            )}

            <Separator className="shrink-0" />
            
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
              
              <ScrollArea className="flex-1 w-full rounded-md border p-4">
                <pre className="text-sm whitespace-pre-wrap break-words">
                  {executionResult}
                </pre>
              </ScrollArea>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
