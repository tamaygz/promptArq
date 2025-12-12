# GitHub OAuth - Quick Reference

## 🚀 Quick Setup (5 minutes)

### 1. Create GitHub OAuth App
Visit: https://github.com/settings/developers
- Click "New OAuth App"
- Homepage URL: `http://localhost:5173`
- Callback URL: `http://localhost:5173/auth/callback`
- Copy Client ID and Client Secret

### 2. Configure Environment
```bash
cp .env.example .env
```

Edit `.env`:
```env
VITE_GITHUB_CLIENT_ID=your_client_id
VITE_GITHUB_CLIENT_SECRET=your_client_secret
```

### 3. Run
```bash
npm install
npm run dev
```

This starts **both servers**:
- 🔐 OAuth Proxy: http://localhost:3001
- 🌐 Vite App: http://localhost:5173

Visit http://localhost:5173 and sign in!

---

## 🔑 API Quick Reference

```typescript
import { 
  initiateGitHubLogin,
  getCurrentUser,
  isAuthenticated,
  logout 
} from '@/lib/github-auth'

// Start login flow
await initiateGitHubLogin()

// Check auth status
if (isAuthenticated()) {
  const user = getCurrentUser()
  console.log(user.login, user.email)
}

// Logout
logout()
```

---

## 🌐 Routes

| Route | Purpose | Auth Required |
|-------|---------|---------------|
| `/` | Main app | ✅ Yes |
| `/login` | Login page | ❌ No |
| `/auth/callback` | OAuth callback | ❌ No |

---

## 🔒 Environment Detection

```typescript
import { isSparkEnvironment } from '@/lib/storage-adapter'

if (isSparkEnvironment()) {
  // Use Spark auth
  const user = await window.spark.user()
} else {
  // Use GitHub OAuth
  const user = getCurrentUser()
}
```

---

## 🐛 Common Issues

### "Client ID not configured"
- Check `.env` file exists
- Restart dev server after creating `.env`

### "Invalid state parameter"
- Clear browser cache
- Try again in incognito mode

### "Failed to exchange code"
- Verify callback URL matches exactly
- Check client secret is correct

---

## 📚 Full Documentation

- **Setup Guide**: [OAUTH_SETUP.md](./OAUTH_SETUP.md)
- **Implementation Details**: [GITHUB_AUTH_INTEGRATION.md](./GITHUB_AUTH_INTEGRATION.md)
- **GitHub Docs**: https://docs.github.com/en/apps/oauth-apps

---

## ⚙️ Configuration Options

```env
# Required
VITE_GITHUB_CLIENT_ID=abc123...

# Required (see security note in OAUTH_SETUP.md)
VITE_GITHUB_CLIENT_SECRET=secret123...

# Optional (defaults to current origin + /auth/callback)
VITE_GITHUB_REDIRECT_URI=http://localhost:5173/auth/callback
```

---

## 🔐 User Object

```typescript
interface GitHubUser {
  id: string              // GitHub user ID
  login: string           // GitHub username
  email: string           // Primary email
  avatarUrl: string       // Profile picture
  name: string | null     // Display name
  bio: string | null      // User bio
  location: string | null // Location
  company: string | null  // Company
  isOwner: boolean        // App owner flag
}
```

---

## 🎯 Production Checklist

- [ ] Create separate GitHub OAuth app for production
- [ ] Update redirect URI to production domain
- [ ] Implement backend proxy for token exchange
- [ ] Remove client secret from frontend
- [ ] Enable HTTPS
- [ ] Test login flow
- [ ] Test logout flow
- [ ] Test token refresh

---

**Need Help?** Check the [full setup guide](./OAUTH_SETUP.md) or open an issue.
