import { useState, useEffect } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Slider } from '@/components/ui/slider'
import { ScrollArea } from '@/components/ui/scroll-area'
import { ModelConfig, Project, Category, Tag } from '@/lib/types'
import { Plus, Trash, Cpu, Lightning } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'

type ModelConfigDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  modelConfigs: ModelConfig[]
  projects: Project[]
  categories: Category[]
  tags: Tag[]
  onUpdate: (configs: ModelConfig[] | ((current: ModelConfig[]) => ModelConfig[])) => void
}

export function ModelConfigDialog({
  open,
  onOpenChange,
  modelConfigs,
  projects,
  categories,
  tags,
  onUpdate
}: ModelConfigDialogProps) {
  const [user, setUser] = useState<any>(null)
  const [editingConfig, setEditingConfig] = useState<ModelConfig | null>(null)
  const [isCreating, setIsCreating] = useState(false)

  const [name, setName] = useState('')
  const [provider, setProvider] = useState<'openai' | 'anthropic' | 'azure'>('openai')
  const [modelName, setModelName] = useState('gpt-4o-mini')
  const [temperature, setTemperature] = useState(0.7)
  const [maxTokens, setMaxTokens] = useState(2000)
  const [scopeType, setScopeType] = useState<'team' | 'project' | 'category' | 'tag' | 'prompt'>('team')
  const [scopeId, setScopeId] = useState('')

  useEffect(() => {
    window.spark.user().then(setUser)
  }, [])

  useEffect(() => {
    if (editingConfig) {
      setName(editingConfig.name)
      setProvider(editingConfig.provider)
      setModelName(editingConfig.modelName)
      setTemperature(editingConfig.temperature)
      setMaxTokens(editingConfig.maxTokens)
      setScopeType(editingConfig.scopeType)
      setScopeId(editingConfig.scopeId || '')
    } else {
      resetForm()
    }
  }, [editingConfig])

  const resetForm = () => {
    setName('')
    setProvider('openai')
    setModelName('gpt-4o-mini')
    setTemperature(0.7)
    setMaxTokens(2000)
    setScopeType('team')
    setScopeId('')
  }

  const handleCreate = () => {
    setIsCreating(true)
    setEditingConfig(null)
    resetForm()
  }

  const handleEdit = (config: ModelConfig) => {
    setEditingConfig(config)
    setIsCreating(false)
  }

  const handleDelete = (configId: string) => {
    onUpdate((current) => current.filter(c => c.id !== configId))
    toast.success('Model configuration deleted')
  }

  const handleSave = () => {
    if (!name.trim()) {
      toast.error('Configuration name is required')
      return
    }

    if (scopeType !== 'team' && !scopeId) {
      toast.error(`Please select a ${scopeType}`)
      return
    }

    const newConfig: ModelConfig = {
      id: editingConfig?.id || `config-${Date.now()}`,
      name: name.trim(),
      provider,
      modelName,
      temperature,
      maxTokens,
      scopeType,
      scopeId: scopeType === 'team' ? undefined : scopeId,
      createdBy: user?.login || 'unknown',
      createdAt: editingConfig?.createdAt || Date.now()
    }

    if (editingConfig) {
      onUpdate((current) => current.map(c => c.id === editingConfig.id ? newConfig : c))
      toast.success('Model configuration updated')
    } else {
      onUpdate((current) => [...current, newConfig])
      toast.success('Model configuration created')
    }

    setIsCreating(false)
    setEditingConfig(null)
    resetForm()
  }

  const handleCancel = () => {
    setIsCreating(false)
    setEditingConfig(null)
    resetForm()
  }

  const getProviderModels = (provider: string) => {
    switch (provider) {
      case 'openai':
        return ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'gpt-3.5-turbo']
      case 'anthropic':
        return ['claude-3-5-sonnet-20241022', 'claude-3-opus-20240229', 'claude-3-sonnet-20240229', 'claude-3-haiku-20240307']
      case 'azure':
        return ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'gpt-35-turbo']
      default:
        return []
    }
  }

  const getScopeLabel = (config: ModelConfig) => {
    if (config.scopeType === 'team') return 'Team Default'
    if (config.scopeType === 'project') {
      const project = projects.find(p => p.id === config.scopeId)
      return project ? `Project: ${project.name}` : 'Unknown Project'
    }
    if (config.scopeType === 'category') {
      const category = categories.find(c => c.id === config.scopeId)
      return category ? `Category: ${category.name}` : 'Unknown Category'
    }
    if (config.scopeType === 'tag') {
      const tag = tags.find(t => t.id === config.scopeId)
      return tag ? `Tag: ${tag.name}` : 'Unknown Tag'
    }
    return 'Prompt Specific'
  }

  const getScopeOptions = () => {
    switch (scopeType) {
      case 'project':
        return projects.map(p => ({ value: p.id, label: p.name }))
      case 'category':
        return categories.map(c => ({ value: c.id, label: c.name }))
      case 'tag':
        return tags.map(t => ({ value: t.id, label: t.name }))
      default:
        return []
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl w-[80vw] max-h-[85vh] flex flex-col overflow-hidden">
        <DialogHeader className="shrink-0">
          <DialogTitle className="flex items-center gap-2">
            <Cpu size={20} />
            Model Provider Configuration
          </DialogTitle>
          <DialogDescription>
            Configure LLM providers and models for the "Improve Prompt" feature. Settings can be scoped to teams, projects, categories, or tags.
          </DialogDescription>
        </DialogHeader>

        <div className="flex gap-6 flex-1 min-h-0 overflow-hidden">
          <div className="w-1/2 flex flex-col">
            <div className="flex items-center justify-between mb-4">
              <h3 className="font-semibold">Configurations</h3>
              <Button size="sm" onClick={handleCreate} disabled={isCreating || editingConfig !== null}>
                <Plus size={16} />
                New Config
              </Button>
            </div>

            <ScrollArea className="flex-1 pr-4">
              {modelConfigs.length === 0 && !isCreating && (
                <div className="text-center py-12 text-muted-foreground text-sm">
                  No model configurations yet. Create one to customize the "Improve Prompt" feature.
                </div>
              )}

              <div className="space-y-3">
                {modelConfigs.map(config => (
                  <Card
                    key={config.id}
                    className={cn(
                      'p-4 cursor-pointer transition-all hover:shadow-md',
                      editingConfig?.id === config.id && 'ring-2 ring-primary'
                    )}
                    onClick={() => handleEdit(config)}
                  >
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex-1">
                        <h4 className="font-medium mb-1">{config.name}</h4>
                        <div className="flex items-center gap-2 mb-2">
                          <Badge variant="outline" className="text-xs">
                            {config.provider}
                          </Badge>
                          <Badge variant="secondary" className="text-xs">
                            {config.modelName}
                          </Badge>
                        </div>
                        <p className="text-xs text-muted-foreground">
                          {getScopeLabel(config)}
                        </p>
                      </div>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={(e) => {
                          e.stopPropagation()
                          handleDelete(config.id)
                        }}
                      >
                        <Trash size={16} className="text-destructive" />
                      </Button>
                    </div>
                    <div className="flex gap-4 text-xs text-muted-foreground mt-2">
                      <span>Temp: {config.temperature}</span>
                      <span>Max Tokens: {config.maxTokens}</span>
                    </div>
                  </Card>
                ))}
              </div>
            </ScrollArea>
          </div>

          <Separator orientation="vertical" className="h-auto" />

          <div className="w-1/2 flex flex-col">
            {(isCreating || editingConfig) ? (
              <>
                <h3 className="font-semibold mb-4">
                  {editingConfig ? 'Edit Configuration' : 'New Configuration'}
                </h3>

                <ScrollArea className="flex-1 pr-4">
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="config-name">Configuration Name</Label>
                      <Input
                        id="config-name"
                        placeholder="e.g., Production GPT-4, Development Claude"
                        value={name}
                        onChange={(e) => setName(e.target.value)}
                      />
                    </div>

                    <div>
                      <Label htmlFor="provider">Provider</Label>
                      <Select value={provider} onValueChange={(v: any) => {
                        setProvider(v)
                        setModelName(getProviderModels(v)[0])
                      }}>
                        <SelectTrigger id="provider">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="openai">OpenAI</SelectItem>
                          <SelectItem value="anthropic">Anthropic</SelectItem>
                          <SelectItem value="azure">Azure OpenAI</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>

                    <div>
                      <Label htmlFor="model">Model</Label>
                      <Select value={modelName} onValueChange={setModelName}>
                        <SelectTrigger id="model">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {getProviderModels(provider).map(model => (
                            <SelectItem key={model} value={model}>
                              {model}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>

                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <Label htmlFor="temperature">Temperature</Label>
                        <span className="text-sm text-muted-foreground">{temperature}</span>
                      </div>
                      <Slider
                        id="temperature"
                        min={0}
                        max={2}
                        step={0.1}
                        value={[temperature]}
                        onValueChange={(v) => setTemperature(v[0])}
                        className="w-full"
                      />
                      <p className="text-xs text-muted-foreground mt-1">
                        Lower = more focused, Higher = more creative
                      </p>
                    </div>

                    <div>
                      <Label htmlFor="max-tokens">Max Tokens</Label>
                      <Input
                        id="max-tokens"
                        type="number"
                        min={100}
                        max={32000}
                        step={100}
                        value={maxTokens}
                        onChange={(e) => setMaxTokens(parseInt(e.target.value) || 2000)}
                      />
                    </div>

                    <Separator />

                    <div>
                      <Label htmlFor="scope-type">Scope</Label>
                      <Select value={scopeType} onValueChange={(v: any) => {
                        setScopeType(v)
                        setScopeId('')
                      }}>
                        <SelectTrigger id="scope-type">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="team">Team Default</SelectItem>
                          <SelectItem value="project">Project</SelectItem>
                          <SelectItem value="category">Category</SelectItem>
                          <SelectItem value="tag">Tag</SelectItem>
                        </SelectContent>
                      </Select>
                      <p className="text-xs text-muted-foreground mt-1">
                        Resolution order: Prompt → Project → Category → Tag → Team Default
                      </p>
                    </div>

                    {scopeType !== 'team' && (
                      <div>
                        <Label htmlFor="scope-id">
                          Select {scopeType.charAt(0).toUpperCase() + scopeType.slice(1)}
                        </Label>
                        <Select value={scopeId} onValueChange={setScopeId}>
                          <SelectTrigger id="scope-id">
                            <SelectValue placeholder={`Choose a ${scopeType}...`} />
                          </SelectTrigger>
                          <SelectContent>
                            {getScopeOptions().map(option => (
                              <SelectItem key={option.value} value={option.value}>
                                {option.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                    )}
                  </div>
                </ScrollArea>

                <div className="flex gap-2 mt-4 pt-4 border-t">
                  <Button onClick={handleSave} className="flex-1">
                    <Lightning size={16} />
                    {editingConfig ? 'Update' : 'Create'}
                  </Button>
                  <Button variant="outline" onClick={handleCancel}>
                    Cancel
                  </Button>
                </div>
              </>
            ) : (
              <div className="flex-1 flex items-center justify-center text-center text-muted-foreground p-8">
                <div>
                  <Cpu size={48} className="mx-auto mb-4 opacity-50" />
                  <p className="text-sm">
                    Select a configuration to edit or create a new one
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
