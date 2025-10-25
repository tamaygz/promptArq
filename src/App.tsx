import { useState, useEffect } from 'react'
import { useKV } from '@github/spark/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
import { Plus, MagnifyingGlass, Sparkle, FolderOpen, GearSix, Archive, DownloadSimple, User as UserIcon, Cpu, GitBranch, CaretLeft, CaretRight, Users, CaretDown, List, X, BookOpen } from '@phosphor-icons/react'
import { PromptList } from '@/components/PromptList'
import { PromptEditor } from '@/components/PromptEditor'
import { ProjectDialog } from '@/components/ProjectDialog'
import { SystemPromptDialog } from '@/components/SystemPromptDialog'
import { ModelConfigDialog } from '@/components/ModelConfigDialog'
import { MCPServerDialog } from '@/components/MCPServerDialog'
import { SharedPromptView } from '@/components/SharedPromptView'
import { AuthGuard } from '@/components/AuthGuard'
import { UserProfile } from '@/components/UserProfile'
import { TeamDialog } from '@/components/TeamDialog'
import { TemplateDialog } from '@/components/TemplateDialog'
import { TagFilter } from '@/components/TagFilter'
import { Prompt, Project, Category, Tag, SystemPrompt, PromptVersion, ModelConfig, Team, TeamMember } from '@/lib/types'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Badge } from '@/components/ui/badge'
import { Toaster } from '@/components/ui/sonner'
import { toast } from 'sonner'
import { motion, AnimatePresence } from 'framer-motion'
import { exportAllPrompts } from '@/lib/export'
import logoIcon from '@/assets/images/logo_icon_boxed.png'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { useIsMobile } from '@/hooks/use-mobile'
import { PromptTemplate } from '@/lib/default-templates'
import { initializeDefaults } from '@/lib/initialize-defaults'
import { BackgroundDecorations } from '@/components/BackgroundDecorations'
import { FloatingShapes } from '@/components/FloatingShapes'

function App() {
  const isMobile = useIsMobile()
  const [prompts, setPrompts] = useKV<Prompt[]>('prompts', [])
  const [projects, setProjects] = useKV<Project[]>('projects', [])
  const [categories, setCategories] = useKV<Category[]>('categories', [])
  const [tags, setTags] = useKV<Tag[]>('tags', [])
  const [systemPrompts, setSystemPrompts] = useKV<SystemPrompt[]>('system-prompts', [])
  const [modelConfigs, setModelConfigs] = useKV<ModelConfig[]>('model-configs', [])
  const [versions, setVersions] = useKV<PromptVersion[]>('prompt-versions', [])
  const [teams, setTeams] = useKV<Team[]>('teams', [])
  const [teamMembers, setTeamMembers] = useKV<TeamMember[]>('team-members', [])
  const [initialized, setInitialized] = useKV<boolean>('app-initialized', false)
  
  const [selectedPromptId, setSelectedPromptId] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedProjectId, setSelectedProjectId] = useState<string | 'all'>('all')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [showProjectDialog, setShowProjectDialog] = useState(false)
  const [showSystemPromptDialog, setShowSystemPromptDialog] = useState(false)
  const [showModelConfigDialog, setShowModelConfigDialog] = useState(false)
  const [showMCPServerDialog, setShowMCPServerDialog] = useState(false)
  const [showTeamDialog, setShowTeamDialog] = useState(false)
  const [showNewPrompt, setShowNewPrompt] = useState(false)
  const [showArchived, setShowArchived] = useState(false)
  const [shareToken, setShareToken] = useState<string | null>(null)
  const [showUserProfile, setShowUserProfile] = useState(false)
  const [currentUser, setCurrentUser] = useState<{ login: string; avatarUrl: string; id: string } | null>(null)
  const [sidebarCollapsed, setSidebarCollapsed] = useKV<boolean>('sidebar-collapsed', false)
  const [selectedTeamId, setSelectedTeamId] = useKV<string | null>('selected-team-id', null)
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false)
  const [showTemplateDialog, setShowTemplateDialog] = useState(false)
  const [selectedTemplate, setSelectedTemplate] = useState<PromptTemplate | null>(null)

  const handleTeamInvite = async (inviteToken: string) => {
    try {
      const user = await window.spark.user()
      const allTeams = await window.spark.kv.get<Team[]>('teams') || []
      const allMembers = await window.spark.kv.get<TeamMember[]>('team-members') || []
      
      const team = allTeams.find(t => t.inviteToken === inviteToken)
      
      if (!team) {
        toast.error('Invalid invite link')
        window.history.pushState({}, '', window.location.pathname)
        return
      }

      const alreadyMember = allMembers.some(
        m => m.teamId === team.id && m.userId === user.id
      )

      if (alreadyMember) {
        toast.info('You are already a member of this team')
        window.history.pushState({}, '', window.location.pathname)
        return
      }

      const newMember: TeamMember = {
        id: `member_${Date.now()}`,
        teamId: team.id,
        userId: user.id,
        userName: user.login,
        userAvatar: user.avatarUrl,
        role: 'member',
        joinedAt: Date.now()
      }

      const updatedMembers = [...allMembers, newMember]
      await window.spark.kv.set('team-members', updatedMembers)
      setTeamMembers(updatedMembers)
      
      toast.success(`You've joined ${team.name}!`)
      window.history.pushState({}, '', window.location.pathname)
    } catch (err) {
      console.error('Failed to join team:', err)
      toast.error('Failed to join team')
      window.history.pushState({}, '', window.location.pathname)
    }
  }

  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const token = params.get('share')
    if (token) {
      setShareToken(token)
    }

    const teamInvite = params.get('team_invite')
    if (teamInvite) {
      handleTeamInvite(teamInvite)
    }
    
    loadCurrentUser()
  }, [])

  useEffect(() => {
    if (initialized === undefined || projects === undefined || categories === undefined || tags === undefined) {
      return
    }

    if (!initialized && projects.length === 0 && categories.length === 0 && tags.length === 0) {
      const defaults = initializeDefaults()
      setProjects([defaults.project])
      setCategories(defaults.categories)
      setTags(defaults.tags)
      setInitialized(true)
      toast.success('Welcome! Your workspace has been set up with default categories and tags.')
    } else if (!initialized && (projects.length > 0 || categories.length > 0 || tags.length > 0)) {
      setInitialized(true)
    }
  }, [initialized, projects, categories, tags])

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

  const selectedPrompt = prompts?.find(p => p.id === selectedPromptId)

  const currentTeam = teams?.find(t => t.id === selectedTeamId)
  const userTeams = teams?.filter(t => 
    teamMembers?.some(m => m.userId === currentUser?.id && m.teamId === t.id)
  ) || []
  
  const accessibleProjectIds = selectedTeamId 
    ? (currentTeam?.projectIds || [])
    : (projects || []).map(p => p.id)

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
    
    const matchesTeamAccess = selectedTeamId 
      ? accessibleProjectIds.includes(prompt.projectId)
      : true
    
    return matchesSearch && matchesProject && matchesTags && matchesTeamAccess
  })

  const handleCreatePrompt = () => {
    setSelectedPromptId(null)
    setShowNewPrompt(true)
  }

  const handleSelectPrompt = (promptId: string) => {
    setSelectedPromptId(promptId)
    setShowNewPrompt(false)
    if (isMobile) {
      setMobileSidebarOpen(false)
    }
  }

  const handleCloseEditor = () => {
    setSelectedPromptId(null)
    setShowNewPrompt(false)
    setSelectedTemplate(null)
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

  const handleSelectTemplate = (template: PromptTemplate) => {
    setSelectedTemplate(template)
    setSelectedPromptId(null)
    setShowNewPrompt(true)
  }

  if (shareToken) {
    return <SharedPromptView shareToken={shareToken} onClose={handleCloseSharedView} />
  }

  return (
    <AuthGuard>
      <div className="h-screen flex flex-col bg-background relative">
        <BackgroundDecorations />
        <Toaster />
        <header className="border-b border-border bg-card px-4 md:px-10 py-4 md:py-8 shrink-0 relative z-10">
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-3 md:gap-6 min-w-0">
              {isMobile && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setMobileSidebarOpen(!mobileSidebarOpen)}
                  className="shrink-0 h-11 w-11 p-0"
                >
                  <List size={20} />
                </Button>
              )}
              <div className="flex items-center gap-2 md:gap-4 min-w-0">
                <img src={logoIcon} alt="arqioly logo" className="w-8 h-8 md:w-11 md:h-11 rounded-lg shrink-0" />
                <h1 className="text-lg md:text-2xl font-semibold tracking-tight truncate">arqioly</h1>
              </div>
              
              {!isMobile && userTeams.length > 0 && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="sm" className="gap-2">
                      <Users size={16} />
                      {selectedTeamId ? currentTeam?.name || 'All Prompts' : 'All Prompts'}
                      <CaretDown size={12} />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className="w-56">
                    <DropdownMenuLabel>Switch Team View</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={() => setSelectedTeamId(null)}>
                      <span className="flex-1">All Prompts</span>
                      {!selectedTeamId && <span className="text-primary">✓</span>}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    {userTeams.map(team => (
                      <DropdownMenuItem 
                        key={team.id}
                        onClick={() => setSelectedTeamId(team.id)}
                      >
                        <span className="flex-1">{team.name}</span>
                        {selectedTeamId === team.id && <span className="text-primary">✓</span>}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
            </div>
            
            <div className="flex items-center gap-2 md:gap-4 shrink-0">
              {!isMobile && (
                <>
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
                    onClick={() => setShowTemplateDialog(true)}
                    className="gap-2"
                  >
                    <BookOpen size={16} weight="fill" />
                    Templates
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
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => setShowTeamDialog(true)}
                  >
                    <Users size={16} />
                    Teams
                  </Button>
                </>
              )}
              <Button onClick={handleCreatePrompt} size="sm" className="h-11">
                <Plus size={isMobile ? 20 : 16} weight="bold" />
                {!isMobile && 'New Prompt'}
              </Button>
              
              {currentUser && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-2 px-2 h-11"
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

      <div className="flex-1 flex overflow-hidden relative">
        {isMobile && mobileSidebarOpen && (
          <div 
            className="absolute inset-0 bg-black/50 z-40"
            onClick={() => setMobileSidebarOpen(false)}
          />
        )}
        
        <motion.aside 
          initial={false}
          animate={{ 
            x: isMobile 
              ? (mobileSidebarOpen ? 0 : -420)
              : 0,
            width: isMobile 
              ? 420
              : (sidebarCollapsed ? 0 : 420)
          }}
          transition={{ duration: 0.3, ease: [0.4, 0, 0.2, 1] }}
          className={`${isMobile ? 'absolute left-0 top-0 bottom-0 z-50 shadow-2xl' : 'relative'} border-r border-border bg-card flex flex-col overflow-hidden`}
          style={{ 
            maxWidth: isMobile ? '85vw' : undefined,
            backgroundImage: 'radial-gradient(circle at 1px 1px, rgb(var(--border) / 0.05) 1px, transparent 0)',
            backgroundSize: '24px 24px'
          }}
        >
          <div className={`${(isMobile ? mobileSidebarOpen : !sidebarCollapsed) ? 'opacity-100' : 'opacity-0'} transition-opacity duration-200 flex flex-col h-full overflow-hidden`}>
            <div className="p-4 md:p-8 border-b border-border space-y-4 md:space-y-5 shrink-0">
              {isMobile && (
                <div className="flex items-center justify-between mb-2">
                  <h2 className="font-semibold text-base">Prompts</h2>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setMobileSidebarOpen(false)}
                    className="h-8 w-8 p-0"
                  >
                    <X size={20} />
                  </Button>
                </div>
              )}
              {selectedTeamId && currentTeam && (
                <div className="bg-primary/10 border border-primary/20 rounded-lg p-3 md:p-4">
                  <div className="flex items-center gap-2 mb-2">
                    <Users size={16} className="text-primary" />
                    <span className="text-sm font-medium text-primary">Team View</span>
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Showing prompts from {currentTeam.projectIds.length} project{currentTeam.projectIds.length !== 1 ? 's' : ''} accessible to {currentTeam.name}
                  </p>
                </div>
              )}
              {isMobile && userTeams.length > 0 && (
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="sm" className="w-full gap-2">
                      <Users size={16} />
                      {selectedTeamId ? currentTeam?.name || 'All Prompts' : 'All Prompts'}
                      <CaretDown size={12} />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="start" className="w-56">
                    <DropdownMenuLabel>Switch Team View</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={() => setSelectedTeamId(null)}>
                      <span className="flex-1">All Prompts</span>
                      {!selectedTeamId && <span className="text-primary">✓</span>}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    {userTeams.map(team => (
                      <DropdownMenuItem 
                        key={team.id}
                        onClick={() => setSelectedTeamId(team.id)}
                      >
                        <span className="flex-1">{team.name}</span>
                        {selectedTeamId === team.id && <span className="text-primary">✓</span>}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              )}
              <div className="relative">
                <MagnifyingGlass 
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" 
                  size={16} 
                />
                <Input
                  placeholder="Search prompts..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="pl-9 h-11"
                />
              </div>
              <Button
                variant={showArchived ? "default" : "outline"}
                size="sm"
                onClick={() => setShowArchived(!showArchived)}
                className="w-full h-11"
              >
                <Archive size={16} />
                {showArchived ? 'Viewing Archived' : 'View Archived'}
              </Button>
              {isMobile && (
                <div className="grid grid-cols-2 gap-2">
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => {
                      setShowTemplateDialog(true)
                      setMobileSidebarOpen(false)
                    }}
                    className="h-11 col-span-2"
                  >
                    <BookOpen size={16} weight="fill" />
                    Templates
                  </Button>
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => {
                      setShowProjectDialog(true)
                      setMobileSidebarOpen(false)
                    }}
                    className="h-11"
                  >
                    <FolderOpen size={16} />
                    Projects
                  </Button>
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => {
                      setShowTeamDialog(true)
                      setMobileSidebarOpen(false)
                    }}
                    className="h-11"
                  >
                    <Users size={16} />
                    Teams
                  </Button>
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => {
                      setShowSystemPromptDialog(true)
                      setMobileSidebarOpen(false)
                    }}
                    className="h-11"
                  >
                    <GearSix size={16} />
                    System
                  </Button>
                  <Button 
                    variant="outline" 
                    size="sm"
                    onClick={() => {
                      setShowModelConfigDialog(true)
                      setMobileSidebarOpen(false)
                    }}
                    className="h-11"
                  >
                    <Cpu size={16} />
                    Models
                  </Button>
                </div>
              )}
            </div>

            <div className="flex-1 overflow-y-auto min-h-0">
              <Tabs value={selectedProjectId} onValueChange={(v) => setSelectedProjectId(v)} className="flex flex-col h-full">
                <div className="px-4 md:px-8 pt-4 md:pt-8 sticky top-0 bg-card z-10">
                  <TabsList className="w-full">
                    <TabsTrigger value="all" className="flex-1">All</TabsTrigger>
                    {(projects || [])
                      .filter(p => selectedTeamId ? accessibleProjectIds.includes(p.id) : true)
                      .slice(0, 2)
                      .map(project => (
                        <TabsTrigger key={project.id} value={project.id} className="flex-1">
                          {project.name}
                        </TabsTrigger>
                      ))}
                  </TabsList>
                </div>

                <TagFilter
                  tags={tags || []}
                  prompts={(prompts || []).filter(p => 
                    selectedTeamId ? accessibleProjectIds.includes(p.projectId) : true
                  )}
                  selectedTags={selectedTags}
                  onToggleTag={toggleTag}
                  maxTopTags={12}
                />

                <TabsContent value={selectedProjectId} className="mt-0 flex-1">
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
          </div>
        </motion.aside>

        {!isMobile && (
          <div className="relative">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setSidebarCollapsed(prev => !prev)}
              className="absolute top-4 -right-10 z-10 h-8 w-8 p-0 rounded-full border border-border bg-card hover:bg-accent"
            >
              {sidebarCollapsed ? <CaretRight size={16} /> : <CaretLeft size={16} />}
            </Button>
          </div>
        )}

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
                  template={selectedTemplate || undefined}
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
                    setSelectedTemplate(null)
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
                className="h-full flex items-center justify-center text-center p-6 md:p-16 overflow-y-auto relative"
              >
              <FloatingShapes />
              <div className="max-w-3xl w-full relative z-10">
                <img src={logoIcon} alt="arqioly logo" className="w-16 h-16 md:w-24 md:h-24 rounded-2xl mx-auto mb-6 md:mb-8" />
                <h2 className="text-2xl md:text-3xl font-semibold mb-4 md:mb-6">
                  {selectedTeamId && currentTeam ? `${currentTeam.name}` : 'Welcome to arqioly'}
                </h2>
                <p className="text-base md:text-lg text-muted-foreground mb-8 md:mb-16">
                  {selectedTeamId && currentTeam 
                    ? `Team workspace with access to ${currentTeam.projectIds.length} project${currentTeam.projectIds.length !== 1 ? 's' : ''}. Select a prompt to get started.`
                    : 'Create and manage your LLM prompts with versioning, AI improvements, and team collaboration.'
                  }
                </p>
                
                {(prompts || []).length > 0 && (
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-8 mb-8 md:mb-16">
                    <div className="bg-card border rounded-lg p-6 md:p-8 relative overflow-hidden group hover:shadow-lg transition-shadow">
                      <div className="absolute top-0 right-0 w-24 h-24 bg-primary/5 rounded-full blur-2xl group-hover:bg-primary/10 transition-colors" />
                      <div className="relative z-10">
                        <div className="text-3xl md:text-4xl font-bold text-primary mb-2 md:mb-3">
                          {selectedTeamId 
                            ? filteredPrompts.filter(p => !p.isArchived).length
                            : (prompts || []).filter(p => !p.isArchived).length
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {selectedTeamId ? 'Team Prompts' : 'Active Prompts'}
                        </div>
                      </div>
                    </div>
                    <div className="bg-card border rounded-lg p-6 md:p-8 relative overflow-hidden group hover:shadow-lg transition-shadow">
                      <div className="absolute top-0 right-0 w-24 h-24 bg-accent/5 rounded-full blur-2xl group-hover:bg-accent/10 transition-colors" />
                      <div className="relative z-10">
                        <div className="text-3xl md:text-4xl font-bold text-primary mb-2 md:mb-3">
                          {selectedTeamId 
                            ? (currentTeam?.projectIds.length || 0)
                            : (projects || []).length
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {selectedTeamId ? 'Accessible Projects' : 'Projects'}
                        </div>
                      </div>
                    </div>
                    <div className="bg-card border rounded-lg p-6 md:p-8 relative overflow-hidden group hover:shadow-lg transition-shadow">
                      <div className="absolute top-0 right-0 w-24 h-24 bg-secondary/5 rounded-full blur-2xl group-hover:bg-secondary/10 transition-colors" />
                      <div className="relative z-10">
                        <div className="text-3xl md:text-4xl font-bold text-primary mb-2 md:mb-3">
                          {selectedTeamId 
                            ? (teamMembers?.filter(m => m.teamId === selectedTeamId).length || 0)
                            : (tags || []).length
                          }
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {selectedTeamId ? 'Team Members' : 'Tags'}
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                <Button onClick={handleCreatePrompt} size="lg" className="h-12 md:h-auto">
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
        prompts={prompts || []}
        onUpdateProjects={setProjects}
        onUpdateCategories={setCategories}
        onUpdateTags={setTags}
        onUpdatePrompts={setPrompts}
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

      <TeamDialog
        open={showTeamDialog}
        onOpenChange={setShowTeamDialog}
        teams={teams || []}
        teamMembers={teamMembers || []}
        projects={projects || []}
        currentUserId={currentUser?.id || ''}
        onUpdateTeams={setTeams}
        onUpdateTeamMembers={setTeamMembers}
      />

      <TemplateDialog
        open={showTemplateDialog}
        onOpenChange={setShowTemplateDialog}
        onSelectTemplate={handleSelectTemplate}
      />
    </div>
    </AuthGuard>
  )
}

export default App
