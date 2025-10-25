import { useState, useMemo } from 'react'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Tag, Prompt } from '@/lib/types'
import { MagnifyingGlass, X } from '@phosphor-icons/react'
import { Button } from '@/components/ui/button'

type TagSelectorProps = {
  tags: Tag[]
  selectedTags: string[]
  onToggleTag: (tagId: string) => void
  prompts?: Prompt[]
  maxTopTags?: number
  label?: string
  showLabel?: boolean
}

export function TagSelector({ 
  tags, 
  selectedTags, 
  onToggleTag, 
  prompts = [],
  maxTopTags = 8,
  label = "Tags",
  showLabel = true
}: TagSelectorProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [isFocused, setIsFocused] = useState(false)

  const tagUsageCounts = useMemo(() => {
    const counts: Record<string, number> = {}
    tags.forEach(tag => {
      counts[tag.id] = prompts.filter(p => p.tags.includes(tag.id)).length
    })
    return counts
  }, [tags, prompts])

  const topUsedTags = useMemo(() => {
    return [...tags]
      .filter(tag => tagUsageCounts[tag.id] > 0)
      .sort((a, b) => tagUsageCounts[b.id] - tagUsageCounts[a.id])
      .slice(0, maxTopTags)
  }, [tags, tagUsageCounts, maxTopTags])

  const top10Tags = useMemo(() => {
    return [...tags]
      .sort((a, b) => {
        const aUsage = tagUsageCounts[a.id] || 0
        const bUsage = tagUsageCounts[b.id] || 0
        return bUsage - aUsage
      })
      .slice(0, 10)
  }, [tags, tagUsageCounts])

  const filteredTags = useMemo(() => {
    if (!searchQuery.trim()) {
      return isFocused ? top10Tags : []
    }
    
    const query = searchQuery.toLowerCase()
    return tags
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
  }, [tags, searchQuery, selectedTags, tagUsageCounts, isFocused, top10Tags])

  const isTagInTopUsed = (tagId: string) => topUsedTags.some(t => t.id === tagId)

  const handleTagClick = (tagId: string) => {
    onToggleTag(tagId)
    setSearchQuery('')
    setIsFocused(false)
  }

  return (
    <div className="flex flex-col gap-4">
      {showLabel && <Label className="text-sm font-medium">{label}</Label>}
      
      {topUsedTags.length > 0 && (
        <div className="space-y-3">
          <div className="text-xs text-muted-foreground font-medium">Most Used</div>
          <div className="flex flex-wrap gap-2">
            {topUsedTags.map(tag => (
              <Badge
                key={tag.id}
                variant={selectedTags.includes(tag.id) ? "default" : "outline"}
                className="cursor-pointer px-3 py-1.5 text-sm hover:opacity-80 transition-opacity"
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
                {tagUsageCounts[tag.id] > 0 && (
                  <span className="ml-1.5 opacity-70 text-xs">
                    ({tagUsageCounts[tag.id]})
                  </span>
                )}
              </Badge>
            ))}
          </div>
        </div>
      )}

      <div className="space-y-3">
        <div className="relative">
          <MagnifyingGlass 
            className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none" 
            size={16} 
          />
          <Input
            placeholder="Search all tags..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            onFocus={() => setIsFocused(true)}
            onBlur={() => setTimeout(() => setIsFocused(false), 200)}
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

        {(searchQuery || isFocused) && filteredTags.length > 0 && (
          <ScrollArea className="h-[280px] rounded-lg border border-border bg-card/50">
            <div className="p-3 space-y-2">
              {!searchQuery && isFocused && (
                <div className="text-xs text-muted-foreground font-medium mb-3 px-2.5">
                  Top 10 Tags
                </div>
              )}
              {filteredTags.map(tag => {
                const isSelected = selectedTags.includes(tag.id)
                const usage = tagUsageCounts[tag.id] || 0
                
                return (
                  <div
                    key={tag.id}
                    onClick={() => handleTagClick(tag.id)}
                    className="flex items-center justify-between p-2.5 rounded-md hover:bg-accent/50 cursor-pointer transition-colors"
                  >
                    <div className="flex items-center gap-3 min-w-0 flex-1">
                      <div 
                        className="w-3 h-3 rounded-full shrink-0" 
                        style={{ backgroundColor: tag.color }}
                      />
                      <span className="text-sm font-medium truncate">{tag.name}</span>
                      {usage > 0 && (
                        <span className="text-xs text-muted-foreground">
                          ({usage})
                        </span>
                      )}
                    </div>
                    {isSelected && (
                      <div 
                        className="w-5 h-5 rounded-full flex items-center justify-center text-xs font-bold shrink-0"
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
              })}
            </div>
          </ScrollArea>
        )}

        {searchQuery && filteredTags.length === 0 && (
          <div className="rounded-lg border border-border bg-card/50 p-6">
            <div className="text-sm text-muted-foreground text-center">
              No tags found matching "{searchQuery}"
            </div>
          </div>
        )}

        {!searchQuery && !isFocused && selectedTags.length > 0 && (
          <div className="space-y-3">
            <div className="text-xs text-muted-foreground font-medium">Selected Tags</div>
            <div className="flex flex-wrap gap-2">
              {selectedTags
                .filter(tagId => !isTagInTopUsed(tagId))
                .map(tagId => {
                  const tag = tags.find(t => t.id === tagId)
                  if (!tag) return null
                  
                  return (
                    <Badge
                      key={tag.id}
                      variant="default"
                      className="cursor-pointer px-3 py-1.5 text-sm hover:opacity-80 transition-opacity"
                      style={{
                        backgroundColor: tag.color,
                        borderColor: tag.color
                      }}
                      onClick={() => onToggleTag(tag.id)}
                    >
                      {tag.name}
                      {tagUsageCounts[tag.id] > 0 && (
                        <span className="ml-1.5 opacity-70 text-xs">
                          ({tagUsageCounts[tag.id]})
                        </span>
                      )}
                    </Badge>
                  )
                })}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
