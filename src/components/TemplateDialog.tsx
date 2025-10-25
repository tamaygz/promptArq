import { useState, useMemo } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Card } from '@/components/ui/card'
import { MagnifyingGlass, Sparkle, X, ArrowsOut, ArrowsIn } from '@phosphor-icons/react'
import { PROMPT_TEMPLATES, PromptTemplate, getTemplatesByCategory } from '@/lib/default-templates'
import { DEFAULT_CATEGORIES } from '@/lib/default-categories'

type TemplateDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelectTemplate: (template: PromptTemplate) => void
}

export function TemplateDialog({ open, onOpenChange, onSelectTemplate }: TemplateDialogProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedCategory, setSelectedCategory] = useState<string>('all')
  const [selectedTag, setSelectedTag] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)

  const allTags = useMemo(() => {
    const tagSet = new Set<string>()
    PROMPT_TEMPLATES.forEach(template => {
      template.tags.forEach(tag => tagSet.add(tag))
    })
    return Array.from(tagSet).sort()
  }, [])

  const filteredTemplates = useMemo(() => {
    let filtered = PROMPT_TEMPLATES

    if (selectedCategory !== 'all') {
      filtered = getTemplatesByCategory(selectedCategory)
    }

    if (selectedTag) {
      filtered = filtered.filter(t => t.tags.includes(selectedTag))
    }

    if (searchQuery) {
      const query = searchQuery.toLowerCase()
      filtered = filtered.filter(t => 
        t.title.toLowerCase().includes(query) ||
        t.description.toLowerCase().includes(query) ||
        t.category.toLowerCase().includes(query) ||
        t.tags.some(tag => tag.toLowerCase().includes(query))
      )
    }

    return filtered
  }, [selectedCategory, selectedTag, searchQuery])

  const handleSelectTemplate = (template: PromptTemplate) => {
    onSelectTemplate(template)
    onOpenChange(false)
    setSearchQuery('')
    setSelectedCategory('all')
    setSelectedTag(null)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className={`${isFullscreen ? 'max-w-[96vw] h-[96vh]' : 'max-w-6xl h-[85vh]'} flex flex-col p-0 transition-all`}>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => setIsFullscreen(!isFullscreen)}
          className="absolute top-4 right-14 z-10 h-8 w-8 p-0 opacity-70 hover:opacity-100 transition-opacity"
        >
          {isFullscreen ? <ArrowsIn size={18} /> : <ArrowsOut size={18} />}
        </Button>
        
        <DialogHeader className="px-6 pt-6 pb-4 border-b shrink-0">
          <DialogTitle className="text-2xl flex items-center gap-2">
            <Sparkle size={24} weight="fill" className="text-primary" />
            Prompt Template Library
          </DialogTitle>
          <DialogDescription>
            Choose from {PROMPT_TEMPLATES.length}+ professionally crafted prompt templates
          </DialogDescription>
        </DialogHeader>

        <div className="px-6 py-4 border-b shrink-0 space-y-4">
          <div className="relative">
            <MagnifyingGlass 
              className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" 
              size={18} 
            />
            <Input
              placeholder="Search templates..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10 h-11"
            />
          </div>

          {selectedTag && (
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">Filtered by tag:</span>
              <Badge 
                variant="secondary" 
                className="gap-1 cursor-pointer hover:bg-destructive hover:text-destructive-foreground transition-colors"
                onClick={() => setSelectedTag(null)}
              >
                {selectedTag}
                <X size={12} />
              </Badge>
            </div>
          )}
        </div>

        <Tabs value={selectedCategory} onValueChange={setSelectedCategory} className="flex-1 flex flex-col min-h-0">
          <div className="px-6 shrink-0">
            <TabsList className="w-full justify-start overflow-x-auto flex-nowrap h-auto py-2">
              <TabsTrigger value="all" className="shrink-0">
                All ({PROMPT_TEMPLATES.length})
              </TabsTrigger>
              {DEFAULT_CATEGORIES.map(category => {
                const count = getTemplatesByCategory(category.name).length
                return (
                  <TabsTrigger key={category.name} value={category.name} className="shrink-0">
                    {category.name} ({count})
                  </TabsTrigger>
                )
              })}
            </TabsList>
          </div>

          <TabsContent value={selectedCategory} className="flex-1 mt-0 min-h-0">
            <ScrollArea className="h-full">
              <div className="px-6 py-4">
                {filteredTemplates.length === 0 ? (
                  <div className="text-center py-12 text-muted-foreground">
                    <Sparkle size={48} className="mx-auto mb-4 opacity-50" />
                    <p>No templates found matching your criteria</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {filteredTemplates.map((template, index) => (
                      <Card
                        key={index}
                        className="p-5 hover:border-primary hover:shadow-lg transition-all cursor-pointer group"
                        onClick={() => handleSelectTemplate(template)}
                      >
                        <div className="flex flex-col gap-3 h-full">
                          <div className="flex items-start justify-between gap-2">
                            <h3 className="font-semibold text-base leading-tight group-hover:text-primary transition-colors">
                              {template.title}
                            </h3>
                            <Sparkle 
                              size={18} 
                              weight="fill" 
                              className="text-primary shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" 
                            />
                          </div>
                          
                          <p className="text-sm text-muted-foreground line-clamp-2 flex-1">
                            {template.description}
                          </p>

                          <div className="flex flex-wrap gap-1.5 pt-2 border-t">
                            <Badge variant="secondary" className="text-xs">
                              {template.category}
                            </Badge>
                            {template.tags.slice(0, 2).map(tag => (
                              <Badge
                                key={tag}
                                variant="outline"
                                className="text-xs cursor-pointer hover:bg-primary hover:text-primary-foreground transition-colors"
                                onClick={(e) => {
                                  e.stopPropagation()
                                  setSelectedTag(tag)
                                }}
                              >
                                {tag}
                              </Badge>
                            ))}
                            {template.tags.length > 2 && (
                              <Badge variant="outline" className="text-xs">
                                +{template.tags.length - 2}
                              </Badge>
                            )}
                          </div>
                        </div>
                      </Card>
                    ))}
                  </div>
                )}
              </div>
            </ScrollArea>
          </TabsContent>
        </Tabs>

        <div className="px-6 py-4 border-t shrink-0 flex items-center justify-between text-sm text-muted-foreground">
          <div>
            Showing {filteredTemplates.length} template{filteredTemplates.length !== 1 ? 's' : ''}
          </div>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
