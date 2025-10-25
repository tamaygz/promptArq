# Authentication Setup Guide

This application supports user authentication via the Spark runtime, which provides built-in GitHub authentication. The app also includes support for Microsoft OAuth as an alternative authentication method.

## Spark Runtime Authentication (GitHub)

The Spark runtime provides built-in GitHub authentication through the `spark.user()` API. No additional configuration is required for GitHub authentication as it's handled automatically by the runtime.

When a user accesses the application:
1. The Spark runtime checks for an authenticated session
2. If authenticated, `spark.user()` returns the user's GitHub profile data
3. If not authenticated, the user is prompted to sign in with GitHub

The returned user object includes:
- `id`: Unique user identifier
- `login`: GitHub username
- `email`: User's email address
- `avatarUrl`: Profile picture URL
- `isOwner`: Whether the user is the app owner

## Microsoft OAuth Setup (Optional)

For organizations that prefer Microsoft authentication, you can configure Microsoft OAuth:

1. Go to [Azure Portal - App Registrations](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
2. Click "New registration"
3. Fill in the application details:
   - **Name**: arqioly (or your preferred name)
   - **Supported account types**: Choose based on your needs
     - "Accounts in any organizational directory and personal Microsoft accounts" for broad access
   - **Redirect URI**: Select "Web" and enter `https://yourdomain.com/auth/microsoft/callback`
4. Click "Register"
5. Copy the **Application (client) ID**
6. Copy the **Directory (tenant) ID**
7. Go to "Certificates & secrets" and create a new client secret
8. Set environment variables:
   ```
   VITE_MICROSOFT_CLIENT_ID=your_client_id_here
   VITE_MICROSOFT_TENANT_ID=your_tenant_id_here
   ```

## Environment Variables

Create a `.env` file in your project root (only needed for Microsoft OAuth):

```env
# Microsoft OAuth (Optional)
VITE_MICROSOFT_CLIENT_ID=your_microsoft_client_id
VITE_MICROSOFT_TENANT_ID=common
```

## Local Development

For local development with Microsoft OAuth, use this callback URL:
- Microsoft: `http://localhost:5173/auth/microsoft/callback`

GitHub authentication works automatically in both local development and production.

## Features

Once authenticated, users can:
- View their profile information (avatar, email, login)
- See their authentication provider (GitHub or Microsoft)
- Access account creation and last login timestamps
- Sign out from the application

## User Data Storage

User information is stored in the Spark KV storage system:
- First login creates a user record with creation timestamp
- Subsequent logins update the last login timestamp
- User preferences and settings are persisted across sessions
