import { useState } from 'react'
import { useKV } from '@github/spark/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Plus, MagnifyingGlass, Sparkle, FolderOpen, GearSix, Archive, DownloadSimple } from '@phosphor-icons/react'
import { PromptList } from '@/components/PromptList'
import { PromptEditor } from '@/components/PromptEditor'
import { ProjectDialog } from '@/components/ProjectDialog'
import { SystemPromptDialog } from '@/components/SystemPromptDialog'
import { Prompt, Project, Category, Tag, SystemPrompt, PromptVersion } from '@/lib/types'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Toaster } from '@/components/ui/sonner'
import { toast } from 'sonner'
import { motion, AnimatePresence } from 'framer-motion'
import { exportAllPrompts } from '@/lib/export'

function App() {
  const [prompts, setPrompts] = useKV<Prompt[]>('prompts', [])
  const [projects, setProjects] = useKV<Project[]>('projects', [])
  const [categories, setCategories] = useKV<Category[]>('categories', [])
  const [tags, setTags] = useKV<Tag[]>('tags', [])
  const [systemPrompts, setSystemPrompts] = useKV<SystemPrompt[]>('system-prompts', [])
  const [versions, setVersions] = useKV<PromptVersion[]>('prompt-versions', [])
  
  const [selectedPromptId, setSelectedPromptId] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedProjectId, setSelectedProjectId] = useState<string | 'all'>('all')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [showProjectDialog, setShowProjectDialog] = useState(false)
  const [showSystemPromptDialog, setShowSystemPromptDialog] = useState(false)
  const [showNewPrompt, setShowNewPrompt] = useState(false)
  const [showArchived, setShowArchived] = useState(false)

  const selectedPrompt = prompts?.find(p => p.id === selectedPromptId)

  const filteredPrompts = (prompts || []).filter(prompt => {
    if (!showArchived && prompt.isArchived) return false
    if (showArchived && !prompt.isArchived) return false
    
    const matchesSearch = !searchQuery || 
      prompt.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      prompt.description.toLowerCase().includes(searchQuery.toLowerCase()) ||
      prompt.content.toLowerCase().includes(searchQuery.toLowerCase())
    
    const matchesProject = selectedProjectId === 'all' || prompt.projectId === selectedProjectId
    
    const matchesTags = selectedTags.length === 0 || 
      selectedTags.every(tagId => prompt.tags.includes(tagId))
    
    return matchesSearch && matchesProject && matchesTags
  })

  const handleCreatePrompt = () => {
    setSelectedPromptId(null)
    setShowNewPrompt(true)
  }

  const handleSelectPrompt = (promptId: string) => {
    setSelectedPromptId(promptId)
    setShowNewPrompt(false)
  }

  const handleCloseEditor = () => {
    setSelectedPromptId(null)
    setShowNewPrompt(false)
  }

  const toggleTag = (tagId: string) => {
    setSelectedTags(prev => 
      prev.includes(tagId) 
        ? prev.filter(id => id !== tagId)
        : [...prev, tagId]
    )
  }

  const handleExportAll = () => {
    exportAllPrompts(
      prompts || [],
      versions || [],
      projects || [],
      categories || [],
      tags || []
    )
    toast.success('All prompts exported successfully')
  }

  return (
    <div className="h-screen flex flex-col bg-background">
      <Toaster />
      <header className="border-b border-border bg-card px-6 py-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center">
                <Sparkle weight="fill" className="text-primary-foreground" size={20} />
              </div>
              <h1 className="text-xl font-semibold tracking-tight">arqioly</h1>
            </div>
          </div>
          
          <div className="flex items-center gap-3">
            <Button 
              variant="ghost" 
              size="sm"
              onClick={handleExportAll}
              disabled={!prompts || prompts.length === 0}
            >
              <DownloadSimple size={16} />
              Export All
            </Button>
            <Button 
              variant="outline" 
              size="sm"
              onClick={() => setShowSystemPromptDialog(true)}
            >
              <GearSix size={16} />
              System Prompts
            </Button>
            <Button 
              variant="outline" 
              size="sm"
              onClick={() => setShowProjectDialog(true)}
            >
              <FolderOpen size={16} />
              Projects
            </Button>
            <Button onClick={handleCreatePrompt} size="sm">
              <Plus size={16} weight="bold" />
              New Prompt
            </Button>
          </div>
        </div>
      </header>

      <div className="flex-1 flex overflow-hidden">
        <aside className="w-80 border-r border-border bg-card flex flex-col">
          <div className="p-4 border-b border-border space-y-3">
            <div className="relative">
              <MagnifyingGlass 
                className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" 
                size={16} 
              />
              <Input
                placeholder="Search prompts..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>
            <Button
              variant={showArchived ? "default" : "outline"}
              size="sm"
              onClick={() => setShowArchived(!showArchived)}
              className="w-full"
            >
              <Archive size={16} />
              {showArchived ? 'Viewing Archived' : 'View Archived'}
            </Button>
          </div>

          <Tabs value={selectedProjectId} onValueChange={(v) => setSelectedProjectId(v)} className="flex-1 flex flex-col">
            <div className="px-4 pt-4">
              <TabsList className="w-full">
                <TabsTrigger value="all" className="flex-1">All</TabsTrigger>
                {(projects || []).slice(0, 2).map(project => (
                  <TabsTrigger key={project.id} value={project.id} className="flex-1">
                    {project.name}
                  </TabsTrigger>
                ))}
              </TabsList>
            </div>

            {(tags || []).length > 0 && (
              <div className="px-4 pt-4 pb-2">
                <div className="text-xs font-medium text-muted-foreground mb-2">Filter by tags</div>
                <div className="flex flex-wrap gap-1.5">
                  {(tags || []).map(tag => (
                    <Badge
                      key={tag.id}
                      variant={selectedTags.includes(tag.id) ? "default" : "outline"}
                      className="cursor-pointer text-xs"
                      style={selectedTags.includes(tag.id) ? { 
                        backgroundColor: tag.color,
                        borderColor: tag.color
                      } : {
                        borderColor: tag.color,
                        color: tag.color
                      }}
                      onClick={() => toggleTag(tag.id)}
                    >
                      {tag.name}
                    </Badge>
                  ))}
                </div>
              </div>
            )}

            <TabsContent value={selectedProjectId} className="flex-1 overflow-auto mt-0">
              <PromptList
                prompts={filteredPrompts}
                projects={projects || []}
                categories={categories || []}
                tags={tags || []}
                selectedPromptId={selectedPromptId}
                onSelectPrompt={handleSelectPrompt}
              />
            </TabsContent>
          </Tabs>
        </aside>

        <main className="flex-1 overflow-hidden">
          <AnimatePresence mode="wait">
            {(selectedPrompt || showNewPrompt) ? (
              <motion.div
                key="editor"
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                transition={{ duration: 0.2 }}
                className="h-full"
              >
                <PromptEditor
                  prompt={selectedPrompt}
                  projects={projects || []}
                  categories={categories || []}
                  tags={tags || []}
                  systemPrompts={systemPrompts || []}
                  onClose={handleCloseEditor}
                  onUpdate={(updatedPrompt) => {
                    setPrompts(current => {
                      const existing = current || []
                      const index = existing.findIndex(p => p.id === updatedPrompt.id)
                      if (index >= 0) {
                        const newPrompts = [...existing]
                        newPrompts[index] = updatedPrompt
                        return newPrompts
                      }
                      return [...existing, updatedPrompt]
                    })
                  }}
                />
              </motion.div>
            ) : (
              <motion.div
                key="welcome"
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                exit={{ opacity: 0, scale: 0.95 }}
                transition={{ duration: 0.2 }}
                className="h-full flex items-center justify-center text-center p-8"
              >
              <div className="max-w-2xl w-full">
                <div className="w-16 h-16 bg-accent rounded-2xl flex items-center justify-center mx-auto mb-4">
                  <Sparkle size={32} weight="duotone" className="text-primary" />
                </div>
                <h2 className="text-2xl font-semibold mb-2">Welcome to arqioly</h2>
                <p className="text-muted-foreground mb-8">
                  Create and manage your LLM prompts with versioning, AI improvements, and team collaboration.
                </p>
                
                {(prompts || []).length > 0 && (
                  <div className="grid grid-cols-3 gap-4 mb-8">
                    <div className="bg-card border rounded-lg p-4">
                      <div className="text-3xl font-bold text-primary mb-1">
                        {(prompts || []).filter(p => !p.isArchived).length}
                      </div>
                      <div className="text-sm text-muted-foreground">Active Prompts</div>
                    </div>
                    <div className="bg-card border rounded-lg p-4">
                      <div className="text-3xl font-bold text-primary mb-1">
                        {(projects || []).length}
                      </div>
                      <div className="text-sm text-muted-foreground">Projects</div>
                    </div>
                    <div className="bg-card border rounded-lg p-4">
                      <div className="text-3xl font-bold text-primary mb-1">
                        {(tags || []).length}
                      </div>
                      <div className="text-sm text-muted-foreground">Tags</div>
                    </div>
                  </div>
                )}

                <Button onClick={handleCreatePrompt} size="lg">
                  <Plus size={20} weight="bold" />
                  {(prompts || []).length > 0 ? 'Create New Prompt' : 'Create your first prompt'}
                </Button>
              </div>
            </motion.div>
            )}
          </AnimatePresence>
        </main>
      </div>

      <ProjectDialog
        open={showProjectDialog}
        onOpenChange={setShowProjectDialog}
        projects={projects || []}
        categories={categories || []}
        tags={tags || []}
        onUpdateProjects={setProjects}
        onUpdateCategories={setCategories}
        onUpdateTags={setTags}
      />

      <SystemPromptDialog
        open={showSystemPromptDialog}
        onOpenChange={setShowSystemPromptDialog}
        systemPrompts={systemPrompts || []}
        projects={projects || []}
        categories={categories || []}
        tags={tags || []}
        onUpdate={setSystemPrompts}
      />
    </div>
  )
}

export default App
