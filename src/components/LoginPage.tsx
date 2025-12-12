import { useState } from 'react'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import logoIcon from '@/assets/images/logo_icon_boxed.png'
import { initiateGitHubLogin } from '@/lib/github-auth'
import { GithubLogo, Sparkle } from '@phosphor-icons/react'
import { BackgroundDecorations } from '@/components/BackgroundDecorations'
import { FloatingShapes } from '@/components/FloatingShapes'

export function LoginPage() {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleGitHubLogin = async () => {
    setLoading(true)
    setError(null)
    
    try {
      await initiateGitHubLogin()
    } catch (err) {
      console.error('Login error:', err)
      setError(err instanceof Error ? err.message : 'Failed to initiate login')
      setLoading(false)
    }
  }

  return (
    <div className="relative h-screen flex items-center justify-center bg-gradient-to-br from-background via-background to-muted/20 overflow-hidden">
      <BackgroundDecorations />
      <FloatingShapes />
      
      <div className="relative z-10 w-full max-w-md p-4">
        <Card className="backdrop-blur-sm bg-background/95 border-2 shadow-2xl">
          <div className="p-8">
            {/* Logo and Header */}
            <div className="text-center mb-8">
              <div className="relative inline-block mb-6">
                <img 
                  src={logoIcon} 
                  alt="promptArq logo" 
                  className="w-20 h-20 rounded-2xl mx-auto shadow-lg"
                />
                <div className="absolute -top-1 -right-1">
                  <Sparkle className="w-6 h-6 text-primary animate-pulse" weight="fill" />
                </div>
              </div>
              
              <h1 className="text-3xl font-bold mb-2 bg-gradient-to-r from-primary to-primary/60 bg-clip-text text-transparent">
                Welcome to promptArq
              </h1>
              <p className="text-muted-foreground">
                Your professional prompt library & management system
              </p>
            </div>

            {/* Features List */}
            <div className="mb-8 space-y-3">
              <div className="flex items-center gap-3 text-sm text-muted-foreground">
                <div className="w-2 h-2 rounded-full bg-primary animate-pulse" />
                <span>Organize prompts with projects, tags & categories</span>
              </div>
              <div className="flex items-center gap-3 text-sm text-muted-foreground">
                <div className="w-2 h-2 rounded-full bg-primary animate-pulse" style={{ animationDelay: '0.2s' }} />
                <span>Version control & collaboration with teams</span>
              </div>
              <div className="flex items-center gap-3 text-sm text-muted-foreground">
                <div className="w-2 h-2 rounded-full bg-primary animate-pulse" style={{ animationDelay: '0.4s' }} />
                <span>Execute prompts with AI models & MCP servers</span>
              </div>
            </div>

            {/* Error Alert */}
            {error && (
              <Alert variant="destructive" className="mb-6">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            {/* Login Button */}
            <Button
              onClick={handleGitHubLogin}
              disabled={loading}
              size="lg"
              className="w-full text-lg gap-3 shadow-lg hover:shadow-xl transition-all"
            >
              <GithubLogo className="w-6 h-6" weight="fill" />
              {loading ? 'Connecting...' : 'Sign in with GitHub'}
            </Button>

            {/* Info Text */}
            <p className="text-center text-xs text-muted-foreground mt-6">
              By signing in, you agree to our terms of service and privacy policy.
              <br />
              Your data is stored securely and never shared.
            </p>
          </div>
        </Card>

        {/* Additional Info */}
        <div className="text-center mt-6 text-sm text-muted-foreground">
          <p>
            Need help?{' '}
            <a 
              href="https://github.com/tamaygz/promptArq" 
              target="_blank" 
              rel="noopener noreferrer"
              className="text-primary hover:underline"
            >
              View Documentation
            </a>
          </p>
        </div>
      </div>
    </div>
  )
}
