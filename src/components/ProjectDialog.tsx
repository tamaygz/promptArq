import { useState } from 'react'
import { Project, Category, Tag } from '@/lib/types'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Plus, Trash, Sparkle } from '@phosphor-icons/react'
import { toast } from 'sonner'
import { ScrollArea } from '@/components/ui/scroll-area'
import { getDefaultCategories } from '@/lib/default-categories'
import { Separator } from '@/components/ui/separator'

type ProjectDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  projects: Project[]
  categories: Category[]
  tags: Tag[]
  onUpdateProjects: (projects: Project[]) => void
  onUpdateCategories: (categories: Category[]) => void
  onUpdateTags: (tags: Tag[]) => void
}

const COLORS = [
  '#3b82f6',
  '#8b5cf6',
  '#ec4899',
  '#f59e0b',
  '#10b981',
  '#06b6d4',
  '#6366f1',
  '#f43f5e',
]

export function ProjectDialog({
  open,
  onOpenChange,
  projects,
  categories,
  tags,
  onUpdateProjects,
  onUpdateCategories,
  onUpdateTags,
}: ProjectDialogProps) {
  const [newProjectName, setNewProjectName] = useState('')
  const [newProjectDesc, setNewProjectDesc] = useState('')
  const [newCategoryName, setNewCategoryName] = useState('')
  const [newCategoryDesc, setNewCategoryDesc] = useState('')
  const [selectedProjectForCategory, setSelectedProjectForCategory] = useState('')
  const [newTagName, setNewTagName] = useState('')
  const [selectedColor, setSelectedColor] = useState(COLORS[0])

  const defaultCategories = getDefaultCategories()

  const handleAddProject = () => {
    if (!newProjectName.trim()) {
      toast.error('Project name is required')
      return
    }

    const project: Project = {
      id: `project-${Date.now()}`,
      name: newProjectName.trim(),
      description: newProjectDesc.trim(),
      color: COLORS[projects.length % COLORS.length],
    }

    onUpdateProjects([...projects, project])
    setNewProjectName('')
    setNewProjectDesc('')
    toast.success('Project created')
  }

  const handleDeleteProject = (id: string) => {
    onUpdateProjects(projects.filter(p => p.id !== id))
    onUpdateCategories(categories.filter(c => c.projectId !== id))
    toast.success('Project deleted')
  }

  const handleAddCategory = () => {
    if (!newCategoryName.trim()) {
      toast.error('Category name is required')
      return
    }

    if (!selectedProjectForCategory) {
      toast.error('Select a project first')
      return
    }

    const category: Category = {
      id: `category-${Date.now()}`,
      projectId: selectedProjectForCategory,
      name: newCategoryName.trim(),
      description: newCategoryDesc.trim(),
    }

    onUpdateCategories([...categories, category])
    setNewCategoryName('')
    setNewCategoryDesc('')
    toast.success('Category created')
  }

  const handleDeleteCategory = (id: string) => {
    onUpdateCategories(categories.filter(c => c.id !== id))
    toast.success('Category deleted')
  }

  const handleAddTag = () => {
    if (!newTagName.trim()) {
      toast.error('Tag name is required')
      return
    }

    const tag: Tag = {
      id: `tag-${Date.now()}`,
      name: newTagName.trim(),
      color: selectedColor,
    }

    onUpdateTags([...tags, tag])
    setNewTagName('')
    toast.success('Tag created')
  }

  const handleDeleteTag = (id: string) => {
    onUpdateTags(tags.filter(t => t.id !== id))
    toast.success('Tag deleted')
  }

  const handleAddDefaultCategory = (categoryName: string, categoryDesc: string) => {
    if (!selectedProjectForCategory) {
      toast.error('Select a project first')
      return
    }

    const exists = categories.some(
      c => c.projectId === selectedProjectForCategory && c.name === categoryName
    )

    if (exists) {
      toast.error('This category already exists in the selected project')
      return
    }

    const category: Category = {
      id: `category-${Date.now()}`,
      projectId: selectedProjectForCategory,
      name: categoryName,
      description: categoryDesc,
    }

    onUpdateCategories([...categories, category])
    toast.success(`${categoryName} category added`)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl">
        <DialogHeader>
          <DialogTitle>Manage Projects & Organization</DialogTitle>
          <DialogDescription>
            Create and manage projects, categories, and tags for organizing your prompts.
          </DialogDescription>
        </DialogHeader>

        <Tabs defaultValue="projects" className="flex-1">
          <TabsList className="grid w-full grid-cols-3">
            <TabsTrigger value="projects">Projects</TabsTrigger>
            <TabsTrigger value="categories">Categories</TabsTrigger>
            <TabsTrigger value="tags">Tags</TabsTrigger>
          </TabsList>

          <TabsContent value="projects" className="space-y-6">
            <Card className="p-6">
              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-3">
                  <Label htmlFor="project-name">Project Name</Label>
                  <Input
                    id="project-name"
                    value={newProjectName}
                    onChange={(e) => setNewProjectName(e.target.value)}
                    placeholder="e.g., Customer Support"
                    className="h-11"
                  />
                </div>
                <div className="flex flex-col gap-3">
                  <Label htmlFor="project-desc">Description</Label>
                  <Textarea
                    id="project-desc"
                    value={newProjectDesc}
                    onChange={(e) => setNewProjectDesc(e.target.value)}
                    placeholder="Optional description..."
                    rows={2}
                  />
                </div>
                <Button onClick={handleAddProject} className="w-full">
                  <Plus size={16} weight="bold" />
                  Create Project
                </Button>
              </div>
            </Card>

            <ScrollArea className="h-64">
              <div className="flex flex-col gap-3">
                {projects.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground py-12">
                    No projects yet. Create your first project above.
                  </div>
                ) : (
                  projects.map(project => (
                    <Card key={project.id} className="p-5">
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-3 mb-2">
                            <div
                              className="w-3 h-3 rounded-full"
                              style={{ backgroundColor: project.color }}
                            />
                            <h4 className="font-medium">{project.name}</h4>
                          </div>
                          {project.description && (
                            <p className="text-sm text-muted-foreground">
                              {project.description}
                            </p>
                          )}
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteProject(project.id)}
                        >
                          <Trash size={16} />
                        </Button>
                      </div>
                    </Card>
                  ))
                )}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="categories" className="space-y-6">
            <Card className="p-6">
              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-3">
                  <Label htmlFor="category-project">Project</Label>
                  <select
                    id="category-project"
                    value={selectedProjectForCategory}
                    onChange={(e) => setSelectedProjectForCategory(e.target.value)}
                    className="flex h-11 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                  >
                    <option value="">Select a project</option>
                    {projects.map(project => (
                      <option key={project.id} value={project.id}>
                        {project.name}
                      </option>
                    ))}
                  </select>
                </div>

                {selectedProjectForCategory && (
                  <>
                    <Separator />
                    <div className="space-y-3">
                      <div className="flex items-center gap-2">
                        <Sparkle size={16} className="text-primary" />
                        <Label className="text-sm font-medium">Quick Add Templates</Label>
                      </div>
                      <div className="grid grid-cols-2 gap-2">
                        {defaultCategories.map(template => {
                          const exists = categories.some(
                            c => c.projectId === selectedProjectForCategory && c.name === template.name
                          )
                          return (
                            <Button
                              key={template.name}
                              variant="outline"
                              size="sm"
                              onClick={() => handleAddDefaultCategory(template.name, template.description)}
                              disabled={exists}
                              className="justify-start text-left h-auto py-2"
                            >
                              <Plus size={14} className="mr-2 shrink-0" />
                              <span className="truncate text-xs">{template.name}</span>
                            </Button>
                          )
                        })}
                      </div>
                    </div>
                    <Separator />
                  </>
                )}

                <div className="flex flex-col gap-3">
                  <Label htmlFor="category-name">Category Name</Label>
                  <Input
                    id="category-name"
                    value={newCategoryName}
                    onChange={(e) => setNewCategoryName(e.target.value)}
                    placeholder="e.g., Email Templates"
                    className="h-11"
                  />
                </div>
                <div className="flex flex-col gap-3">
                  <Label htmlFor="category-desc">Description</Label>
                  <Textarea
                    id="category-desc"
                    value={newCategoryDesc}
                    onChange={(e) => setNewCategoryDesc(e.target.value)}
                    placeholder="Optional description..."
                    rows={2}
                  />
                </div>
                <Button onClick={handleAddCategory} className="w-full">
                  <Plus size={16} weight="bold" />
                  Create Category
                </Button>
              </div>
            </Card>

            <ScrollArea className="h-64">
              <div className="flex flex-col gap-3">
                {categories.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground py-12">
                    No categories yet. Create your first category above.
                  </div>
                ) : (
                  categories.map(category => {
                    const project = projects.find(p => p.id === category.projectId)
                    return (
                      <Card key={category.id} className="p-5">
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="flex items-center gap-3 mb-2">
                              <h4 className="font-medium">{category.name}</h4>
                              {project && (
                                <Badge variant="outline" style={{ borderColor: project.color, color: project.color }}>
                                  {project.name}
                                </Badge>
                              )}
                            </div>
                            {category.description && (
                              <p className="text-sm text-muted-foreground">
                                {category.description}
                              </p>
                            )}
                          </div>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDeleteCategory(category.id)}
                          >
                            <Trash size={16} />
                          </Button>
                        </div>
                      </Card>
                    )
                  })
                )}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="tags" className="space-y-6">
            <Card className="p-6">
              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-3">
                  <Label htmlFor="tag-name">Tag Name</Label>
                  <Input
                    id="tag-name"
                    value={newTagName}
                    onChange={(e) => setNewTagName(e.target.value)}
                    placeholder="e.g., production"
                    className="h-11"
                  />
                </div>
                <div className="flex flex-col gap-3">
                  <Label>Color</Label>
                  <div className="flex gap-2.5">
                    {COLORS.map(color => (
                      <button
                        key={color}
                        className={`w-9 h-9 rounded-md transition-all ${
                          selectedColor === color ? 'ring-2 ring-offset-2 ring-primary' : ''
                        }`}
                        style={{ backgroundColor: color }}
                        onClick={() => setSelectedColor(color)}
                      />
                    ))}
                  </div>
                </div>
                <Button onClick={handleAddTag} className="w-full">
                  <Plus size={16} weight="bold" />
                  Create Tag
                </Button>
              </div>
            </Card>

            <ScrollArea className="h-64">
              <div className="flex flex-col gap-3">
                {tags.length === 0 ? (
                  <div className="text-center text-sm text-muted-foreground py-12">
                    No tags yet. Create your first tag above.
                  </div>
                ) : (
                  tags.map(tag => (
                    <Card key={tag.id} className="p-5">
                      <div className="flex items-center justify-between">
                        <Badge style={{ backgroundColor: tag.color, borderColor: tag.color }} className="px-4 py-2">
                          {tag.name}
                        </Badge>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleDeleteTag(tag.id)}
                        >
                          <Trash size={16} />
                        </Button>
                      </div>
                    </Card>
                  ))
                )}
              </div>
            </ScrollArea>
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  )
}
