# Authentication Setup Guide

This application supports two authentication modes:

1. **Spark Runtime Authentication** (when running in Spark environment)
2. **GitHub OAuth** (standalone mode)

## Authentication Modes

### Spark Runtime Authentication (Automatic)

When running in the Spark environment, authentication is handled automatically through the `spark.user()` API. No configuration is required.

The Spark runtime provides:
- Automatic authentication flow
- Session management
- User profile data (id, login, email, avatarUrl, isOwner)

### GitHub OAuth (Standalone Mode)

When running outside the Spark environment (e.g., local development, Windows app), the application uses GitHub OAuth for authentication.

## GitHub OAuth Setup

### Step 1: Create a GitHub OAuth App

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click **"New OAuth App"**
3. Fill in the application details:

   **For Local Development:**
   - **Application name:** promptArq Development (or your preferred name)
   - **Homepage URL:** `http://localhost:5173`
   - **Authorization callback URL:** `http://localhost:5173/auth/callback`

   **For Production:**
   - **Application name:** promptArq
   - **Homepage URL:** Your production domain (e.g., `https://promptarq.app`)
   - **Authorization callback URL:** `https://your-domain.com/auth/callback`

4. Click **"Register application"**
5. Copy the **Client ID** displayed on the next page
6. Click **"Generate a new client secret"** and copy the secret immediately (you won't be able to see it again)

### Step 2: Configure Environment Variables

1. Copy the `.env.example` file to create `.env`:
   ```bash
   cp .env.example .env
   ```

2. Edit the `.env` file and add your GitHub OAuth credentials:
   ```env
   VITE_GITHUB_CLIENT_ID=your_github_client_id_here
   VITE_GITHUB_CLIENT_SECRET=your_github_client_secret_here
   ```

3. (Optional) Customize the redirect URI if needed:
   ```env
   VITE_GITHUB_REDIRECT_URI=http://localhost:5173/auth/callback
   ```

### Step 3: Run the Application

```bash
npm install
npm run dev
```

This will start **both**:
- **OAuth Proxy Server** on http://localhost:3001 (handles token exchange)
- **Vite Dev Server** on http://localhost:5173 (your app)

The application will now:
1. Check if running in Spark environment
2. If not, use GitHub OAuth for authentication
3. Redirect unauthenticated users to the login page
4. Handle OAuth callback via proxy server
5. Store user session securely

## Security Considerations

### ✅ Secure Backend Proxy

**Development & Production:**
This implementation includes a secure backend proxy server (`server.js`) that handles token exchange. The client secret **never leaves the server**.

**How It Works:**

1. **Frontend** (browser):
   - Initiates OAuth flow with GitHub
   - Receives authorization code
   - Sends code to backend proxy

2. **Backend Proxy** (Node.js/Express):
   - Receives authorization code from frontend
   - Exchanges code for access token with GitHub
   - Keeps client secret secure on server
   - Returns only the access token to frontend

3. **Security Benefits**:
   - ✅ Client secret never exposed to browser
   - ✅ CORS properly handled by proxy
   - ✅ Same code works for dev and production
   - ✅ Easy to add additional security layers

**The Proxy Server:**

Located in `server.js`, it runs on port 3001 by default and provides:
- `/health` - Health check endpoint
- `/api/auth/github/token` - Secure token exchange

**Production Deployment:**

For production, deploy both:
1. **Frontend**: Deploy to static hosting (Vercel, Netlify, etc.)
2. **Backend Proxy**: Deploy to Node.js hosting (Heroku, Railway, etc.)
3. Update `VITE_OAUTH_PROXY_URL` to point to your backend URL

**Alternative: GitHub App**

For even better security, consider creating a GitHub App:
- Fine-grained permissions
- Installation tokens instead of user tokens
- Better audit logging
- Webhook support

## Authentication Flow

### GitHub OAuth Flow

1. **User clicks "Sign in with GitHub"**
   - App generates PKCE code verifier and challenge
   - Stores verifier in sessionStorage
   - Redirects to GitHub authorization page

2. **User authorizes the app on GitHub**
   - User reviews requested permissions
   - Grants or denies access

3. **GitHub redirects back to app**
   - Includes authorization code and state in URL
   - App validates state to prevent CSRF attacks

4. **App exchanges code for access token**
   - Sends code + verifier to GitHub token endpoint
   - Receives access token

5. **App fetches user profile**
   - Uses access token to call GitHub API
   - Retrieves user information
   - Stores token and user data in localStorage

6. **User is authenticated**
   - App redirects to main interface
   - User session persists across page reloads

## Features

Once authenticated, users can:
- View their profile information (avatar, email, login)
- Create and manage LLM prompts with versioning
- Organize prompts into projects with categories and tags
- Share prompts with team members
- Export prompts and configurations
- Configure MCP (Model Context Protocol) servers

## User Data Storage

**Spark Mode:**
- Data stored in Spark KV storage system
- Automatically scoped to authenticated user

**Standalone Mode:**
- Data stored in SQLite database
- User session stored in localStorage
- Data persists across sessions

## Troubleshooting

### "GitHub Client ID not configured" error
- Ensure you've created a `.env` file with `VITE_GITHUB_CLIENT_ID`
- Restart the dev server after creating/updating `.env`

### "Invalid state parameter" error
- This is a security check to prevent CSRF attacks
- Clear your browser cache and try again
- Ensure you're not blocking third-party cookies

### "Failed to exchange authorization code" error
- Check that your GitHub OAuth app redirect URI matches exactly
- Verify your client secret is correct
- Check browser console for detailed error messages

### Token appears expired
- The app automatically validates tokens on load
- If validation fails, you'll be redirected to login
- Simply sign in again to get a fresh token

## Development Tips

1. **Testing OAuth Flow Locally:**
   - Use `http://localhost:5173` as your base URL
   - GitHub OAuth works fine with localhost
   - No need for HTTPS in development

2. **Multiple Environments:**
   - Create separate OAuth apps for dev/staging/production
   - Use environment-specific `.env` files
   - Keep credentials secure and never commit them

3. **Debugging:**
   - Check browser DevTools Console for auth errors
   - Network tab shows OAuth requests/responses
   - localStorage inspection shows stored tokens

## API Reference

### GitHub Auth Service (`src/lib/github-auth.ts`)

- `initiateGitHubLogin()` - Start OAuth flow
- `handleGitHubCallback(code, state)` - Handle OAuth callback
- `getCurrentUser()` - Get logged in user
- `getAccessToken()` - Get current access token
- `isAuthenticated()` - Check if user is authenticated
- `logout()` - Clear session and redirect to home
- `validateAndRefreshUser()` - Validate token and refresh user data

## Additional Resources

- [GitHub OAuth Documentation](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [OAuth 2.0 PKCE Flow](https://oauth.net/2/pkce/)
- [GitHub API Documentation](https://docs.github.com/en/rest)

