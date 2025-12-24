# Developer Guide

## Development Setup

### Prerequisites
- Node.js 16+ and npm
- Git
- Modern browser (Chrome, Firefox, Edge)
- Code editor (VS Code recommended)

### Initial Setup

```bash
# Clone repository
git clone https://github.com/tamaygz/promptArq.git
cd promptArq

# Install dependencies
npm install

# Set up environment variables
cp .env.example .env
# Edit .env with your values

# Start development server
npm run dev
```

This starts:
- **Vite dev server** on port 5173 (or 5000)
- **OAuth proxy server** on port 3001

### Environment Variables

Create `.env` file:

```env
# GitHub OAuth (for authentication features)
VITE_GITHUB_CLIENT_ID=your_client_id
VITE_GITHUB_CLIENT_SECRET=your_client_secret
VITE_GITHUB_REDIRECT_URI=http://localhost:5173/auth/callback

# OAuth Proxy Port
PROXY_PORT=3001
```

**GitHub OAuth App Setup:**
1. Go to GitHub Settings → Developer settings → OAuth Apps
2. Create new OAuth app
3. Homepage URL: `http://localhost:5173`
4. Callback URL: `http://localhost:5173/auth/callback`
5. Copy Client ID and generate Client Secret
6. Add to `.env`

## Project Structure

```
promptArq/
├── src/
│   ├── components/          # React components
│   │   ├── ui/              # Radix UI components
│   │   ├── PromptEditor.tsx
│   │   ├── PromptList.tsx
│   │   └── [other features]
│   ├── lib/                 # Utilities and core logic
│   │   ├── types.ts         # TypeScript types
│   │   ├── storage-adapter.ts # Persistence layer
│   │   ├── default-*.ts     # Default data
│   │   └── utils.ts         # Helper functions
│   ├── hooks/               # Custom React hooks
│   ├── assets/              # Images, icons
│   ├── styles/              # CSS files
│   ├── App.tsx              # Main application
│   └── main.tsx             # Entry point
├── server.js                # OAuth proxy server
├── vite.config.ts           # Vite configuration
├── tailwind.config.js       # Tailwind configuration
├── package.json             # Dependencies and scripts
└── docs/                    # Documentation

```

## Technology Stack

### Core
- **React 19** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool
- **Tailwind CSS 4** - Styling

### UI Components
- **Radix UI** - Accessible primitives
- **Framer Motion** - Animations
- **Phosphor Icons** - Icon library
- **Sonner** - Toast notifications

### State Management
- React useState
- Custom useStorage hook
- Local/Spark KV storage

### Backend
- **Express** - OAuth proxy
- **CORS** - Cross-origin handling

## Key Concepts

### Storage Adapter Pattern

The app uses a dual-mode storage system:

```typescript
// src/lib/storage-adapter.ts
export interface StorageAdapter {
  keys(): Promise<string[]>;
  get<T>(key: string): Promise<T | undefined>;
  set<T>(key: string, value: T): Promise<void>;
  delete(key: string): Promise<void>;
}
```

**Adapters:**
- `SparkKVAdapter` - GitHub Spark deployment
- `LocalStorageAdapter` - Local development

**Auto-detection:**
```typescript
isSparkEnvironment()
├─> localhost → false (use localStorage)
├─> github.app domain → true (use Spark KV)
└─> Default → false (safe fallback)
```

### useStorage Hook

Custom hook for persistent state:

```typescript
const [prompts, setPrompts] = useStorage<Prompt[]>('prompts', []);
```

**Features:**
- Automatic persistence
- Type-safe
- Initial value support
- Works with both storage adapters

### Component Architecture

**Layout Components:**
- App.tsx - Main layout
- Sidebar with project/tag navigation
- Content area with editor

**Feature Components:**
- Modular, self-contained
- Props-based communication
- Event callbacks for actions

**UI Components:**
- Radix primitives wrapped with Tailwind
- Consistent styling
- Accessible by default

## Development Workflow

### Running the App

```bash
# Development mode (hot reload)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint code
npm run lint
```

### Scripts Explained

**package.json scripts:**
- `dev` - Runs client + server concurrently
- `client` - Vite dev server only
- `server` - OAuth proxy only
- `build` - TypeScript compile + Vite build
- `kill` - Kill process on port 5000 (Windows)

### Adding New Features

#### New Entity Type

1. **Add TypeScript type** (`src/lib/types.ts`)
```typescript
export type MyEntity = {
  id: string
  name: string
  // ...
}
```

2. **Add to App state**
```typescript
const [myEntities, setMyEntities] = useStorage<MyEntity[]>('my-entities', []);
```

3. **Create component** (`src/components/MyEntityDialog.tsx`)

4. **Wire up in App.tsx**

#### New Component

1. Create component file
2. Import in App.tsx
3. Add to UI
4. Connect state/callbacks

#### New Storage Key

Just use with useStorage:
```typescript
const [data, setData] = useStorage<T>('new-key', defaultValue);
```

Automatically works with both storage adapters.

### Styling

**Tailwind CSS:**
- Utility-first approach
- Configure in `tailwind.config.js`
- Custom theme in `src/styles/theme.css`

**Component Styling:**
```tsx
<div className="flex items-center gap-2 p-4 rounded-lg bg-secondary">
  {/* content */}
</div>
```

**Dark Mode:**
App uses dark theme by default. Light mode support via `next-themes` if needed.

### State Management

**Local State:**
```typescript
const [value, setValue] = useState(initialValue);
```

**Persistent State:**
```typescript
const [value, setValue] = useStorage('key', initialValue);
```

**Prop Drilling:**
Pass callbacks down for child components to trigger parent actions.

**Future:** Consider Zustand or Redux if state gets complex.

## Testing

Currently no automated tests. Contributions welcome!

**Manual Testing:**
- Test all features in both Spark and local mode
- Test with/without authentication
- Test mobile responsive design
- Test keyboard shortcuts
- Test error handling

## Building for Production

### GitHub Spark Deployment

1. Push to GitHub
2. Spark automatically deploys
3. KV store automatically available
4. HTTPS automatically configured

### Standalone Deployment

```bash
# Build production bundle
npm run build

# Output in dist/
dist/
├── index.html
├── assets/
│   ├── index-[hash].js
│   └── index-[hash].css
└── [other assets]
```

**Deploy `dist/` folder to:**
- Vercel
- Netlify
- AWS S3 + CloudFront
- Any static hosting

**Requirements:**
- HTTPS (for OAuth)
- OAuth proxy server running
- Environment variables configured

### OAuth Proxy Deployment

Deploy `server.js` separately:

```bash
# Example: Deploy to Heroku
git push heroku main

# Or run on VPS
node server.js
```

Update `.env` with production OAuth URLs.

## Debugging

### Browser DevTools

**Console:**
- `window.spark` - Check Spark availability
- Storage adapter logs

**Network:**
- API requests to OAuth proxy
- GitHub OAuth flow
- Storage operations

**React DevTools:**
- Component hierarchy
- Props/state inspection
- Performance profiling

### Common Issues

**OAuth not working:**
- Check client ID/secret
- Verify redirect URI matches exactly
- Check CORS settings on proxy
- Try incognito mode

**Storage not persisting:**
- Check localStorage quota
- Verify Spark KV in production
- Check storage adapter detection

**Build errors:**
- Clear node_modules: `npm run clean && npm install`
- Check TypeScript errors: `npm run build`
- Update dependencies

## Contributing

### Guidelines

- Follow existing code style
- Write meaningful commit messages
- Test thoroughly before PR
- Update documentation
- Add types for new code

### Code Style

- Use TypeScript
- 2-space indentation
- Semicolons required
- Single quotes for strings
- Trailing commas in multiline

### Pull Request Process

1. Fork repository
2. Create feature branch: `git checkout -b feature/my-feature`
3. Make changes
4. Test thoroughly
5. Commit: `git commit -m "Add my feature"`
6. Push: `git push origin feature/my-feature`
7. Create Pull Request
8. Address review comments

## Architecture Deep Dive

See [Architecture.md](./Architecture.md) for detailed technical architecture including:
- Component structure
- Data flow
- Storage system
- Authentication flow
- MCP integration

## Additional Resources

- [User Guide](./UserGuide.md) - End-user documentation
- [MCP Guide](./MCP.md) - MCP integration setup
- [Windows App](../WindowsApp/README.md) - Desktop application
- [GitHub Issues](https://github.com/tamaygz/promptArq/issues) - Bug reports & features
