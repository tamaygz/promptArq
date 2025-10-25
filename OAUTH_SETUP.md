# Authentication Setup Guide

This application uses the Spark runtime's built-in authentication system. No additional OAuth configuration or client IDs are required.

## Spark Runtime Authentication

The Spark runtime provides built-in authentication through the `spark.user()` API. Authentication is handled automatically by the Spark platform.

When a user accesses the application:
1. The Spark runtime checks for an authenticated session
2. If authenticated, `spark.user()` returns the user's profile data
3. The application loads with the user's session automatically

The returned user object includes:
- `id`: Unique user identifier
- `login`: Username
- `email`: User's email address
- `avatarUrl`: Profile picture URL
- `isOwner`: Whether the user is the app owner

## No Configuration Required

This application requires **zero configuration** for authentication. The Spark runtime handles all authentication flows, session management, and user data retrieval.

## Features

Once authenticated, users can:
- View their profile information (avatar, email, login)
- Create and manage LLM prompts with versioning
- Organize prompts into projects with categories and tags
- Share prompts with team members
- Export prompts and configurations
- Configure MCP (Model Context Protocol) servers

## User Data Storage

User data and preferences are stored in the Spark KV storage system:
- All prompts, projects, and settings are persisted per user
- Data is automatically scoped to the authenticated user
- No manual database setup required
