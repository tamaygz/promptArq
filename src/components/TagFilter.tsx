import { useState, useMemo } from 'react'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Tag, Prompt } from '@/lib/types'
import { MagnifyingGlass, X } from '@phosphor-icons/react'
import { Button } from '@/components/ui/button'

type TagFilterProps = {
  tags: Tag[]
  prompts: Prompt[]
  selectedTags: string[]
  onToggleTag: (tagId: string) => void
  maxTopTags?: number
}

export function TagFilter({ 
  tags, 
  prompts,
  selectedTags, 
  onToggleTag,
  maxTopTags = 12
}: TagFilterProps) {
  const [searchQuery, setSearchQuery] = useState('')

  const tagUsageCounts = useMemo(() => {
    const counts: Record<string, number> = {}
    tags.forEach(tag => {
      counts[tag.id] = prompts.filter(p => p.tags.includes(tag.id)).length
    })
    return counts
  }, [tags, prompts])

  const tagsInUse = useMemo(() => {
    return tags.filter(tag => tagUsageCounts[tag.id] > 0)
  }, [tags, tagUsageCounts])

  const topUsedTags = useMemo(() => {
    return [...tagsInUse]
      .sort((a, b) => tagUsageCounts[b.id] - tagUsageCounts[a.id])
      .slice(0, maxTopTags)
  }, [tagsInUse, tagUsageCounts, maxTopTags])

  const filteredTags = useMemo(() => {
    if (!searchQuery.trim()) return []
    
    const query = searchQuery.toLowerCase()
    return tagsInUse
      .filter(tag => tag.name.toLowerCase().includes(query))
      .sort((a, b) => {
        const aSelected = selectedTags.includes(a.id)
        const bSelected = selectedTags.includes(b.id)
        if (aSelected && !bSelected) return -1
        if (!aSelected && bSelected) return 1
        
        const aUsage = tagUsageCounts[a.id] || 0
        const bUsage = tagUsageCounts[b.id] || 0
        return bUsage - aUsage
      })
  }, [tagsInUse, searchQuery, selectedTags, tagUsageCounts])

  if (tagsInUse.length === 0) return null

  return (
    <div className="px-4 md:px-8 pt-4 md:pt-8 pb-4 space-y-4">
      <div className="text-sm font-medium text-muted-foreground">Filter by tags</div>
      
      {topUsedTags.length > 0 && !searchQuery && (
        <div className="flex flex-wrap gap-2">
          {topUsedTags.map(tag => (
            <Badge
              key={tag.id}
              variant={selectedTags.includes(tag.id) ? "default" : "outline"}
              className="cursor-pointer text-xs hover:opacity-80 transition-opacity"
              style={selectedTags.includes(tag.id) ? { 
                backgroundColor: tag.color,
                borderColor: tag.color
              } : {
                borderColor: tag.color,
                color: tag.color
              }}
              onClick={() => onToggleTag(tag.id)}
            >
              {tag.name}
            </Badge>
          ))}
        </div>
      )}

      <div className="relative">
        <MagnifyingGlass 
          className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" 
          size={16} 
        />
        <Input
          placeholder="Search more tags..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="pl-9 pr-9 h-10"
        />
        {searchQuery && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setSearchQuery('')}
            className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7 p-0"
          >
            <X size={14} />
          </Button>
        )}
      </div>

      {searchQuery && (
        <ScrollArea className="h-[240px] rounded-lg border border-border bg-card/50">
          <div className="p-2 space-y-1">
            {filteredTags.length > 0 ? (
              filteredTags.map(tag => {
                const isSelected = selectedTags.includes(tag.id)
                const usage = tagUsageCounts[tag.id] || 0
                
                return (
                  <div
                    key={tag.id}
                    onClick={() => onToggleTag(tag.id)}
                    className="flex items-center justify-between p-2 rounded-md hover:bg-accent/50 cursor-pointer transition-colors"
                  >
                    <div className="flex items-center gap-2.5 min-w-0 flex-1">
                      <div 
                        className="w-2.5 h-2.5 rounded-full shrink-0" 
                        style={{ backgroundColor: tag.color }}
                      />
                      <span className="text-sm font-medium truncate">{tag.name}</span>
                      <span className="text-xs text-muted-foreground">
                        ({usage})
                      </span>
                    </div>
                    {isSelected && (
                      <div 
                        className="w-4 h-4 rounded-full flex items-center justify-center text-[10px] font-bold shrink-0"
                        style={{ 
                          backgroundColor: tag.color,
                          color: 'white'
                        }}
                      >
                        ✓
                      </div>
                    )}
                  </div>
                )
              })
            ) : (
              <div className="text-sm text-muted-foreground text-center py-8">
                No tags found matching "{searchQuery}"
              </div>
            )}
          </div>
        </ScrollArea>
      )}
    </div>
  )
}
