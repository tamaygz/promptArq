import { useEffect, useState } from 'react'
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

  useEffect(() => {
    checkAuth()
  }, [])

  const checkAuth = async () => {
    try {
      const userData = await window.spark.user()
      setUser(userData)
    } catch (err) {
      console.error('Auth check failed:', err)
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
