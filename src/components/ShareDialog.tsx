import { useState } from 'react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Copy, Check, ShareNetwork } from '@phosphor-icons/react'
import { toast } from 'sonner'

type ShareDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  shareUrl: string
}

export function ShareDialog({ open, onOpenChange, shareUrl }: ShareDialogProps) {
  const [copied, setCopied] = useState(false)

  const handleCopy = () => {
    navigator.clipboard.writeText(shareUrl)
    setCopied(true)
    toast.success('Link copied to clipboard')
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShareNetwork size={20} />
            Share Prompt
          </DialogTitle>
          <DialogDescription>
            Anyone with this link can view this prompt, but they won't be able to edit it.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4 py-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="share-link">Share Link</Label>
            <div className="flex gap-2">
              <Input
                id="share-link"
                value={shareUrl}
                readOnly
                className="flex-1 font-mono text-sm"
              />
              <Button onClick={handleCopy} size="sm" className="shrink-0">
                {copied ? <Check size={16} /> : <Copy size={16} />}
                {copied ? 'Copied!' : 'Copy'}
              </Button>
            </div>
          </div>

          <div className="bg-muted p-3 rounded-md">
            <p className="text-sm text-muted-foreground">
              This link provides read-only access to your prompt. Share it with team members, clients, or collaborators to get feedback.
            </p>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
