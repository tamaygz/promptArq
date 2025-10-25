import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { SignOut, Crown, GithubLogo, MicrosoftOutlookLogo } from '@phosphor-icons/react'
import { User } from '@/lib/types'

type UserProfileProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function UserProfile({ open, onOpenChange }: UserProfileProps) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (open) {
      loadUser()
    }
  }, [open])

  const loadUser = async () => {
    try {
      const userData = await window.spark.user()
      const storedUsers = await window.spark.kv.get<User[]>('users') || []
      
      const userId = String(userData.id)
      let existingUser = storedUsers.find(u => u.id === userId)
      
      if (!existingUser) {
        existingUser = {
          id: userId,
          login: userData.login,
          email: userData.email,
          avatarUrl: userData.avatarUrl,
          provider: userData.login.includes('@') ? 'microsoft' : 'github',
          isOwner: userData.isOwner,
          createdAt: Date.now(),
          lastLoginAt: Date.now()
        }
        
        await window.spark.kv.set('users', [...storedUsers, existingUser])
      } else {
        existingUser.lastLoginAt = Date.now()
        const updatedUsers = storedUsers.map(u => u.id === existingUser!.id ? existingUser! : u)
        await window.spark.kv.set('users', updatedUsers)
      }
      
      setUser(existingUser)
    } catch (err) {
      console.error('Failed to load user:', err)
    } finally {
      setLoading(false)
    }
  }

  const handleSignOut = () => {
    window.location.href = '/auth/logout'
  }

  if (loading || !user) {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent>
          <div className="flex items-center justify-center p-8">
            <p className="text-muted-foreground">Loading profile...</p>
          </div>
        </DialogContent>
      </Dialog>
    )
  }

  const getInitials = (name: string) => {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
  }

  const formatDate = (timestamp: number) => {
    return new Date(timestamp).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    })
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>User Profile</DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          <div className="flex items-center gap-4">
            <Avatar className="w-16 h-16">
              <AvatarImage src={user.avatarUrl} alt={user.login} />
              <AvatarFallback>{getInitials(user.login)}</AvatarFallback>
            </Avatar>
            
            <div className="flex-1">
              <div className="flex items-center gap-2 mb-1">
                <h3 className="font-semibold text-lg">{user.name || user.login}</h3>
                {user.isOwner && (
                  <Badge variant="secondary" className="gap-1">
                    <Crown size={12} weight="fill" />
                    Owner
                  </Badge>
                )}
              </div>
              <p className="text-sm text-muted-foreground">{user.email}</p>
            </div>
          </div>

          <Separator />

          <div className="space-y-4">
            <div>
              <h4 className="text-sm font-medium mb-2">Account Details</h4>
              <Card className="p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">User ID</span>
                  <span className="text-xs font-mono text-muted-foreground">{String(user.id).slice(0, 12)}...</span>
                </div>

                <Separator />

                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Provider</span>
                  <div className="flex items-center gap-2">
                    {user.provider === 'github' ? (
                      <>
                        <GithubLogo size={16} weight="fill" />
                        <span className="text-sm font-medium">GitHub</span>
                      </>
                    ) : (
                      <>
                        <MicrosoftOutlookLogo size={16} weight="fill" />
                        <span className="text-sm font-medium">Microsoft</span>
                      </>
                    )}
                  </div>
                </div>

                <Separator />

                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Account Created</span>
                  <span className="text-sm font-medium">{formatDate(user.createdAt)}</span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Last Login</span>
                  <span className="text-sm font-medium">{formatDate(user.lastLoginAt)}</span>
                </div>
              </Card>
            </div>
          </div>

          <Button
            onClick={handleSignOut}
            variant="outline"
            className="w-full"
          >
            <SignOut size={16} />
            Sign Out
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
