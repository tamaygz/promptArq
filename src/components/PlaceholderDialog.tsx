import { useState, useEffect } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Copy, Check, MagicWand } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { Placeholder, extractPlaceholders, replacePlaceholders } from '@/lib/placeholder-utils'
import { Card } from '@/components/ui/card'

type PlaceholderDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  content: string
}

export function PlaceholderDialog({ open, onOpenChange, content }: PlaceholderDialogProps) {
  const [placeholderNames, setPlaceholderNames] = useState<string[]>([])
  const [placeholderValues, setPlaceholderValues] = useState<Record<string, string>>({})
  const [generatedPrompt, setGeneratedPrompt] = useState('')
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (open) {
      const names = extractPlaceholders(content)
      setPlaceholderNames(names)
      
      const initialValues: Record<string, string> = {}
      names.forEach(name => {
        initialValues[name] = placeholderValues[name] || ''
      })
      setPlaceholderValues(initialValues)
      setGeneratedPrompt('')
      setCopied(false)
    }
  }, [open, content])

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

  const handleValueChange = (name: string, value: string) => {
    setPlaceholderValues(prev => ({
      ...prev,
      [name]: value
    }))
  }

  const allFilled = placeholderNames.every(name => placeholderValues[name]?.trim())

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[85vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Fill Placeholders</DialogTitle>
          <DialogDescription>
            Enter values for each placeholder to generate your prompt
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-hidden flex flex-col gap-6">
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

              <div className="flex items-center gap-3 pt-4 border-t">
                <Button
                  onClick={handleGenerate}
                  disabled={!allFilled}
                  className="flex-1"
                >
                  <MagicWand size={16} weight="bold" />
                  Generate Prompt
                </Button>
              </div>

              {generatedPrompt && (
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
              )}
            </>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
