import { useState, useEffect } from 'react'
import { useKV } from '@github/spark/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
import { Plus, MagnifyingGlass, Sparkle, FolderOpen, GearSix, Archive, DownloadSimple, User as UserIcon, Cpu, GitBranch } from '@phosphor-icons/react'
import { PromptList } from '@/components/PromptList'
import { PromptEditor } from '@/components/PromptEditor'
import { ProjectDialog } from '@/components/ProjectDialog'
import { SystemPromptDialog } from '@/components/SystemPromptDialog'
import { ModelConfigDialog } from '@/components/ModelConfigDialog'
import { MCPServerDialog } from '@/components/MCPServerDialog'
import { SharedPromptView } from '@/components/SharedPromptView'
import { AuthGuard } from '@/components/AuthGuard'
import { AuthCallback } from '@/components/AuthCallback'
import { UserProfile } from '@/components/UserProfile'
import { Prompt, Project, Category, Tag, SystemPrompt, PromptVersion, ModelConfig } from '@/lib/types'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Toaster } from '@/components/ui/sonner'
import { toast } from 'sonner'
import { motion, AnimatePresence } from 'framer-motion'
import { exportAllPrompts } from '@/lib/export'
import logoIcon from '@/assets/images/logo_icon_boxed.png'

function App() {
  const [prompts, setPrompts] = useKV<Prompt[]>('prompts', [])
  const [projects, setProjects] = useKV<Project[]>('projects', [])
  const [categories, setCategories] = useKV<Category[]>('categories', [])
  const [tags, setTags] = useKV<Tag[]>('tags', [])
  const [systemPrompts, setSystemPrompts] = useKV<SystemPrompt[]>('system-prompts', [])
  const [modelConfigs, setModelConfigs] = useKV<ModelConfig[]>('model-configs', [])
  const [versions, setVersions] = useKV<PromptVersion[]>('prompt-versions', [])
  
  const [selectedPromptId, setSelectedPromptId] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedProjectId, setSelectedProjectId] = useState<string | 'all'>('all')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [showProjectDialog, setShowProjectDialog] = useState(false)
  const [showSystemPromptDialog, setShowSystemPromptDialog] = useState(false)
  const [showModelConfigDialog, setShowModelConfigDialog] = useState(false)
  const [showMCPServerDialog, setShowMCPServerDialog] = useState(false)
  const [showNewPrompt, setShowNewPrompt] = useState(false)
  const [showArchived, setShowArchived] = useState(false)
  const [shareToken, setShareToken] = useState<string | null>(null)
  const [showUserProfile, setShowUserProfile] = useState(false)
  const [currentUser, setCurrentUser] = useState<{ login: string; avatarUrl: string } | null>(null)

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const token = params.get('share')
    if (token) {
      setShareToken(token)
    }
    
    loadCurrentUser()
  }, [])

  const loadCurrentUser = async () => {
    try {
      const userData = await window.spark.user()
      setCurrentUser(userData)
    } catch (err) {
      console.error('Failed to load user:', err)
    }
  }

  const getInitials = (name: string) => {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
  }

  if (window.location.pathname === '/auth/github/callback') {
    return <AuthCallback provider="github" />
  }

  if (window.location.pathname === '/auth/microsoft/callback') {
    return <AuthCallback provider="microsoft" />
  }

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

  const handleCloseSharedView = () => {
    setShareToken(null)
    window.history.pushState({}, '', window.location.pathname)
  }

  if (shareToken) {
    return <SharedPromptView shareToken={shareToken} onClose={handleCloseSharedView} />
  }

  return (
    <AuthGuard>
      <div className="h-screen flex flex-col bg-background">
        <Toaster />
        <header className="border-b border-border bg-card px-10 py-8">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-6">
              <div className="flex items-center gap-4">
                <img src={logoIcon} alt="arqioly logo" className="w-11 h-11 rounded-lg" />
                <h1 className="text-2xl font-semibold tracking-tight">arqioly</h1>
              </div>
            </div>
            
            <div className="flex items-center gap-4">
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
                onClick={() => setShowMCPServerDialog(true)}
              >
                <GitBranch size={16} />
                MCP Server
              </Button>
              <Button 
                variant="outline" 
                size="sm"
                onClick={() => setShowModelConfigDialog(true)}
              >
                <Cpu size={16} />
                Model Config
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
              
              {currentUser && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-2 px-2"
                  onClick={() => setShowUserProfile(true)}
                >
                  <Avatar className="w-7 h-7">
                    <AvatarImage src={currentUser.avatarUrl} alt={currentUser.login} />
                    <AvatarFallback className="text-xs">
                      {getInitials(currentUser.login)}
                    </AvatarFallback>
                  </Avatar>
                </Button>
              )}
            </div>
          </div>
        </header>

      <div className="flex-1 flex overflow-hidden">
        <aside className="w-[420px] border-r border-border bg-card flex flex-col overflow-hidden">
          <div className="p-8 border-b border-border space-y-5 shrink-0">
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

          <div className="flex-1 overflow-y-auto">
            <Tabs value={selectedProjectId} onValueChange={(v) => setSelectedProjectId(v)}>
              <div className="px-8 pt-8 sticky top-0 bg-card z-10">
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
                <div className="px-8 pt-8 pb-4">
                  <div className="text-sm font-medium text-muted-foreground mb-4">Filter by tags</div>
                  <div className="flex flex-wrap gap-2">
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

              <TabsContent value={selectedProjectId} className="mt-0">
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
          </div>
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
                  modelConfigs={modelConfigs || []}
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
                className="h-full flex items-center justify-center text-center p-16"
              >
              <div className="max-w-3xl w-full">
                <img src={logoIcon} alt="arqioly logo" className="w-24 h-24 rounded-2xl mx-auto mb-8" />
                <h2 className="text-3xl font-semibold mb-6">Welcome to arqioly</h2>
                <p className="text-lg text-muted-foreground mb-16">
                  Create and manage your LLM prompts with versioning, AI improvements, and team collaboration.
                </p>
                
                {(prompts || []).length > 0 && (
                  <div className="grid grid-cols-3 gap-8 mb-16">
                    <div className="bg-card border rounded-lg p-8">
                      <div className="text-4xl font-bold text-primary mb-3">
                        {(prompts || []).filter(p => !p.isArchived).length}
                      </div>
                      <div className="text-sm text-muted-foreground">Active Prompts</div>
                    </div>
                    <div className="bg-card border rounded-lg p-8">
                      <div className="text-4xl font-bold text-primary mb-3">
                        {(projects || []).length}
                      </div>
                      <div className="text-sm text-muted-foreground">Projects</div>
                    </div>
                    <div className="bg-card border rounded-lg p-8">
                      <div className="text-4xl font-bold text-primary mb-3">
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

      <UserProfile
        open={showUserProfile}
        onOpenChange={setShowUserProfile}
      />

      <ModelConfigDialog
        open={showModelConfigDialog}
        onOpenChange={setShowModelConfigDialog}
        modelConfigs={modelConfigs || []}
        projects={projects || []}
        categories={categories || []}
        tags={tags || []}
        onUpdate={setModelConfigs}
      />

      <MCPServerDialog
        open={showMCPServerDialog}
        onOpenChange={setShowMCPServerDialog}
        prompts={prompts || []}
        projects={projects || []}
        categories={categories || []}
        tags={tags || []}
      />
    </div>
    </AuthGuard>
  )
}

export default App
