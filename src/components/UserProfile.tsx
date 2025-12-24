import { useState, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { SignOut, Crown, GithubLogo, MicrosoftOutlookLogo, ChartBar, Download, Key } from '@phosphor-icons/react'
import { User } from '@/lib/types'
import { getSparkUser } from '@/lib/spark-utils'
import { logout as githubLogout, isUsingEnvToken } from '@/lib/github-auth'
import { isSparkEnvironment } from '@/lib/storage-adapter'
import { hasGitHubModelsSupport, getCurrentRateLimitStatus } from '@/lib/github-models-client'
import { getUsageSummary, exportUsageAsCSV, clearUsageHistory } from '@/lib/token-usage-logger'

type UserProfileProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  users: User[]
  onUpdateUsers: (users: User[] | ((current: User[]) => User[])) => void
}

export function UserProfile({ open, onOpenChange, users, onUpdateUsers }: UserProfileProps) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [usageStats, setUsageStats] = useState<any>(null)
  const [rateLimitStatus, setRateLimitStatus] = useState<any>(null)

  useEffect(() => {
    if (open) {
      loadUser()
      loadUsageStats()
    }
  }, [open])

  const loadUsageStats = () => {
    if (!isSparkEnvironment() && hasGitHubModelsSupport()) {
      const stats = getUsageSummary()
      const rateLimit = getCurrentRateLimitStatus()
      setUsageStats(stats)
      setRateLimitStatus(rateLimit)
    }
  }

  const loadUser = async () => {
    try {
      const userData = await getSparkUser()
      if (!userData) {
        setLoading(false)
        return
      }
      
      const userId = String(userData.id)
      let existingUser = users.find(u => u.id === userId)
      
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
        
        onUpdateUsers((current) => [...(current || []), existingUser!])
      } else {
        existingUser.lastLoginAt = Date.now()
        onUpdateUsers((current) => 
          (current || []).map(u => u.id === existingUser!.id ? existingUser! : u)
        )
      }
      
      setUser(existingUser)
    } catch (err) {
      console.error('Failed to load user:', err)
    } finally {
      setLoading(false)
    }
  }

  const handleSignOut = () => {
    if (isSparkEnvironment()) {
      window.location.href = '/auth/logout'
    } else {
      // GitHub OAuth logout
      githubLogout()
    }
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
      <DialogContent className="max-w-lg">
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

                {isUsingEnvToken() && (
                  <>
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Authentication Method</span>
                      <div className="flex items-center gap-2">
                        <Key size={16} weight="fill" className="text-blue-500" />
                        <Badge variant="outline" className="text-xs">Environment Variable</Badge>
                      </div>
                    </div>
                    <Separator />
                  </>
                )}

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

            {!isSparkEnvironment() && hasGitHubModelsSupport() && usageStats && (
              <div>
                <h4 className="text-sm font-medium mb-2 flex items-center gap-2">
                  <ChartBar size={16} />
                  AI Usage Statistics
                </h4>
                <Card className="p-4 space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Total Requests</span>
                    <span className="text-sm font-medium">{usageStats.totalRequests}</span>
                  </div>

                  <Separator />

                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Total Tokens Used</span>
                    <span className="text-sm font-medium">{usageStats.totalTokens.toLocaleString()}</span>
                  </div>

                  <Separator />

                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Today (24h)</span>
                    <span className="text-sm font-medium">{usageStats.todayRequests} requests</span>
                  </div>

                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">This Week</span>
                    <span className="text-sm font-medium">{usageStats.weekRequests} requests</span>
                  </div>

                  <Separator />

                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Most Used Model</span>
                    <Badge variant="outline" className="text-xs">{usageStats.topModel}</Badge>
                  </div>

                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted-foreground">Avg Tokens/Request</span>
                    <span className="text-sm font-medium">{usageStats.avgTokensPerRequest}</span>
                  </div>

                  {rateLimitStatus && (
                    <>
                      <Separator />
                      <div className="space-y-2">
                        <div className="flex items-center justify-between">
                          <span className="text-xs text-muted-foreground">Rate Limit (per minute)</span>
                          <span className="text-xs font-medium">
                            {rateLimitStatus.requestsPerMinute} / {rateLimitStatus.maxRequestsPerMinute}
                          </span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-xs text-muted-foreground">Rate Limit (per hour)</span>
                          <span className="text-xs font-medium">
                            {rateLimitStatus.requestsPerHour} / {rateLimitStatus.maxRequestsPerHour}
                          </span>
                        </div>
                      </div>
                    </>
                  )}

                  <Separator />

                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      className="flex-1 text-xs"
                      onClick={() => {
                        const csv = exportUsageAsCSV()
                        const blob = new Blob([csv], { type: 'text/csv' })
                        const url = URL.createObjectURL(blob)
                        const a = document.createElement('a')
                        a.href = url
                        a.download = `promptarq-usage-${Date.now()}.csv`
                        a.click()
                      }}
                    >
                      <Download size={14} />
                      Export CSV
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="flex-1 text-xs"
                      onClick={() => {
                        if (confirm('Clear all usage history? This cannot be undone.')) {
                          clearUsageHistory()
                          loadUsageStats()
                        }
                      }}
                    >
                      Clear History
                    </Button>
                  </div>
                </Card>
              </div>
            )}
          </div>

          {!isUsingEnvToken() && (
            <Button
              onClick={handleSignOut}
              variant="outline"
              className="w-full"
            >
              <SignOut size={16} />
              Sign Out
            </Button>
          )}
          
          {isUsingEnvToken() && (
            <div className="text-xs text-center text-muted-foreground p-2 bg-muted rounded">
              Using environment variable token. Sign out is disabled.
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
