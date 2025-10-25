import { useEffect, useState } from 'react'
import { Card } from '@/components/ui/card'
import logoIcon from '@/assets/images/logo_icon_boxed.png'

type CallbackProps = {
  provider: 'github' | 'microsoft'
}

export function AuthCallback({ provider }: CallbackProps) {
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    handleCallback()
  }, [])

  const handleCallback = async () => {
    try {
      const params = new URLSearchParams(window.location.search)
      const code = params.get('code')
      const error = params.get('error')

      if (error) {
        throw new Error(`Authentication failed: ${error}`)
      }

      if (!code) {
        throw new Error('No authorization code received')
      }

      setStatus('success')
      
      setTimeout(() => {
        window.location.href = '/'
      }, 1500)
      
    } catch (err) {
      console.error('Auth callback error:', err)
      setError(err instanceof Error ? err.message : 'Authentication failed')
      setStatus('error')
    }
  }

  return (
    <div className="h-screen flex items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md p-8">
        <div className="text-center">
          <img 
            src={logoIcon} 
            alt="arqioly logo" 
            className={`w-16 h-16 rounded-2xl mx-auto mb-4 ${status === 'loading' ? 'animate-pulse' : ''}`}
          />
          
          {status === 'loading' && (
            <>
              <h2 className="text-xl font-semibold mb-2">Authenticating...</h2>
              <p className="text-muted-foreground text-sm">
                Completing {provider === 'github' ? 'GitHub' : 'Microsoft'} authentication
              </p>
            </>
          )}

          {status === 'success' && (
            <>
              <h2 className="text-xl font-semibold mb-2 text-primary">Success!</h2>
              <p className="text-muted-foreground text-sm">
                Redirecting to your dashboard...
              </p>
            </>
          )}

          {status === 'error' && (
            <>
              <h2 className="text-xl font-semibold mb-2 text-destructive">Authentication Failed</h2>
              <p className="text-muted-foreground text-sm mb-4">{error}</p>
              <a href="/" className="text-primary text-sm hover:underline">
                Return to home
              </a>
            </>
          )}
        </div>
      </Card>
    </div>
  )
}
