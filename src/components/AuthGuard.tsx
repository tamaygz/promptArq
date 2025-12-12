import { useEffect, useState } from 'react'
import logoIcon from '@/assets/images/logo_icon_boxed.png'
import { isSparkEnvironment } from '@/lib/storage-adapter'
import { isAuthenticated, validateAndRefreshUser } from '@/lib/github-auth'

type UserInfo = {
  id: string
  login: string
  email: string
  avatarUrl: string
  isOwner: boolean
}

type AuthGuardProps = {
  children: React.ReactNode
  onUnauthenticated?: () => void
}

export function AuthGuard({ children, onUnauthenticated }: AuthGuardProps) {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    checkAuth()
  }, [])

  const checkAuth = async () => {
    try {
      // Check if running in Spark environment
      if (isSparkEnvironment()) {
        // Use Spark authentication
        const userData = await window.spark.user()
        setUser(userData)
      } else {
        // Use GitHub OAuth authentication
        if (!isAuthenticated()) {
          // Not authenticated, trigger unauthenticated callback
          if (onUnauthenticated) {
            onUnauthenticated()
          }
          setLoading(false)
          return
        }

        // Validate and refresh user data
        const githubUser = await validateAndRefreshUser()
        if (!githubUser) {
          // Token invalid, trigger unauthenticated callback
          if (onUnauthenticated) {
            onUnauthenticated()
          }
          setLoading(false)
          return
        }

        setUser(githubUser)
      }
    } catch (err) {
      console.error('Auth check failed:', err)
      if (!isSparkEnvironment() && onUnauthenticated) {
        onUnauthenticated()
      }
    } finally {
      setLoading(false)
    }
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

  return <>{children}</>
}
