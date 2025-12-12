/**
 * GitHub OAuth Authentication Service
 * 
 * Implements OAuth 2.0 flow for GitHub authentication
 * using the Authorization Code Grant with PKCE
 */

// GitHub OAuth configuration
const GITHUB_CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENT_ID || ''
const GITHUB_REDIRECT_URI = import.meta.env.VITE_GITHUB_REDIRECT_URI || `${window.location.origin}/auth/callback`
const GITHUB_AUTH_URL = 'https://github.com/login/oauth/authorize'
const GITHUB_TOKEN_URL = 'https://github.com/login/oauth/access_token'
const GITHUB_API_URL = 'https://api.github.com'

export interface GitHubUser {
  id: string
  login: string
  email: string
  avatarUrl: string
  name: string | null
  bio: string | null
  location: string | null
  company: string | null
  isOwner: boolean
}

interface GitHubAPIUser {
  id: number
  login: string
  email: string | null
  avatar_url: string
  name: string | null
  bio: string | null
  location: string | null
  company: string | null
}

interface GitHubEmail {
  email: string
  primary: boolean
  verified: boolean
  visibility: string | null
}

interface TokenResponse {
  access_token: string
  token_type: string
  scope: string
}

/**
 * Generate a random string for PKCE code verifier
 */
function generateCodeVerifier(): string {
  const array = new Uint8Array(32)
  crypto.getRandomValues(array)
  return base64URLEncode(array)
}

/**
 * Generate code challenge from verifier for PKCE
 */
async function generateCodeChallenge(verifier: string): Promise<string> {
  const encoder = new TextEncoder()
  const data = encoder.encode(verifier)
  const hash = await crypto.subtle.digest('SHA-256', data)
  return base64URLEncode(new Uint8Array(hash))
}

/**
 * Base64 URL encode
 */
function base64URLEncode(buffer: Uint8Array): string {
  const base64 = btoa(String.fromCharCode(...buffer))
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '')
}

/**
 * Generate a random state parameter for CSRF protection
 */
function generateState(): string {
  const array = new Uint8Array(16)
  crypto.getRandomValues(array)
  return base64URLEncode(array)
}

/**
 * Initiate GitHub OAuth flow
 */
export async function initiateGitHubLogin(): Promise<void> {
  if (!GITHUB_CLIENT_ID) {
    throw new Error('GitHub Client ID not configured. Please set VITE_GITHUB_CLIENT_ID in your .env file.')
  }

  // Generate and store PKCE parameters
  const codeVerifier = generateCodeVerifier()
  const codeChallenge = await generateCodeChallenge(codeVerifier)
  const state = generateState()

  // Store for verification after redirect
  sessionStorage.setItem('github_code_verifier', codeVerifier)
  sessionStorage.setItem('github_state', state)

  // Build authorization URL
  const params = new URLSearchParams({
    client_id: GITHUB_CLIENT_ID,
    redirect_uri: GITHUB_REDIRECT_URI,
    scope: 'read:user user:email',
    state: state,
    code_challenge: codeChallenge,
    code_challenge_method: 'S256',
    allow_signup: 'true'
  })

  // Redirect to GitHub authorization
  window.location.href = `${GITHUB_AUTH_URL}?${params.toString()}`
}

/**
 * Handle OAuth callback and exchange code for token
 */
export async function handleGitHubCallback(
  code: string,
  state: string
): Promise<GitHubUser> {
  // Verify state to prevent CSRF
  const storedState = sessionStorage.getItem('github_state')
  if (!storedState || storedState !== state) {
    throw new Error('Invalid state parameter - possible CSRF attack')
  }

  const codeVerifier = sessionStorage.getItem('github_code_verifier')
  if (!codeVerifier) {
    throw new Error('Code verifier not found - session may have expired')
  }

  // Clean up session storage
  sessionStorage.removeItem('github_state')
  sessionStorage.removeItem('github_code_verifier')

  // Exchange authorization code for access token
  const token = await exchangeCodeForToken(code, codeVerifier)
  
  // Store the access token
  localStorage.setItem('github_access_token', token)

  // Fetch and store user data
  const user = await fetchGitHubUser(token)
  localStorage.setItem('github_user', JSON.stringify(user))

  return user
}

/**
 * Exchange authorization code for access token
 * Uses backend proxy to keep client_secret secure
 */
async function exchangeCodeForToken(
  code: string,
  codeVerifier: string
): Promise<string> {
  const proxyUrl = import.meta.env.VITE_OAUTH_PROXY_URL || 'http://localhost:3001'
  
  try {
    const response = await fetch(`${proxyUrl}/api/auth/github/token`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        code: code,
        code_verifier: codeVerifier
      })
    })

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ error: 'Unknown error' }))
      throw new Error(`Token exchange failed: ${errorData.error || response.statusText}`)
    }

    const data: TokenResponse = await response.json()
    
    if (!data.access_token) {
      throw new Error('No access token received from proxy server')
    }

    return data.access_token
  } catch (error) {
    console.error('Token exchange error:', error)
    throw new Error('Failed to exchange authorization code for token. Make sure the proxy server is running.')
  }
}

/**
 * Fetch GitHub user profile
 */
async function fetchGitHubUser(token: string): Promise<GitHubUser> {
  try {
    // Fetch user profile
    const userResponse = await fetch(`${GITHUB_API_URL}/user`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/vnd.github.v3+json'
      }
    })

    if (!userResponse.ok) {
      throw new Error('Failed to fetch user profile')
    }

    const userData: GitHubAPIUser = await userResponse.json()

    // Fetch user emails if not included
    let email = userData.email
    if (!email) {
      const emailsResponse = await fetch(`${GITHUB_API_URL}/user/emails`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Accept': 'application/vnd.github.v3+json'
        }
      })

      if (emailsResponse.ok) {
        const emails: GitHubEmail[] = await emailsResponse.json()
        const primaryEmail = emails.find(e => e.primary && e.verified)
        email = primaryEmail?.email || emails[0]?.email || 'no-email@github.user'
      } else {
        email = 'no-email@github.user'
      }
    }

    return {
      id: userData.id.toString(),
      login: userData.login,
      email: email,
      avatarUrl: userData.avatar_url,
      name: userData.name,
      bio: userData.bio,
      location: userData.location,
      company: userData.company,
      isOwner: true // First user is considered owner, can be enhanced
    }
  } catch (error) {
    console.error('Failed to fetch GitHub user:', error)
    throw new Error('Failed to fetch user profile from GitHub')
  }
}

/**
 * Get the currently logged in user
 */
export function getCurrentUser(): GitHubUser | null {
  const userJson = localStorage.getItem('github_user')
  if (!userJson) return null

  try {
    return JSON.parse(userJson)
  } catch {
    return null
  }
}

/**
 * Get the current access token
 */
export function getAccessToken(): string | null {
  return localStorage.getItem('github_access_token')
}

/**
 * Check if user is authenticated
 */
export function isAuthenticated(): boolean {
  const token = getAccessToken()
  const user = getCurrentUser()
  return !!(token && user)
}

/**
 * Logout user
 */
export function logout(): void {
  localStorage.removeItem('github_access_token')
  localStorage.removeItem('github_user')
  window.location.href = '/'
}

/**
 * Validate token and refresh user data
 */
export async function validateAndRefreshUser(): Promise<GitHubUser | null> {
  const token = getAccessToken()
  if (!token) return null

  try {
    // Verify token is still valid by fetching user
    const response = await fetch(`${GITHUB_API_URL}/user`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Accept': 'application/vnd.github.v3+json'
      }
    })

    if (!response.ok) {
      // Token is invalid, clear storage
      logout()
      return null
    }

    // Refresh user data
    const user = await fetchGitHubUser(token)
    localStorage.setItem('github_user', JSON.stringify(user))
    return user
  } catch (error) {
    console.error('Failed to validate token:', error)
    logout()
    return null
  }
}
