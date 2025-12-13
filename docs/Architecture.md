# promptArq - Architecture

## Overview

promptArq is a full-stack web application for AI prompt management, built with React, TypeScript, and Vite. It supports deployment both as a GitHub Spark application and as a standalone web app with local storage.

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Browser / Client                         │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │           React Application (TypeScript)                  │ │
│  │                                                           │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │ │
│  │  │ Components   │  │ State Mgmt   │  │ Routing      │   │ │
│  │  │ - PromptList │  │ - useStorage │  │ - Share URLs │   │ │
│  │  │ - Editor     │  │ - useState   │  │ - Auth       │   │ │
│  │  │ - Dialogs    │  │ - Context    │  │ - Invites    │   │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │ │
│  │                                                           │ │
│  │  ┌────────────────────────────────────────────────────┐  │ │
│  │  │          Storage Adapter (Dual Mode)              │  │ │
│  │  │                                                    │  │ │
│  │  │  ┌──────────────────┐    ┌──────────────────┐    │  │ │
│  │  │  │  Spark KV Store  │ OR │  localStorage    │    │  │ │
│  │  │  │  (Production)    │    │  (Development)   │    │  │ │
│  │  │  └──────────────────┘    └──────────────────┘    │  │ │
│  │  └────────────────────────────────────────────────────┘  │ │
│  │                                                           │ │
│  │  ┌────────────────────────────────────────────────────┐  │ │
│  │  │          UI Layer (Radix + Tailwind)              │  │ │
│  │  │  - Dialogs, Dropdowns, Inputs                     │  │ │
│  │  │  - Animations (Framer Motion)                     │  │ │
│  │  │  - Toast Notifications (Sonner)                   │  │ │
│  │  └────────────────────────────────────────────────────┘  │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │          Authentication (GitHub OAuth)                    │ │
│  │  - AuthGuard component                                    │ │
│  │  - Auth callback handler                                  │ │
│  │  - Token management                                       │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP/HTTPS
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OAuth Proxy Server (Express)                 │
│  - Token exchange with GitHub                                   │
│  - Secure credential handling                                   │
│  - CORS configuration                                           │
│  Port: 3001                                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ GitHub API
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    External Services                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐ │
│  │ GitHub OAuth │  │ GitHub API   │  │ MCP Clients (Claude) │ │
│  │ - Auth flow  │  │ - User info  │  │ - Prompt access      │ │
│  └──────────────┘  └──────────────┘  └──────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Core Components

### Frontend Application

#### App.tsx
The main application component managing global state and routing.

**Responsibilities:**
- Global state management for all entities (prompts, projects, categories, tags, teams, etc.)
- Navigation and view management
- Authentication state
- URL parameter handling (share tokens, team invites)
- Event coordination between components

**Key State:**
- `prompts` - All prompt documents
- `projects` - Project containers
- `categories` - Category organization within projects
- `tags` - Tag system for cross-project organization
- `systemPrompts` - Reusable prompt components
- `modelConfigs` - AI model configurations
- `versions` - Prompt version history
- `teams` - Team definitions
- `teamMembers` - Team membership
- `comments` - Prompt comments
- `sharedPrompts` - Shared prompt metadata
- `users` - User directory

#### Storage Adapter (`src/lib/storage-adapter.ts`)
Dual-mode persistence layer that automatically detects environment.

**Detection Logic:**
```typescript
isSparkEnvironment()
├─> Check hostname (localhost = NOT Spark)
├─> Check github.app domain
├─> Check Spark KV API availability
└─> Check GITHUB_RUNTIME_PERMANENT_NAME
```

**Adapters:**
1. **SparkKVAdapter** - Production deployment on GitHub Spark
   - Uses `window.spark.kv` API
   - Native KV store persistence
   - Automatic sync

2. **LocalStorageAdapter** - Development/standalone deployment
   - Uses browser localStorage
   - Prefixed keys (`promptarq_`)
   - JSON serialization

#### Component Architecture

**Layout Components:**
- `App.tsx` - Main layout with sidebar and content area
- `BackgroundDecorations.tsx` - Visual effects
- `FloatingShapes.tsx` - Animated background elements
- `EnvironmentBadge.tsx` - Shows Spark/local environment indicator

**Feature Components:**
- `PromptList.tsx` - Grid/list view of prompts with search and filters
- `PromptEditor.tsx` - Rich text editor for prompt content
- `ProjectDialog.tsx` - Project management modal
- `SystemPromptDialog.tsx` - System prompt library
- `ModelConfigDialog.tsx` - AI model configuration
- `MCPServerDialog.tsx` - MCP integration settings
- `TeamDialog.tsx` - Team management
- `TemplateDialog.tsx` - Prompt templates
- `ShareDialog.tsx` - Prompt sharing
- `VersionDiff.tsx` - Version comparison view

**Authentication Components:**
- `AuthGuard.tsx` - Route protection
- `AuthCallback.tsx` - OAuth callback handler
- `LoginPage.tsx` - Authentication UI
- `UserProfile.tsx` - User settings and profile

**UI Components:**
- `ui/` - Radix UI primitives (buttons, dialogs, inputs, etc.)
- Fully styled with Tailwind CSS
- Consistent design system

### Backend Server

#### OAuth Proxy (`server.js`)
Express server handling GitHub OAuth token exchange.

**Endpoints:**
- `GET /health` - Health check
- `POST /api/auth/github/token` - Token exchange

**Security:**
- Client secret kept server-side
- CORS restricted to development origins
- Environment variable configuration

**Environment Variables:**
- `VITE_GITHUB_CLIENT_ID` - GitHub OAuth app ID
- `VITE_GITHUB_CLIENT_SECRET` - GitHub OAuth secret
- `VITE_GITHUB_REDIRECT_URI` - OAuth callback URL
- `PROXY_PORT` - Server port (default: 3001)

## Data Model

### Core Entities

#### Prompt
```typescript
{
  id: string
  title: string
  description: string
  content: string              // Main prompt text
  projectId: string            // Parent project
  categoryId: string           // Category within project
  tags: string[]               // Cross-project tags
  createdBy: string            // User ID
  createdAt: number
  updatedAt: number
  isArchived: boolean
  exposedToMCP: boolean        // Available via MCP server
}
```

#### PromptVersion
```typescript
{
  id: string
  promptId: string
  versionNumber: number
  content: string
  changeNote: string
  createdBy: string
  createdAt: number
  improvedFrom?: string        // Version that was improved
}
```

#### Project
```typescript
{
  id: string
  name: string
  description: string
  color: string                // Visual identifier
}
```

#### Category
```typescript
{
  id: string
  projectId: string
  name: string
  description: string
}
```

#### Tag
```typescript
{
  id: string
  name: string
  color: string
}
```

#### Team
```typescript
{
  id: string
  name: string
  description: string
  ownerId: string
  createdAt: number
  inviteToken: string          // For team invites
}
```

#### SystemPrompt
```typescript
{
  id: string
  name: string
  content: string
  description: string
  category: string
}
```

#### ModelConfig
```typescript
{
  id: string
  name: string
  provider: string             // OpenAI, Anthropic, etc.
  model: string                // gpt-4, claude-3, etc.
  temperature: number
  maxTokens: number
  topP: number
  frequencyPenalty: number
  presencePenalty: number
}
```

## Feature Architecture

### Prompt Management
**Flow:**
```
Create Prompt
├─> Generate UUID
├─> Set metadata (title, description, project, category, tags)
├─> Save initial version
├─> Persist to storage
└─> Update UI

Edit Prompt
├─> Modify content
├─> Create new version (if significant change)
├─> Add change note
├─> Update updatedAt timestamp
├─> Persist to storage
└─> Update UI

Archive/Delete
├─> Set isArchived flag
├─> Keep in storage (soft delete)
└─> Remove from active view
```

### Version Control
**Versioning Strategy:**
- Automatic version creation on significant changes
- Manual version with change notes
- Linked list of versions (improvedFrom)
- Diff view for comparing versions
- Full version history retained

**Version Navigation:**
- View all versions
- Compare any two versions
- Restore previous version
- Fork from older version

### Team Collaboration
**Team Features:**
- Create teams
- Invite members via token
- Team-scoped prompts (future)
- Comments on prompts
- Shared prompt library

**Invite Flow:**
```
Create Team
├─> Generate team ID
├─> Generate invite token
├─> Set owner
└─> Persist

Share Invite
├─> Generate invite URL
├─> Copy to clipboard
└─> Send to invitees

Accept Invite
├─> Parse token from URL
├─> Verify team exists
├─> Add user as member
├─> Remove token from URL
└─> Show success toast
```

### MCP Integration
**Model Context Protocol Server:**
- Exposes selected prompts to MCP clients (Claude Desktop, etc.)
- Configuration via dialog
- Enable/disable per prompt
- Copy configuration for MCP client

**MCP Server Endpoints:**
- List exposed prompts
- Get prompt content
- Search prompts
- Get prompt versions

### Authentication Flow
**GitHub OAuth:**
```
Login
├─> Redirect to GitHub OAuth
├─> User authorizes
├─> Callback with code
├─> Exchange code for token (via proxy)
├─> Fetch user info
├─> Store auth state
└─> Redirect to app

Authenticated State
├─> Store user in localStorage
├─> Display user avatar/name
├─> Enable team features
└─> Track user actions
```

### Search & Filtering
**Search Implementation:**
- Real-time filtering
- Searches: title, description, content, tags
- Case-insensitive
- Debounced input

**Filters:**
- Project filter (dropdown)
- Tag filter (multi-select)
- Show archived toggle
- Team filter (when team selected)

### Template System
**Templates:**
- Pre-defined prompt structures
- Categories (code review, documentation, etc.)
- Placeholder system ({{variable}})
- Quick prompt creation

**Template Flow:**
```
Select Template
├─> Choose from library
├─> Fill placeholders
├─> Generate prompt
├─> Save as new prompt
└─> Continue editing
```

## Technology Stack

### Frontend
- **React 19** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **Tailwind CSS 4** - Styling
- **Radix UI** - Accessible components
- **Framer Motion** - Animations
- **Sonner** - Toast notifications
- **Phosphor Icons** - Icon library
- **@github/spark** - Spark integration
- **React Hook Form** - Form management
- **Zod** - Schema validation

### Backend
- **Express** - Web server
- **CORS** - Cross-origin handling
- **dotenv** - Environment configuration

### Development Tools
- **ESLint** - Linting
- **TypeScript Compiler** - Type checking
- **Vite Plugins** - React, SWC
- **Concurrently** - Run multiple servers

## Deployment Architecture

### GitHub Spark Deployment
```
GitHub Spark Platform
├─> Serves React app
├─> Provides KV store
├─> Handles routing
└─> Manages runtime
```

**Features:**
- Zero-config deployment
- Automatic HTTPS
- Global CDN
- KV store persistence
- Environment variables

### Standalone Deployment
```
Web Server (any)
├─> Serve static build
├─> Configure OAuth proxy
├─> Set environment variables
└─> Use localStorage
```

**Requirements:**
- Static file hosting
- OAuth proxy server
- Environment variables
- HTTPS (for OAuth)

## Security Architecture

### Authentication
- OAuth 2.0 with GitHub
- Proxy server for token exchange
- No client secrets in frontend
- Secure token storage

### Data Privacy
- User data isolated per user
- No server-side storage of prompt content
- Local/Spark KV only
- Team invites via cryptographic tokens

### API Security
- CORS restricted
- OAuth validation
- Rate limiting (future)
- Input sanitization

## Performance Considerations

### Optimization Strategies
- React component memoization
- Lazy loading of dialogs
- Virtual scrolling for large lists
- Debounced search
- Optimistic UI updates

### Storage Performance
- JSON serialization overhead
- localStorage 5-10MB limit
- Spark KV unlimited storage
- Indexed access patterns

### Bundle Size
- Code splitting
- Tree shaking
- Component lazy loading
- Dynamic imports

## Extensibility Points

### Adding New Features
1. **New Entity Type** - Add to types.ts, storage keys, state management
2. **New Component** - Create in components/, integrate in App.tsx
3. **New Storage Adapter** - Implement StorageAdapter interface
4. **New Template** - Add to default-templates.ts
5. **New MCP Endpoint** - Extend MCP server configuration

### Customization
- Theme variables (CSS custom properties)
- Icon library swappable
- Storage adapter pluggable
- Component library replaceable

## Future Architecture Considerations

### Planned Enhancements
- Real-time collaboration (WebSockets)
- Conflict resolution
- Offline support (Service Worker)
- Export/import formats
- API integration (OpenAI, Anthropic)
- Prompt execution within app
- Analytics dashboard
- Plugin system

### Scalability
- Move to database for large datasets
- Server-side rendering
- CDN optimization
- Caching strategies
- Search indexing
