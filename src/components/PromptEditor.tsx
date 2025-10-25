import { useState, useEffect } from 'react'
import { useKV } from '@github/spark/hooks'
import { Prompt, Project, Category, Tag, PromptVersion, Comment, SystemPrompt } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Card } from '@/components/ui/card'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { X, FloppyDisk, Clock, ChatCircle, Sparkle, ArrowCounterClockwise, Archive, ArrowCounterClockwise as Restore, GitDiff, Export } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'
import { resolveSystemPrompt } from '@/lib/prompt-resolver'
import { exportPrompt } from '@/lib/export'
import { VersionDiff } from './VersionDiff'

type PromptEditorProps = {
  prompt?: Prompt
  projects: Project[]
  categories: Category[]
  tags: Tag[]
  systemPrompts: SystemPrompt[]
  onClose: () => void
  onUpdate: (prompt: Prompt) => void
}

export function PromptEditor({ prompt, projects, categories, tags, systemPrompts, onClose, onUpdate }: PromptEditorProps) {
  const [versions, setVersions] = useKV<PromptVersion[]>('prompt-versions', [])
  const [comments, setComments] = useKV<Comment[]>('prompt-comments', [])
  const [user, setUser] = useState<any>(null)

  const [title, setTitle] = useState(prompt?.title || '')
  const [description, setDescription] = useState(prompt?.description || '')
  const [content, setContent] = useState(prompt?.content || '')
  const [projectId, setProjectId] = useState(prompt?.projectId || projects[0]?.id || '')
  const [categoryId, setCategoryId] = useState(prompt?.categoryId || '')
  const [selectedTags, setSelectedTags] = useState<string[]>(prompt?.tags || [])
  const [changeNote, setChangeNote] = useState('')
  const [improving, setImproving] = useState(false)
  const [newComment, setNewComment] = useState('')
  const [showDiff, setShowDiff] = useState(false)
  const [diffVersions, setDiffVersions] = useState<{ old: PromptVersion | null, new: PromptVersion | null }>({ old: null, new: null })

  useEffect(() => {
    window.spark.user().then(setUser)
  }, [])

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 's') {
        e.preventDefault()
        handleSave()
      }
      if ((e.metaKey || e.ctrlKey) && e.key === 'i') {
        e.preventDefault()
        if (content.trim() && !improving) {
          handleImprove()
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [title, description, content, projectId, categoryId, selectedTags, changeNote, improving])

  const promptVersions = versions?.filter(v => v.promptId === prompt?.id) || []
  const promptComments = comments?.filter(c => c.promptId === prompt?.id) || []

  const projectCategories = categories.filter(c => c.projectId === projectId)

  const currentProject = projects.find(p => p.id === projectId)
  const currentCategory = categories.find(c => c.id === categoryId)
  const currentTags = tags.filter(t => selectedTags.includes(t.id))

  const handleSave = () => {
    if (!title.trim()) {
      toast.error('Title is required')
      return
    }

    if (!projectId) {
      toast.error('Project is required')
      return
    }

    const now = Date.now()
    const newPrompt: Prompt = {
      id: prompt?.id || `prompt-${now}`,
      title: title.trim(),
      description: description.trim(),
      content,
      projectId,
      categoryId,
      tags: selectedTags,
      createdBy: user?.login || 'anonymous',
      createdAt: prompt?.createdAt || now,
      updatedAt: now,
      isArchived: false
    }

    const newVersion: PromptVersion = {
      id: `version-${now}`,
      promptId: newPrompt.id,
      versionNumber: promptVersions.length + 1,
      content,
      changeNote: changeNote.trim() || 'Updated prompt',
      createdBy: user?.login || 'anonymous',
      createdAt: now
    }

    setVersions(current => [...(current || []), newVersion])
    onUpdate(newPrompt)
    setChangeNote('')
    toast.success('Prompt saved successfully')
  }

  const handleImprove = async () => {
    if (!content.trim()) {
      toast.error('Add some content first')
      return
    }

    setImproving(true)
    try {
      const systemPromptText = resolveSystemPrompt(
        prompt,
        currentProject,
        currentCategory,
        currentTags,
        systemPrompts
      )

      const improvePrompt = window.spark.llmPrompt`${systemPromptText}

Improve this prompt:

${content}

Provide only the improved prompt text, without any explanations or meta-commentary.`

      const improved = await window.spark.llm(improvePrompt, 'gpt-4o-mini')
      
      setContent(improved.trim())
      setChangeNote('Improved by AI')
      toast.success('Prompt improved! Review and save if you like it.')
    } catch (error) {
      toast.error('Failed to improve prompt')
      console.error(error)
    } finally {
      setImproving(false)
    }
  }

  const handleAddComment = () => {
    if (!newComment.trim() || !prompt) return

    const comment: Comment = {
      id: `comment-${Date.now()}`,
      promptId: prompt.id,
      userId: user?.id || 'anonymous',
      userName: user?.login || 'Anonymous',
      userAvatar: user?.avatarUrl || '',
      content: newComment.trim(),
      createdAt: Date.now(),
      updatedAt: Date.now()
    }

    setComments(current => [...(current || []), comment])
    setNewComment('')
    toast.success('Comment added')
  }

  const handleRestore = (version: PromptVersion) => {
    setContent(version.content)
    setChangeNote(`Restored from version ${version.versionNumber}`)
    toast.success('Version restored')
  }

  const handleCompare = (version: PromptVersion) => {
    const currentVersionIndex = promptVersions.findIndex(v => v.id === version.id)
    if (currentVersionIndex > 0) {
      setDiffVersions({
        old: promptVersions[currentVersionIndex - 1],
        new: version
      })
      setShowDiff(true)
    } else {
      toast.error('No previous version to compare')
    }
  }

  const handleExport = () => {
    if (!prompt) return
    
    const promptTags = tags.filter(t => selectedTags.includes(t.id))
    exportPrompt(prompt, promptVersions, currentProject, currentCategory, promptTags)
    toast.success('Prompt exported successfully')
  }

  const handleArchive = () => {
    if (!prompt) return
    const updated = { ...prompt, isArchived: !prompt.isArchived }
    onUpdate(updated)
    toast.success(prompt.isArchived ? 'Prompt restored' : 'Prompt archived')
    if (!prompt.isArchived) {
      onClose()
    }
  }

  const toggleTag = (tagId: string) => {
    setSelectedTags(prev =>
      prev.includes(tagId) ? prev.filter(id => id !== tagId) : [...prev, tagId]
    )
  }

  const formatDate = (timestamp: number) => {
    return new Date(timestamp).toLocaleString()
  }

  return (
    <div className="h-full flex flex-col">
      <div className="border-b border-border bg-card px-6 py-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">
            {prompt ? 'Edit Prompt' : 'New Prompt'}
          </h2>
          <div className="flex items-center gap-2">
            {prompt && (
              <>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleExport}
                >
                  <Export size={16} />
                  Export
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleArchive}
                >
                  {prompt.isArchived ? <Restore size={16} /> : <Archive size={16} />}
                  {prompt.isArchived ? 'Restore' : 'Archive'}
                </Button>
              </>
            )}
            <Button
              variant="outline"
              size="sm"
              onClick={handleImprove}
              disabled={improving || !content.trim()}
              title="Improve with AI (⌘I or Ctrl+I)"
            >
              <Sparkle size={16} weight={improving ? "fill" : "regular"} />
              {improving ? 'Improving...' : 'Improve Prompt'}
            </Button>
            <Button size="sm" onClick={handleSave} title="Save version (⌘S or Ctrl+S)">
              <FloppyDisk size={16} weight="bold" />
              Save Version
            </Button>
            <Button variant="ghost" size="sm" onClick={onClose}>
              <X size={20} />
            </Button>
          </div>
        </div>
      </div>

      <div className="flex-1 flex overflow-hidden">
        <div className="flex-1 overflow-auto">
          <div className="p-6 max-w-4xl">
            <div className="flex flex-col gap-6">
              <div className="flex flex-col gap-2">
                <Label htmlFor="title">Title</Label>
                <Input
                  id="title"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Enter prompt title..."
                  className="text-base"
                />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Describe what this prompt does..."
                  rows={2}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="project">Project</Label>
                  <Select value={projectId} onValueChange={setProjectId}>
                    <SelectTrigger id="project">
                      <SelectValue placeholder="Select project" />
                    </SelectTrigger>
                    <SelectContent>
                      {projects.map(project => (
                        <SelectItem key={project.id} value={project.id}>
                          {project.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="category">Category</Label>
                  <Select value={categoryId || "none"} onValueChange={(val) => setCategoryId(val === "none" ? "" : val)}>
                    <SelectTrigger id="category">
                      <SelectValue placeholder="Select category" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="none">None</SelectItem>
                      {projectCategories.map(category => (
                        <SelectItem key={category.id} value={category.id}>
                          {category.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="flex flex-col gap-2">
                <Label>Tags</Label>
                <div className="flex flex-wrap gap-2">
                  {tags.map(tag => (
                    <Badge
                      key={tag.id}
                      variant={selectedTags.includes(tag.id) ? "default" : "outline"}
                      className="cursor-pointer"
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

              <div className="flex flex-col gap-2">
                <Label htmlFor="content">Prompt Content</Label>
                <Textarea
                  id="content"
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  placeholder="Enter your prompt here..."
                  rows={12}
                  className="font-mono text-sm"
                />
              </div>

              <div className="flex flex-col gap-2">
                <Label htmlFor="changeNote">Change Note (Optional)</Label>
                <Input
                  id="changeNote"
                  value={changeNote}
                  onChange={(e) => setChangeNote(e.target.value)}
                  placeholder="What changed in this version?"
                />
              </div>
            </div>
          </div>
        </div>

        <div className="w-80 border-l border-border bg-card overflow-hidden flex flex-col">
          <Tabs defaultValue="versions" className="flex-1 flex flex-col">
            <div className="px-4 pt-4">
              <TabsList className="w-full">
                <TabsTrigger value="versions" className="flex-1">
                  <Clock size={16} />
                  Versions
                </TabsTrigger>
                <TabsTrigger value="comments" className="flex-1">
                  <ChatCircle size={16} />
                  Comments
                </TabsTrigger>
              </TabsList>
            </div>

            <TabsContent value="versions" className="flex-1 overflow-hidden mt-0">
              <ScrollArea className="h-full">
                <div className="p-4 flex flex-col gap-3">
                  {promptVersions.length === 0 ? (
                    <div className="text-center text-sm text-muted-foreground py-8">
                      No versions yet. Save to create the first version.
                    </div>
                  ) : (
                    promptVersions.slice().reverse().map(version => (
                      <Card key={version.id} className="p-3">
                        <div className="flex items-start justify-between gap-2 mb-2">
                          <div className="flex items-center gap-2">
                            <Badge variant="secondary" className="text-xs">
                              v{version.versionNumber}
                            </Badge>
                            {version.improvedFrom && (
                              <Badge variant="outline" className="text-xs">
                                <Sparkle size={10} weight="fill" />
                                AI
                              </Badge>
                            )}
                          </div>
                          <div className="flex gap-1">
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-6 px-2"
                              onClick={() => handleCompare(version)}
                              title="Compare with previous"
                            >
                              <GitDiff size={14} />
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-6 px-2"
                              onClick={() => handleRestore(version)}
                              title="Restore this version"
                            >
                              <ArrowCounterClockwise size={14} />
                            </Button>
                          </div>
                        </div>
                        <p className="text-xs text-muted-foreground mb-2">
                          {version.changeNote}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          by {version.createdBy} • {formatDate(version.createdAt)}
                        </p>
                      </Card>
                    ))
                  )}
                </div>
              </ScrollArea>
            </TabsContent>

            <TabsContent value="comments" className="flex-1 overflow-hidden mt-0 flex flex-col">
              <ScrollArea className="flex-1">
                <div className="p-4 flex flex-col gap-3">
                  {promptComments.length === 0 ? (
                    <div className="text-center text-sm text-muted-foreground py-8">
                      No comments yet. Start a discussion!
                    </div>
                  ) : (
                    promptComments.map(comment => (
                      <Card key={comment.id} className="p-3">
                        <div className="flex items-start gap-2 mb-2">
                          <Avatar className="h-6 w-6">
                            <AvatarImage src={comment.userAvatar} />
                            <AvatarFallback className="text-xs">
                              {comment.userName.slice(0, 2).toUpperCase()}
                            </AvatarFallback>
                          </Avatar>
                          <div className="flex-1 min-w-0">
                            <div className="text-xs font-medium">{comment.userName}</div>
                            <div className="text-xs text-muted-foreground">
                              {formatDate(comment.createdAt)}
                            </div>
                          </div>
                        </div>
                        <p className="text-sm">{comment.content}</p>
                      </Card>
                    ))
                  )}
                </div>
              </ScrollArea>
              <Separator />
              <div className="p-4">
                <div className="flex gap-2">
                  <Textarea
                    value={newComment}
                    onChange={(e) => setNewComment(e.target.value)}
                    placeholder="Add a comment..."
                    rows={2}
                    className="text-sm"
                  />
                  <Button size="sm" onClick={handleAddComment} disabled={!newComment.trim()}>
                    Send
                  </Button>
                </div>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </div>

      {showDiff && diffVersions.old && diffVersions.new && (
        <VersionDiff
          open={showDiff}
          onOpenChange={setShowDiff}
          oldVersion={diffVersions.old}
          newVersion={diffVersions.new}
        />
      )}
    </div>
  )
}
