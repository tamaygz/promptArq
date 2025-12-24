/**
 * Simple OAuth Proxy Server
 * Handles GitHub OAuth token exchange securely
 */

import express from 'express';
import cors from 'cors';
import 'dotenv/config';

const app = express();
const PORT = process.env.PROXY_PORT || 3001;

// Enable CORS for Vite dev server
app.use(cors({
  origin: ['http://localhost:5173', 'http://localhost:5000'],
  credentials: true
}));

app.use(express.json());

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ status: 'ok', service: 'promptArq OAuth Proxy' });
});

// GitHub OAuth token exchange endpoint
app.post('/api/auth/github/token', async (req, res) => {
  try {
    const { code, code_verifier } = req.body;

    if (!code) {
      return res.status(400).json({ error: 'Authorization code is required' });
    }

    const clientId = process.env.VITE_GITHUB_CLIENT_ID;
    const clientSecret = process.env.VITE_GITHUB_CLIENT_SECRET;
    const redirectUri = process.env.VITE_GITHUB_REDIRECT_URI || 'http://localhost:5173/auth/callback';

    if (!clientId || !clientSecret) {
      console.error('Missing GitHub OAuth credentials');
      return res.status(500).json({ error: 'Server configuration error' });
    }

    // Exchange code for access token with GitHub
    const tokenResponse = await fetch('https://github.com/login/oauth/access_token', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
      },
      body: JSON.stringify({
        client_id: clientId,
        client_secret: clientSecret,
        code: code,
        redirect_uri: redirectUri,
        code_verifier: code_verifier
      })
    });

    if (!tokenResponse.ok) {
      const errorText = await tokenResponse.text();
      console.error('GitHub token exchange failed:', errorText);
      return res.status(tokenResponse.status).json({ 
        error: 'Token exchange failed',
        details: errorText
      });
    }

    const data = await tokenResponse.json();

    if (data.error) {
      console.error('GitHub OAuth error:', data);
      return res.status(400).json({ 
        error: data.error,
        description: data.error_description
      });
    }

    if (!data.access_token) {
      console.error('No access token in response:', data);
      return res.status(500).json({ error: 'No access token received' });
    }

    // Return only the access token (don't expose other data)
    res.json({ 
      access_token: data.access_token,
      token_type: data.token_type || 'bearer',
      scope: data.scope
    });

  } catch (error) {
    console.error('OAuth proxy error:', error);
    res.status(500).json({ 
      error: 'Internal server error',
      message: error.message
    });
  }
});

app.listen(PORT, () => {
  console.log(`🔐 OAuth Proxy Server running on http://localhost:${PORT}`);
  console.log(`📝 Ready to handle GitHub OAuth token exchanges`);
  console.log(`🌐 Accepting requests from Vite dev server`);
});
