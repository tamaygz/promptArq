import { Badge } from '@/components/ui/badge'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Sparkle, CloudSlash, Chip } from '@phosphor-icons/react'
import { getFeatureStatus } from '@/lib/spark-gateway'
import { hasGitHubModelsSupport } from '@/lib/github-models-client'

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

  const hasGitHubModels = hasGitHubModelsSupport()

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge variant="outline" className="gap-1.5 cursor-help">
            {hasGitHubModels && <Chip className="w-3 h-3" weight="fill" />}
            {!hasGitHubModels && <CloudSlash className="w-3 h-3" />}
            Standalone Mode
          </Badge>
        </TooltipTrigger>
        <TooltipContent>
          <div className="text-xs space-y-1">
            <p className="font-semibold">Standalone Mode:</p>
            <p>✓ Full prompt management</p>
            <p>✓ Local storage</p>
            <p>✓ GitHub authentication</p>
            {hasGitHubModels ? (
              <>
                <p>✓ AI features (GitHub Models)</p>
                <p>✓ AI Prompt Improvements</p>
                <p>✓ AI Title Generation</p>
              </>
            ) : (
              <p className="text-muted-foreground">✗ AI features (login required)</p>
            )}
          </div>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}
