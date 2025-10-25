import { useState } from 'react'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
import { Checkbox } from '@/components/ui/checkbox'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Team, TeamMember, Project } from '@/lib/types'
import { Plus, Trash, Copy, Check, Users, LinkSimple, FolderOpen, Crown, ShieldCheck, User, Pencil } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { motion, AnimatePresence } from 'framer-motion'

type TeamDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  teams: Team[]
  teamMembers: TeamMember[]
  projects: Project[]
  currentUserId: string
  onUpdateTeams: (teams: Team[]) => void
  onUpdateTeamMembers: (members: TeamMember[]) => void
}

export function TeamDialog({ 
  open, 
  onOpenChange, 
  teams, 
  teamMembers,
  projects, 
  currentUserId,
  onUpdateTeams,
  onUpdateTeamMembers
}: TeamDialogProps) {
  const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [newTeamName, setNewTeamName] = useState('')
  const [newTeamDescription, setNewTeamDescription] = useState('')
  const [copiedToken, setCopiedToken] = useState<string | null>(null)

  const selectedTeam = teams.find(t => t.id === selectedTeamId)
  const selectedTeamMembers = teamMembers.filter(m => m.teamId === selectedTeamId)

  const handleCreateTeam = () => {
    if (!newTeamName.trim()) {
      toast.error('Please enter a team name')
      return
    }

    const newTeam: Team = {
      id: `team_${Date.now()}`,
      name: newTeamName,
      description: newTeamDescription,
      ownerId: currentUserId,
      projectIds: [],
      inviteToken: generateInviteToken(),
      createdAt: Date.now(),
      updatedAt: Date.now()
    }

    onUpdateTeams([...teams, newTeam])

    const ownerMember: TeamMember = {
      id: `member_${Date.now()}`,
      teamId: newTeam.id,
      userId: currentUserId,
      userName: 'You',
      userAvatar: '',
      role: 'owner',
      joinedAt: Date.now()
    }

    onUpdateTeamMembers([...teamMembers, ownerMember])

    setNewTeamName('')
    setNewTeamDescription('')
    setIsCreating(false)
    setSelectedTeamId(newTeam.id)
    toast.success('Team created successfully')
  }

  const handleDeleteTeam = (teamId: string) => {
    onUpdateTeams(teams.filter(t => t.id !== teamId))
    onUpdateTeamMembers(teamMembers.filter(m => m.teamId !== teamId))
    if (selectedTeamId === teamId) {
      setSelectedTeamId(null)
    }
    toast.success('Team deleted')
  }

  const handleToggleProject = (projectId: string) => {
    if (!selectedTeam) return

    const updatedTeam = {
      ...selectedTeam,
      projectIds: selectedTeam.projectIds.includes(projectId)
        ? selectedTeam.projectIds.filter(id => id !== projectId)
        : [...selectedTeam.projectIds, projectId],
      updatedAt: Date.now()
    }

    onUpdateTeams(teams.map(t => t.id === selectedTeam.id ? updatedTeam : t))
  }

  const handleCopyInviteLink = (token: string) => {
    const inviteLink = `${window.location.origin}?team_invite=${token}`
    navigator.clipboard.writeText(inviteLink)
    setCopiedToken(token)
    toast.success('Invite link copied to clipboard')
    setTimeout(() => setCopiedToken(null), 2000)
  }

  const handleRegenerateToken = () => {
    if (!selectedTeam) return

    const updatedTeam = {
      ...selectedTeam,
      inviteToken: generateInviteToken(),
      updatedAt: Date.now()
    }

    onUpdateTeams(teams.map(t => t.id === selectedTeam.id ? updatedTeam : t))
    toast.success('New invite link generated')
  }

  const handleRemoveMember = (memberId: string) => {
    onUpdateTeamMembers(teamMembers.filter(m => m.id !== memberId))
    toast.success('Member removed from team')
  }

  const handleChangeRole = (memberId: string, newRole: 'admin' | 'member') => {
    onUpdateTeamMembers(teamMembers.map(m => 
      m.id === memberId ? { ...m, role: newRole } : m
    ))
    toast.success('Member role updated')
  }

  const generateInviteToken = () => {
    return `${Date.now()}_${Math.random().toString(36).substring(2, 15)}`
  }

  const getInitials = (name: string) => {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
  }

  const getRoleIcon = (role: string) => {
    switch (role) {
      case 'owner':
        return <Crown size={14} weight="fill" className="text-yellow-500" />
      case 'admin':
        return <ShieldCheck size={14} weight="fill" className="text-primary" />
      default:
        return <User size={14} />
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl max-h-[85vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Users size={24} />
            Team Management
          </DialogTitle>
          <DialogDescription>
            Create teams, manage members, and control project access
          </DialogDescription>
        </DialogHeader>

        <div className="flex-1 overflow-hidden flex gap-6">
          <div className="w-72 flex flex-col gap-4 border-r pr-6">
            <Button 
              onClick={() => setIsCreating(!isCreating)} 
              variant={isCreating ? "secondary" : "default"}
              className="w-full"
            >
              <Plus size={16} />
              {isCreating ? 'Cancel' : 'New Team'}
            </Button>

            <AnimatePresence>
              {isCreating && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: 'auto' }}
                  exit={{ opacity: 0, height: 0 }}
                  className="space-y-4 overflow-hidden"
                >
                  <div className="space-y-2">
                    <Label htmlFor="team-name">Team Name</Label>
                    <Input
                      id="team-name"
                      placeholder="Engineering Team"
                      value={newTeamName}
                      onChange={(e) => setNewTeamName(e.target.value)}
                      onKeyDown={(e) => e.key === 'Enter' && handleCreateTeam()}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="team-description">Description</Label>
                    <Textarea
                      id="team-description"
                      placeholder="Team description..."
                      value={newTeamDescription}
                      onChange={(e) => setNewTeamDescription(e.target.value)}
                      rows={3}
                    />
                  </div>
                  <Button onClick={handleCreateTeam} className="w-full">
                    Create Team
                  </Button>
                </motion.div>
              )}
            </AnimatePresence>

            <Separator />

            <div className="flex-1 overflow-y-auto space-y-2">
              {teams.length === 0 ? (
                <div className="text-center py-12 text-muted-foreground text-sm">
                  No teams yet
                </div>
              ) : (
                teams.map(team => (
                  <button
                    key={team.id}
                    onClick={() => {
                      setSelectedTeamId(team.id)
                      setIsCreating(false)
                    }}
                    className={`w-full text-left p-3 rounded-lg border transition-colors ${
                      selectedTeamId === team.id
                        ? 'bg-primary/10 border-primary'
                        : 'hover:bg-accent border-transparent'
                    }`}
                  >
                    <div className="font-medium mb-1">{team.name}</div>
                    <div className="text-xs text-muted-foreground flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Users size={12} />
                        {teamMembers.filter(m => m.teamId === team.id).length} members
                      </div>
                      <div className="flex items-center gap-1">
                        <FolderOpen size={12} />
                        {team.projectIds.length}
                      </div>
                    </div>
                  </button>
                ))
              )}
            </div>
          </div>

          <div className="flex-1 overflow-y-auto">
            {selectedTeam ? (
              <Tabs defaultValue="projects" className="h-full">
                <TabsList className="mb-6">
                  <TabsTrigger value="projects" className="gap-2">
                    <FolderOpen size={16} />
                    Projects
                  </TabsTrigger>
                  <TabsTrigger value="members" className="gap-2">
                    <Users size={16} />
                    Members
                  </TabsTrigger>
                  <TabsTrigger value="settings" className="gap-2">
                    <LinkSimple size={16} />
                    Settings
                  </TabsTrigger>
                </TabsList>

                <TabsContent value="projects" className="space-y-4">
                  <div>
                    <h3 className="text-lg font-semibold mb-2">{selectedTeam.name}</h3>
                    <p className="text-sm text-muted-foreground mb-6">
                      {selectedTeam.description || 'No description'}
                    </p>
                  </div>

                  <div>
                    <Label className="text-base mb-4 block">Project Access</Label>
                    <p className="text-sm text-muted-foreground mb-4">
                      Select which projects members of this team can access
                    </p>

                    {projects.length === 0 ? (
                      <div className="text-center py-12 text-muted-foreground text-sm border rounded-lg">
                        No projects available
                      </div>
                    ) : (
                      <div className="space-y-3">
                        {projects.map(project => (
                          <div
                            key={project.id}
                            className="flex items-start gap-3 p-4 border rounded-lg hover:bg-accent/50 transition-colors"
                          >
                            <Checkbox
                              id={`project-${project.id}`}
                              checked={selectedTeam.projectIds.includes(project.id)}
                              onCheckedChange={() => handleToggleProject(project.id)}
                            />
                            <div className="flex-1">
                              <label
                                htmlFor={`project-${project.id}`}
                                className="font-medium cursor-pointer flex items-center gap-2"
                              >
                                <div
                                  className="w-3 h-3 rounded-full"
                                  style={{ backgroundColor: project.color }}
                                />
                                {project.name}
                              </label>
                              <p className="text-sm text-muted-foreground mt-1">
                                {project.description}
                              </p>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </TabsContent>

                <TabsContent value="members" className="space-y-4">
                  <div>
                    <h3 className="text-lg font-semibold mb-2">Team Members</h3>
                    <p className="text-sm text-muted-foreground mb-4">
                      {selectedTeamMembers.length} {selectedTeamMembers.length === 1 ? 'member' : 'members'}
                    </p>

                    <div className="bg-muted/50 rounded-lg p-4 mb-6 space-y-3">
                      <div className="text-sm font-medium mb-3">Role Permissions</div>
                      <div className="space-y-2 text-xs">
                        <div className="flex items-start gap-2">
                          <Crown size={14} className="text-yellow-500 mt-0.5 shrink-0" weight="fill" />
                          <div>
                            <span className="font-medium">Owner:</span> Full control including team deletion
                          </div>
                        </div>
                        <div className="flex items-start gap-2">
                          <ShieldCheck size={14} className="text-primary mt-0.5 shrink-0" weight="fill" />
                          <div>
                            <span className="font-medium">Admin:</span> Manage members, projects, and settings
                          </div>
                        </div>
                        <div className="flex items-start gap-2">
                          <User size={14} className="text-muted-foreground mt-0.5 shrink-0" />
                          <div>
                            <span className="font-medium">Member:</span> View and edit accessible prompts
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="space-y-2">
                    {selectedTeamMembers.map(member => {
                      const isOwner = member.role === 'owner'
                      const canModify = !isOwner

                      return (
                        <div
                          key={member.id}
                          className="flex items-center gap-3 p-4 border rounded-lg hover:bg-accent/50 transition-colors"
                        >
                          <Avatar className="w-10 h-10">
                            <AvatarImage src={member.userAvatar} alt={member.userName} />
                            <AvatarFallback className="text-sm">
                              {getInitials(member.userName)}
                            </AvatarFallback>
                          </Avatar>
                          <div className="flex-1 min-w-0">
                            <div className="font-medium flex items-center gap-2">
                              <span className="truncate">{member.userName}</span>
                              {getRoleIcon(member.role)}
                            </div>
                            <div className="text-xs text-muted-foreground">
                              Joined {new Date(member.joinedAt).toLocaleDateString()}
                            </div>
                          </div>
                          <div className="flex items-center gap-2">
                            {canModify ? (
                              <Select
                                value={member.role}
                                onValueChange={(value: 'admin' | 'member') => 
                                  handleChangeRole(member.id, value)
                                }
                              >
                                <SelectTrigger className="w-32 h-8">
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  <SelectItem value="admin">Admin</SelectItem>
                                  <SelectItem value="member">Member</SelectItem>
                                </SelectContent>
                              </Select>
                            ) : (
                              <Badge variant="secondary" className="capitalize">
                                {member.role}
                              </Badge>
                            )}
                            {canModify && (
                              <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => handleRemoveMember(member.id)}
                              >
                                <Trash size={16} />
                              </Button>
                            )}
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </TabsContent>

                <TabsContent value="settings" className="space-y-6">
                  <div>
                    <h3 className="text-lg font-semibold mb-2">Invite Link</h3>
                    <p className="text-sm text-muted-foreground mb-4">
                      Share this link with people you want to invite to {selectedTeam.name}
                    </p>

                    <div className="space-y-3">
                      <div className="flex gap-2">
                        <Input
                          value={`${window.location.origin}?team_invite=${selectedTeam.inviteToken}`}
                          readOnly
                          className="font-mono text-sm"
                        />
                        <Button
                          onClick={() => handleCopyInviteLink(selectedTeam.inviteToken)}
                          variant="secondary"
                        >
                          {copiedToken === selectedTeam.inviteToken ? (
                            <Check size={16} className="text-green-600" />
                          ) : (
                            <Copy size={16} />
                          )}
                        </Button>
                      </div>

                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleRegenerateToken}
                      >
                        Regenerate Invite Link
                      </Button>

                      <p className="text-xs text-muted-foreground">
                        Regenerating the link will invalidate the previous invite link
                      </p>
                    </div>
                  </div>

                  <Separator />

                  <div>
                    <h3 className="text-lg font-semibold mb-2 text-destructive">Danger Zone</h3>
                    <p className="text-sm text-muted-foreground mb-4">
                      Permanently delete this team and remove all members
                    </p>
                    <Button
                      variant="destructive"
                      onClick={() => handleDeleteTeam(selectedTeam.id)}
                    >
                      <Trash size={16} />
                      Delete Team
                    </Button>
                  </div>
                </TabsContent>
              </Tabs>
            ) : (
              <div className="h-full flex items-center justify-center text-center p-8">
                <div>
                  <Users size={48} className="mx-auto mb-4 text-muted-foreground" />
                  <p className="text-muted-foreground">
                    {teams.length === 0
                      ? 'Create your first team to get started'
                      : 'Select a team to view details'}
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
