# GitHub OAuth Flow - Architecture

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          User's Browser                              │
│                                                                       │
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │              promptArq App (React/Vite)                        │ │
│  │              http://localhost:5173                             │ │
│  │                                                                │ │
│  │  • Login Page                                                  │ │
│  │  • OAuth Callback Handler                                     │ │
│  │  • Token Storage (localStorage)                               │ │
│  └────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
                               ↕
                    ═══════════════════════
                    ║  HTTPS (OAuth)      ║
                    ═══════════════════════
                               ↕
┌─────────────────────────────────────────────────────────────────────┐
│                        GitHub OAuth Service                          │
│                   https://github.com/login/oauth                     │
│                                                                       │
│  • Authorization Endpoint                                            │
│  • Token Exchange Endpoint (CORS protected)                          │
│  • User API                                                          │
└─────────────────────────────────────────────────────────────────────┘
                               ↕
                    ═══════════════════════
                    ║  HTTP (Internal)    ║
                    ═══════════════════════
                               ↕
┌─────────────────────────────────────────────────────────────────────┐
│                     OAuth Proxy Server                               │
│                    http://localhost:3001                             │
│                         (server.js)                                  │
│                                                                       │
│  • Token Exchange Proxy                                              │
│  • Keeps Client Secret Secure                                        │
│  • CORS Enabled for Frontend                                         │
│  • Environment Variables (.env)                                      │
└─────────────────────────────────────────────────────────────────────┘
```

## OAuth Flow Sequence

```
User                Browser               GitHub              Proxy Server
 │                     │                    │                      │
 │  1. Click Login     │                    │                      │
 ├────────────────────>│                    │                      │
 │                     │                    │                      │
 │                     │  2. Redirect       │                      │
 │                     ├───────────────────>│                      │
 │                     │   with client_id   │                      │
 │                     │   & PKCE params    │                      │
 │                     │                    │                      │
 │  3. Authorize App   │                    │                      │
 ├────────────────────────────────────────>│                      │
 │  (GitHub Login)     │                    │                      │
 │                     │                    │                      │
 │                     │  4. Redirect back  │                      │
 │                     │<───────────────────┤                      │
 │                     │   with code        │                      │
 │                     │                    │                      │
 │                     │  5. POST code      │                      │
 │                     ├──────────────────────────────────────────>│
 │                     │   to proxy         │                      │
 │                     │                    │                      │
 │                     │                    │  6. Exchange code    │
 │                     │                    │<─────────────────────┤
 │                     │                    │   with secret        │
 │                     │                    │                      │
 │                     │                    │  7. Return token     │
 │                     │                    ├─────────────────────>│
 │                     │                    │                      │
 │                     │  8. Return token   │                      │
 │                     │<──────────────────────────────────────────┤
 │                     │   (no secret)      │                      │
 │                     │                    │                      │
 │  9. Logged In!      │                    │                      │
 │<────────────────────┤                    │                      │
 │                     │                    │                      │
```

## Security Model

### What's Public (Frontend)
- ✅ Client ID (public, safe to expose)
- ✅ Authorization Code (single-use, short-lived)
- ✅ PKCE Code Verifier (adds extra security)
- ✅ Access Token (stored securely, can be used)

### What's Private (Backend)
- 🔒 Client Secret (NEVER exposed to browser)
- 🔒 Token Exchange Logic
- 🔒 Environment Variables

### PKCE (Proof Key for Code Exchange)
```
Frontend:
1. Generate random code_verifier (32 bytes)
2. Hash it: code_challenge = SHA256(code_verifier)
3. Send code_challenge to GitHub

Backend:
4. Receive code_verifier from frontend
5. Include in token exchange
6. GitHub verifies: SHA256(code_verifier) == code_challenge
```

## File Structure

```
promptArq/
├── server.js                    # OAuth Proxy Server ⭐
│   ├── Express.js application
│   ├── CORS enabled
│   ├── Token exchange endpoint
│   └── Reads .env for secrets
│
├── src/
│   ├── lib/
│   │   └── github-auth.ts      # Auth Service ⭐
│   │       ├── initiateGitHubLogin()
│   │       ├── handleGitHubCallback()
│   │       └── Uses proxy server
│   │
│   ├── components/
│   │   ├── LoginPage.tsx       # Login UI
│   │   ├── AuthCallback.tsx    # Callback Handler
│   │   └── AuthGuard.tsx       # Auth Protection
│   │
│   └── main.tsx                # Router
│
├── .env                         # Secrets (git-ignored) 🔒
│   ├── VITE_GITHUB_CLIENT_ID
│   ├── VITE_GITHUB_CLIENT_SECRET
│   └── PROXY_PORT=3001
│
└── package.json
    └── scripts:
        ├── dev: "concurrently server + client"
        ├── server: "node server.js"
        └── client: "vite"
```

## Why We Need a Proxy

### ❌ Direct Token Exchange (Doesn't Work)
```
Browser → GitHub Token Endpoint
         ❌ CORS Error!
         ❌ Client secret exposed!
```

### ✅ Proxy-Based Token Exchange (Secure)
```
Browser → Proxy Server → GitHub Token Endpoint
         ✅ No CORS issues
         ✅ Client secret safe on server
         ✅ Production ready
```

## Development vs Production

### Development (Current Setup)
```
Frontend:  http://localhost:5173 (Vite)
Backend:   http://localhost:3001 (Node.js)
Database:  SQLite (local file)
```

### Production (Example)
```
Frontend:  https://promptarq.app (Vercel/Netlify)
Backend:   https://api.promptarq.app (Heroku/Railway)
Database:  SQLite or PostgreSQL
```

## Testing the Setup

1. **Check Proxy Server**
   ```bash
   curl http://localhost:3001/health
   # Should return: {"status":"ok","service":"promptArq OAuth Proxy"}
   ```

2. **Check Frontend**
   ```bash
   # Visit: http://localhost:5173
   # Should show login page if not authenticated
   ```

3. **Complete OAuth Flow**
   - Click "Sign in with GitHub"
   - Authorize on GitHub
   - Should redirect back and log you in
   - No CORS errors in console!

## Troubleshooting

### Port Already in Use
```bash
# Kill process on port 3001
npx kill-port 3001

# Or change port in .env
PROXY_PORT=3002
```

### Proxy Not Starting
```bash
# Check logs in terminal
# Verify .env file exists
# Ensure dependencies installed: npm install
```

### CORS Errors
```bash
# Verify proxy is running
# Check VITE_OAUTH_PROXY_URL matches
# Default: http://localhost:3001
```

---

**Key Points:**
- 🔐 Client secret stays on server (secure)
- 🌐 CORS handled by proxy (no browser errors)
- 🚀 Same code for dev and production
- ✅ Industry-standard OAuth implementation
