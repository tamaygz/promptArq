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

The application will now:
1. Check if running in Spark environment
2. If not, use GitHub OAuth for authentication
3. Redirect unauthenticated users to the login page
4. Handle OAuth callback and store user session

## Security Considerations

### ⚠️ Important: Client Secret in Frontend

**Development Mode:**
The current implementation includes the client secret in the frontend for ease of local development. This is acceptable for local development but **NOT recommended for production**.

**Production Recommendations:**

For production deployments, implement a secure backend OAuth flow:

1. **Backend Proxy Approach** (Recommended):
   - Create a backend API endpoint to handle token exchange
   - Keep client secret on the server
   - Frontend sends authorization code to your backend
   - Backend exchanges code for token securely
   - Backend returns access token to frontend

2. **GitHub App Instead of OAuth App**:
   - Consider creating a GitHub App which provides better security
   - GitHub Apps support fine-grained permissions
   - Can use installation tokens instead of user tokens

3. **Example Backend Proxy (Node.js/Express)**:
   ```javascript
   app.post('/auth/github/token', async (req, res) => {
     const { code, code_verifier } = req.body;
     
     const response = await fetch('https://github.com/login/oauth/access_token', {
       method: 'POST',
       headers: { 'Content-Type': 'application/json' },
       body: JSON.stringify({
         client_id: process.env.GITHUB_CLIENT_ID,
         client_secret: process.env.GITHUB_CLIENT_SECRET,
         code: code,
         code_verifier: code_verifier
       })
     });
     
     const data = await response.json();
     res.json({ access_token: data.access_token });
   });
   ```

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

