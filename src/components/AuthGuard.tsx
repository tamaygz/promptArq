import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { GithubLogo, MicrosoftOutlookLogo } from '@phosphor-icons/react'
import { motion } from 'framer-motion'
import logoIcon from '@/assets/images/logo_icon_boxed.png'

type UserInfo = {
  id: string
  login: string
  email: string
  avatarUrl: string
  isOwner: boolean
}

type AuthGuardProps = {
  children: React.ReactNode
}

export function AuthGuard({ children }: AuthGuardProps) {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    checkAuth()
  }, [])

  const checkAuth = async () => {
    try {
      const userData = await window.spark.user()
      if (userData && userData.login) {
        setUser(userData)
      } else {
        setError('Please sign in to continue')
      }
    } catch (err) {
      console.error('Auth check failed:', err)
    } finally {
      setLoading(false)
    }
  }

  const handleGitHubLogin = () => {
    window.location.href = 'https://github.com/login'
  }

  const handleMicrosoftLogin = () => {
    const tenantId = 'common'
    const clientId = import.meta.env.VITE_MICROSOFT_CLIENT_ID || 'YOUR_MICROSOFT_CLIENT_ID'
    window.location.href = `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/authorize?client_id=${clientId}&response_type=code&redirect_uri=${window.location.origin}/auth/microsoft/callback&scope=openid%20profile%20email&response_mode=query`
  }

  if (loading) {
    return (
      <div className="h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <img src={logoIcon} alt="arqioly logo" className="w-16 h-16 rounded-2xl mx-auto mb-4 animate-pulse" />
          <p className="text-muted-foreground">Loading...</p>
        </div>
      </div>
    )
  }

  if (!user) {
    return (
      <div className="h-screen flex items-center justify-center bg-background p-4">
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.3 }}
        >
          <Card className="w-full max-w-md p-8">
            <div className="text-center mb-8">
              <img src={logoIcon} alt="arqioly logo" className="w-16 h-16 rounded-2xl mx-auto mb-4" />
              <h1 className="text-2xl font-semibold mb-2">Welcome to arqioly</h1>
              <p className="text-muted-foreground text-sm">
                Sign in to manage your LLM prompts with versioning, AI improvements, and team collaboration
              </p>
            </div>

            {error && (
              <div className="mb-6 p-3 bg-destructive/10 border border-destructive/20 rounded-lg text-sm text-destructive">
                {error}
              </div>
            )}

            <div className="space-y-3">
              <Button
                onClick={handleGitHubLogin}
                className="w-full"
                size="lg"
                variant="outline"
              >
                <GithubLogo size={20} weight="fill" />
                Continue with GitHub
              </Button>

              <Button
                onClick={handleMicrosoftLogin}
                className="w-full"
                size="lg"
                variant="outline"
              >
                <MicrosoftOutlookLogo size={20} weight="fill" />
                Continue with Microsoft
              </Button>
            </div>

            <p className="text-xs text-muted-foreground text-center mt-6">
              By signing in, you agree to our Terms of Service and Privacy Policy
            </p>
          </Card>
        </motion.div>
      </div>
    )
  }

  return <>{children}</>
}
