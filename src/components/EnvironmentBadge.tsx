import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Sparkle, CloudSlash } from '@phosphor-icons/react'
import { getFeatureStatus } from '@/lib/spark-gateway'

export function EnvironmentBadge() {
  const features = getFeatureStatus()
  
  if (features.spark) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Badge variant="default" className="gap-1.5 cursor-help">
              <Sparkle className="w-3 h-3" weight="fill" />
              Spark Mode
            </Badge>
          </TooltipTrigger>
          <TooltipContent>
            <div className="text-xs space-y-1">
              <p className="font-semibold">Spark Features Available:</p>
              <p>✓ AI Prompt Improvements</p>
              <p>✓ AI Title Generation</p>
              <p>✓ Prompt Execution</p>
              <p>✓ Cloud Storage</p>
            </div>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    )
  }

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge variant="outline" className="gap-1.5 cursor-help">
            <CloudSlash className="w-3 h-3" />
            Standalone Mode
          </Badge>
        </TooltipTrigger>
        <TooltipContent>
          <div className="text-xs space-y-1">
            <p className="font-semibold">Standalone Mode:</p>
            <p>✓ Full prompt management</p>
            <p>✓ Local SQLite storage</p>
            <p>✓ GitHub authentication</p>
            <p className="text-muted-foreground">✗ AI features disabled</p>
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
