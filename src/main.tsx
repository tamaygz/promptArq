import { createRoot } from 'react-dom/client'
import { ErrorBoundary } from "react-error-boundary";
import { initializeSpark } from './lib/spark-gateway'

import App from './App.tsx'
import { ErrorFallback } from './ErrorFallback.tsx'
import { AuthCallback } from './components/AuthCallback.tsx'
import { LoginPage } from './components/LoginPage.tsx'
import { isSparkEnvironment } from './lib/storage-adapter'
import { isAuthenticated } from './lib/github-auth'

// Initialize Spark safely (only loads if available)
initializeSpark()

import "./main.css"
import "./styles/theme.css"
import "./index.css"

// Simple client-side routing
function Router() {
  const path = window.location.pathname
  
  // Handle OAuth callback
  if (path === '/auth/callback') {
    return <AuthCallback provider="github" />
  }
  
  // Handle login page
  if (path === '/login') {
    // If already authenticated, redirect to home
    if (isAuthenticated() || isSparkEnvironment()) {
      window.location.href = '/'
      return null
    }
    return <LoginPage />
  }
  
  // Main app - handle authentication
  if (!isSparkEnvironment() && !isAuthenticated()) {
    window.location.href = '/login'
    return null
  }
  
  return <App />
}

createRoot(document.getElementById('root')!).render(
  <ErrorBoundary FallbackComponent={ErrorFallback}>
    <Router />
   </ErrorBoundary>
)
