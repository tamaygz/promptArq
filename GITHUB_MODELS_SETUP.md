# GitHub Models API - Server-Side Token Setup

This document explains how to use GitHub Models API with environment variable tokens for server-side deployments, CI/CD, or testing.

## Overview

By default, promptArq uses GitHub OAuth flow for authentication. However, you can also use a GitHub Personal Access Token via environment variable for:

- **Server-side rendering** (SSR)
- **CI/CD pipelines**
- **Automated testing**
- **Development environments** where OAuth flow is inconvenient

## Quick Start

### 1. Create a GitHub Personal Access Token

1. Go to [GitHub Settings > Tokens](https://github.com/settings/tokens)
2. Click **"Generate new token"** → **"Generate new token (classic)"**
3. Configure the token:
   - **Name**: `promptArq Development` (or any descriptive name)
   - **Expiration**: Choose appropriate expiration
   - **Scopes**: Select:
     - ✅ `read:user` - Read user profile data
     - ✅ `user:email` - Access user email addresses
4. Click **"Generate token"**
5. **Copy the token immediately** (you won't see it again!)

### 2. Set Environment Variable

Add the token to your `.env` file:

```bash
# .env
VITE_GITHUB_TOKEN=ghp_your_personal_access_token_here
```

### 3. Restart Development Server

```bash
npm run dev
```

The app will automatically detect and use the environment variable token.

## How It Works

### Token Priority

The authentication system checks for tokens in this order:

1. **Environment Variable** (`VITE_GITHUB_TOKEN`) - Highest priority
2. **OAuth Token** (from localStorage) - Fallback

If `VITE_GITHUB_TOKEN` is set, the OAuth flow is bypassed completely.

### User Profile

When using an environment token:
- User profile is fetched automatically on app startup
- User data is cached in `localStorage` as `github_user_env`
- The profile page shows "Environment Variable" authentication method
- Sign out button is disabled (can't logout from env token)

### GitHub Models API

All AI features work the same way:
- "Improve Prompt" ✅
- "Generate Title" ✅
- "Execute Prompt" ✅
- Rate limiting ✅
- Usage tracking ✅

## Security Considerations

### ⚠️ IMPORTANT

- **Never commit tokens to version control**
- **Add `.env` to `.gitignore`** (already done)
- **Use tokens with minimal required scopes**
- **Rotate tokens regularly**
- **Use different tokens for dev/staging/prod**

### Environment-Specific Tokens

```bash
# Development
VITE_GITHUB_TOKEN=ghp_dev_token_here

# Staging
VITE_GITHUB_TOKEN=ghp_staging_token_here

# Production
VITE_GITHUB_TOKEN=ghp_prod_token_here
```

### CI/CD Usage

#### GitHub Actions

```yaml
# .github/workflows/test.yml
name: Test

on: [push]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Install dependencies
        run: npm ci
      
      - name: Run tests
        env:
          VITE_GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: npm test
```

#### Docker

```dockerfile
# Dockerfile
FROM node:20-alpine

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Build with env var support
ARG VITE_GITHUB_TOKEN
ENV VITE_GITHUB_TOKEN=$VITE_GITHUB_TOKEN

RUN npm run build

EXPOSE 5173
CMD ["npm", "run", "preview"]
```

Run with:
```bash
docker build --build-arg VITE_GITHUB_TOKEN=ghp_xxx -t promptarq .
docker run -p 5173:5173 promptarq
```

## Comparison: OAuth vs Environment Token

| Feature | OAuth Flow | Environment Token |
|---------|-----------|-------------------|
| **Setup Complexity** | Medium (OAuth app + proxy) | Easy (just token) |
| **User Experience** | Login button + redirect | Automatic |
| **Security** | Token stored in localStorage | Token in environment |
| **Token Expiration** | ~8 hours (auto-refresh) | Based on token settings |
| **Best For** | End-user applications | Server-side, CI/CD, testing |
| **Sign Out** | ✅ Supported | ❌ Disabled |
| **Per-User Auth** | ✅ Yes | ❌ Single account |

## Troubleshooting

### Token Not Working

1. **Check token scopes**: Must have `read:user` and `user:email`
2. **Check token expiration**: Generate a new one if expired
3. **Check environment variable name**: Must be exactly `VITE_GITHUB_TOKEN`
4. **Restart dev server**: Environment variables only loaded on startup

### User Profile Not Loading

Check browser console for errors:
```
Failed to fetch user with env token: ...
```

This usually means:
- Token is invalid or expired
- Token doesn't have required scopes
- GitHub API is down

### Rate Limiting

GitHub Models API has rate limits:
- **50 requests/minute**
- **500 requests/hour**

The app handles this automatically with:
- Request queuing
- Exponential backoff
- Clear error messages

See usage statistics in User Profile → AI Usage Statistics.

## Advanced Configuration

### Custom Token Validation

You can manually validate a token:

```typescript
import { getAccessToken, fetchUserWithEnvToken } from '@/lib/github-auth'

// Check if env token is available
const token = getAccessToken()
console.log('Token:', token ? 'Present' : 'Missing')

// Fetch user with env token
const user = await fetchUserWithEnvToken()
console.log('User:', user)
```

### Disable OAuth When Using Env Token

Modify `main.tsx` to skip login page:

```typescript
// main.tsx
import { isUsingEnvToken } from './lib/github-auth'

function Router() {
  const path = window.location.pathname
  
  // Skip authentication if using env token
  if (isUsingEnvToken()) {
    return <App />
  }
  
  // ... rest of routing logic
}
```

## Support

For issues or questions:
- Check [GitHub Models documentation](https://github.com/marketplace/models)
- Open an issue on the promptArq repository
- Review error messages in browser console
