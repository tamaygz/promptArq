# OAuth Proxy Server Fix

## Problem
GitHub doesn't allow direct token exchange from browsers due to CORS policy. This is by design for security.

## Solution
Created a backend proxy server (`server.js`) that:
1. Runs on http://localhost:3001
2. Handles the OAuth token exchange securely
3. Keeps client secret on the server (never exposed to browser)
4. Returns only the access token to the frontend

## What Changed

### New Files
- `server.js` - Express server that proxies OAuth requests

### Updated Files
- `package.json` - Added dependencies and scripts
- `github-auth.ts` - Now uses proxy server instead of direct GitHub API calls
- `.env.example` - Added proxy configuration options
- Documentation files - Updated with proxy server information

### New Dependencies
- `express` - Web server framework
- `cors` - CORS middleware
- `dotenv` - Environment variable loader
- `concurrently` - Run multiple commands simultaneously

## How to Use

### Development
```bash
# Install dependencies
npm install

# Run both servers (proxy + vite)
npm run dev
```

### Individual Commands
```bash
# Run proxy server only
npm run server

# Run vite only
npm run client
```

## Configuration

Add to your `.env` file (optional):
```env
# Proxy server port (default: 3001)
PROXY_PORT=3001

# Proxy URL from frontend (default: http://localhost:3001)
VITE_OAUTH_PROXY_URL=http://localhost:3001
```

## Production Deployment

1. **Deploy Frontend**: Use any static hosting (Vercel, Netlify, etc.)
2. **Deploy Backend**: Deploy `server.js` to Node.js hosting (Heroku, Railway, etc.)
3. **Update Environment**: Set `VITE_OAUTH_PROXY_URL` to your backend URL

Example:
```env
VITE_OAUTH_PROXY_URL=https://your-backend.herokuapp.com
```

## Security

✅ **Client secret is now secure** - Never sent to browser  
✅ **CORS properly configured** - Proxy handles cross-origin requests  
✅ **Production ready** - Same code works for dev and prod  

## Testing

1. Make sure `.env` file has your GitHub credentials
2. Run `npm run dev`
3. Visit http://localhost:5173
4. Click "Sign in with GitHub"
5. Should work without CORS errors!

## Troubleshooting

**Proxy server won't start:**
- Check if port 3001 is already in use
- Change `PROXY_PORT` in `.env`

**Still getting CORS errors:**
- Make sure proxy server is running
- Check browser console for proxy URL
- Verify `VITE_OAUTH_PROXY_URL` is correct

**"Failed to exchange code":**
- Verify GitHub OAuth app settings
- Check client ID and secret in `.env`
- Look at proxy server logs for details
