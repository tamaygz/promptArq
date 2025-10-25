import { PromptVersion } from '@/lib/types'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { cn } from '@/lib/utils'

type VersionDiffProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  oldVersion: PromptVersion
  newVersion: PromptVersion
}

export function VersionDiff({ open, onOpenChange, oldVersion, newVersion }: VersionDiffProps) {
  const oldLines = oldVersion.content.split('\n')
  const newLines = newVersion.content.split('\n')
  
  const maxLines = Math.max(oldLines.length, newLines.length)
  
  const formatDate = (timestamp: number) => {
    return new Date(timestamp).toLocaleString()
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-7xl w-[80vw]">
        <DialogHeader>
          <DialogTitle>Version Comparison</DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <Badge variant="secondary">v{oldVersion.versionNumber}</Badge>
              <span className="text-xs text-muted-foreground">
                {formatDate(oldVersion.createdAt)}
              </span>
            </div>
            <p className="text-sm text-muted-foreground">{oldVersion.changeNote}</p>
          </div>

          <div className="space-y-2">
            <div className="flex items-center gap-2">
              <Badge variant="default">v{newVersion.versionNumber}</Badge>
              <span className="text-xs text-muted-foreground">
                {formatDate(newVersion.createdAt)}
              </span>
            </div>
            <p className="text-sm text-muted-foreground">{newVersion.changeNote}</p>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4 h-[500px]">
          <ScrollArea className="h-full border rounded-lg">
            <pre className="p-4 text-xs font-mono whitespace-pre-wrap bg-muted/30">
              {oldVersion.content}
            </pre>
          </ScrollArea>

          <ScrollArea className="h-full border rounded-lg">
            <pre className="p-4 text-xs font-mono whitespace-pre-wrap bg-primary/5">
              {newVersion.content}
            </pre>
          </ScrollArea>
        </div>

        <div className="flex items-center justify-center gap-4 text-xs text-muted-foreground">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-muted/30 border" />
            <span>Old Version</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded bg-primary/5 border" />
            <span>New Version</span>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
