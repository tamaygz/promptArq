# arqioly – Prompt Management Web App (MVP)

A streamlined prompt management system for teams to create, organize, version, and improve LLM prompts collaboratively.

**Experience Qualities**:
1. **Professional** - Clean, confident interface that conveys expertise and reliability
2. **Efficient** - Minimal friction from idea to improved prompt with clear workflows
3. **Collaborative** - Transparent team interactions with versioning and commenting

**Complexity Level**: Light Application (multiple features with basic state)
- Core features include prompt management, versioning, tagging, and AI-powered improvements without requiring backend infrastructure

## Essential Features

### Prompt Library ✓
- **Functionality**: Central repository showing all prompts with metadata
- **Purpose**: Quick access and overview of team's prompt inventory
- **Trigger**: Landing page after app load
- **Progression**: View grid → Filter by tag/project → Click prompt → Open editor
- **Success criteria**: All prompts visible, filterable, and accessible within 2 clicks
- **Status**: IMPLEMENTED with search, project filters, tag filters, and archive toggle

### Prompt Editor ✓
- **Functionality**: Create and edit prompts with title, content, tags, project, category
- **Purpose**: Primary workspace for prompt authoring
- **Trigger**: Click "New Prompt" or select existing prompt
- **Progression**: Enter details → Save version → Add change note → Persist to storage
- **Success criteria**: Changes saved immediately, version history preserved
- **Status**: IMPLEMENTED with keyboard shortcuts (⌘S/Ctrl+S to save, ⌘I/Ctrl+I to improve)

### Version History ✓
- **Functionality**: View all versions with timestamps, authors, and change notes
- **Purpose**: Track evolution and enable rollback
- **Trigger**: Click "History" in prompt editor
- **Progression**: View list → Select version → Preview content → Restore if desired
- **Success criteria**: Every save creates immutable version, easy comparison
- **Status**: IMPLEMENTED with restore and compare functionality

### AI Prompt Improvement ✓
- **Functionality**: Analyze prompt and suggest improvements using LLM with system prompt resolution
- **Purpose**: Automate prompt optimization with AI assistance
- **Trigger**: Click "Improve Prompt" button
- **Progression**: Click button → Resolve system prompt → LLM analyzes → View suggestion → Accept or reject → New version created if accepted
- **Success criteria**: Suggestions appear within 5s, acceptance creates new version with metadata
- **Status**: IMPLEMENTED with system prompt resolution algorithm (Prompt > Project > Category > Tag > Team Default)

### Organization System ✓
- **Functionality**: Projects, categories, and tags for taxonomy
- **Purpose**: Structure growing prompt library
- **Trigger**: Assign during prompt creation/editing
- **Progression**: Create project/category/tag → Assign to prompt → Filter library by taxonomy
- **Success criteria**: Multi-dimensional filtering, consistent taxonomy across team
- **Status**: IMPLEMENTED with color-coded projects and tags

### Comments & Collaboration ✓
- **Functionality**: Thread discussions on prompts and versions
- **Purpose**: Async feedback and decision documentation
- **Trigger**: Click comment icon on prompt
- **Progression**: View existing → Add comment → Mention user → Receive notification
- **Success criteria**: Comments persist with versions, mentions work
- **Status**: IMPLEMENTED with user avatars and timestamps

### System Prompts ✓
- **Functionality**: Configure system prompts at different scopes (Team, Project, Category, Tag, Prompt)
- **Purpose**: Control AI improvement behavior with context-aware instructions
- **Trigger**: Click "System Prompts" button in header
- **Progression**: Create system prompt → Assign scope → Set priority → Use in improvement
- **Success criteria**: System prompts resolved by precedence during improvement
- **Status**: IMPLEMENTED with full precedence resolution algorithm

### Archive & Export ✓
- **Functionality**: Archive prompts and export data as JSON
- **Purpose**: Data portability and prompt lifecycle management
- **Trigger**: Click archive/export buttons
- **Progression**: Archive prompt → View archived → Restore if needed; Export single or all prompts
- **Success criteria**: Archived prompts hidden by default, exports include all metadata
- **Status**: IMPLEMENTED with individual and bulk export

### Version Comparison ✓
- **Functionality**: Side-by-side comparison of prompt versions
- **Purpose**: Understand changes between iterations
- **Trigger**: Click compare icon on version
- **Progression**: Select version → View side-by-side diff → Understand changes
- **Success criteria**: Clear visual distinction between versions
- **Status**: IMPLEMENTED with side-by-side view

### User Authentication ✓
- **Functionality**: GitHub and Microsoft OAuth login with user profiles
- **Purpose**: Secure access and personalized experience
- **Trigger**: App load without authenticated session
- **Progression**: Choose provider → OAuth flow → Return to app → Access user profile from header
- **Success criteria**: Seamless authentication, persistent sessions, profile shows provider info
- **Status**: IMPLEMENTED with AuthGuard, login page, and user profile dialog

### Model Provider Configuration ✓
- **Functionality**: Configure LLM providers and models with specific parameters (temperature, max tokens) scoped to different levels
- **Purpose**: Control which AI model is used for prompt improvement with fine-grained configuration
- **Trigger**: Click "Model Config" button in header
- **Progression**: Create config → Select provider (OpenAI/Anthropic/Azure) → Choose model → Set parameters → Assign scope (Team/Project/Category/Tag) → Use in improvement
- **Success criteria**: Model configs resolved by precedence during improvement, tooltip shows active config
- **Status**: IMPLEMENTED with full precedence resolution algorithm (Prompt → Project → Category → Tag → Team Default)

### MCP Server Exposure ✓
- **Functionality**: Expose individual prompts via Model Context Protocol (MCP) for AI agents to discover and execute
- **Purpose**: Allow AI assistants like Claude to access and use arqioly prompts through standardized protocol
- **Trigger**: Check "Expose via MCP Server" in prompt editor, or click "MCP Server" button in header
- **Progression**: Enable MCP on prompt → Save → View MCP Server dialog → Copy endpoint/config → Configure AI agent → Agent retrieves prompts grouped by project
- **Success criteria**: Prompts with MCP enabled are visible in MCP Server dialog, grouped by project with proper metadata
- **Status**: IMPLEMENTED with checkbox in prompt editor and MCP configuration dialog showing exposed prompts by project

### Team Management ✓
- **Functionality**: Create teams, invite members via shareable links, configure project access, manage roles, and filter prompts by team
- **Purpose**: Enable collaboration with role-based access control and workspace organization
- **Trigger**: Click "Teams" button in header or use team selector dropdown
- **Progression**: Create team → Configure project access → Assign member roles (Owner/Admin/Member) → Copy invite link → Share with team members → Members join via link → Switch team view → Filter prompts by team access
- **Success criteria**: Teams can be created, invite links work, project access is configurable, member roles can be changed, team filtering shows only accessible prompts, statistics reflect team context
- **Status**: IMPLEMENTED with team creation, invite token generation, project access configuration, member management, role assignment (Owner/Admin/Member), team view selector, and filtered prompt display

### Collapsible Sidebar ✓
- **Functionality**: Collapse/expand the left sidebar to maximize editor space
- **Purpose**: Give users control over their workspace layout
- **Trigger**: Click collapse/expand button on sidebar edge
- **Progression**: Click button → Sidebar animates closed/open → State persists between sessions
- **Success criteria**: Smooth animation, persistent state, intuitive button placement
- **Status**: IMPLEMENTED with animated transitions and persistent state in KV storage

### Team View Filtering ✓
- **Functionality**: Filter all prompts and projects based on selected team's access permissions
- **Purpose**: Provide team-scoped workspace view showing only accessible content
- **Trigger**: Select team from dropdown in header
- **Progression**: Select team → View updates to show only team-accessible projects → Prompt list filters to team context → Statistics reflect team data → Visual indicators show active team view
- **Success criteria**: Seamless team switching, accurate filtering, clear visual feedback, persistent team selection
- **Status**: IMPLEMENTED with team selector dropdown, filtered prompt display, team-scoped statistics, and visual indicators

## Edge Case Handling

- **No prompts state** ✓ - Welcoming empty state with statistics and "Create First Prompt" CTA
- **Improve fails** ✓ - Clear error message, option to retry, doesn't lose user's work
- **Long content** ✓ - Scrollable editor with fixed action bar
- **Duplicate names** ✓ - Allow duplicates with project/category context for disambiguation
- **Version limit** ✓ - No artificial limit; stores in browser KV storage
- **Offline mode** - Not implemented (future enhancement)

## Implementation Status

### Completed (MVP Ready)
- ✅ Prompt CRUD operations
- ✅ Version management and history
- ✅ Projects, categories, and tags organization
- ✅ System prompt configuration and resolution
- ✅ Model provider configuration and resolution
- ✅ AI-powered prompt improvement with configurable models
- ✅ Comments system
- ✅ Archive/restore functionality
- ✅ Search and filtering
- ✅ Export (single and bulk)
- ✅ Version comparison/diff view
- ✅ Keyboard shortcuts
- ✅ Animated transitions
- ✅ Dashboard with statistics
- ✅ Authentication (GitHub/Microsoft OAuth)
- ✅ User profiles with provider information
- ✅ Sharing prompts with read-only links
- ✅ MCP Server exposure for AI agents
- ✅ Team management with invite links
- ✅ Project access control per team
- ✅ Role assignment and management (Owner/Admin/Member)
- ✅ Team view filtering and switching
- ✅ Collapsible sidebar

### Future Enhancements (V2+)
- ⏳ Granular permissions within teams (project-level and category-level access control)
- ⏳ Real-time collaborative editing with presence
- ⏳ Advanced run tracking with tokens, latency, and cost
- ⏳ Test cases and evaluations
- ⏳ Public sharing and templates
- ⏳ MCP Server implementation (actual backend server)
- ⏳ Audit logging
- ⏳ Cost tracking and analytics
- ⏳ Additional model providers (local models, custom endpoints)
- ⏳ Team activity feed and notifications

## Design Direction

The design should feel **professional, trustworthy, and efficient** - like a tool built by developers for developers. Clean typography, generous whitespace, and purposeful use of color communicate quality. The interface is **minimal and functional** - every element serves the core purpose of managing prompts effectively.

## Color Selection

**Analogous blue palette** - Professional blues with warm accents for actions, creating trust and focus.

- **Primary Color**: `oklch(0.55 0.22 250)` - Vibrant blue (#3366FF family) communicates action, trust, and primary CTAs
- **Secondary Colors**: 
  - Light blue `oklch(0.92 0.02 250)` for subtle backgrounds
  - Medium blue `oklch(0.75 0.12 250)` for hover states
- **Accent Color**: `oklch(0.55 0.22 250)` - Same as primary for cohesion, used for buttons and interactive elements
- **Foreground/Background Pairings**:
  - Background (Light gray `oklch(0.98 0 0)`): Dark text `oklch(0.25 0 0)` - Ratio 14.2:1 ✓
  - Card (White `oklch(1 0 0)`): Dark text `oklch(0.25 0 0)` - Ratio 15.4:1 ✓
  - Primary (Blue `oklch(0.55 0.22 250)`): White text `oklch(1 0 0)` - Ratio 6.8:1 ✓
  - Accent (Blue `oklch(0.55 0.22 250)`): White text `oklch(1 0 0)` - Ratio 6.8:1 ✓
  - Muted (Light gray `oklch(0.96 0 0)`): Medium gray text `oklch(0.50 0 0)` - Ratio 4.5:1 ✓

## Font Selection

**Inter** - Modern, highly legible sans-serif perfect for UI work and technical content. Its multiple weights support clear hierarchy without feeling robotic.

- **Typographic Hierarchy**:
  - H1 (Page Title): Inter SemiBold/32px/tight tracking (-0.02em)
  - H2 (Section Header): Inter SemiBold/24px/tight tracking (-0.01em)
  - H3 (Card Title): Inter Medium/18px/normal tracking
  - Body (Content): Inter Regular/15px/relaxed leading (1.6)
  - Small (Metadata): Inter Regular/13px/normal leading with muted color
  - Code (Prompt content): SF Mono/14px/normal leading (1.5) for readability

## Animations

**Subtle and purposeful** - animations guide attention and confirm actions without creating delay or distraction. Every motion respects the user's time.

- **Purposeful Meaning**: Quick fades (150ms) for state changes, smooth slides (250ms) for panels, gentle springs for interactive feedback
- **Hierarchy of Movement**: 
  - Primary actions (save, improve) get satisfying micro-interactions
  - Navigation transitions are instant or near-instant
  - Loading states use subtle pulse, never block interaction
  - Hover states respond within 50ms

## Component Selection

- **Components**: 
  - `Button` - Primary actions (Save, Improve), secondary (Cancel), ghost (toolbar)
  - `Input` & `Textarea` - Prompt title, content, search
  - `Card` - Prompt list items, version history entries, comment cards
  - `Badge` - Tags with colors, status indicators
  - `Dialog` - Create project/category, confirmations
  - `Select` - Project/category pickers
  - `Tabs` - Switch between editor/versions/comments
  - `ScrollArea` - Long lists and content areas
  - `Separator` - Visual section breaks
  - `Avatar` - User presence (using spark.user() data)
  - `Popover` - Tag color picker, quick actions menu
- **Customizations**: 
  - Tag badges with custom colors from palette
  - Prompt content editor with monospace font and subtle grid background
  - Version diff view with side-by-side or unified comparison
- **States**: 
  - Buttons: hover lifts with shadow, active compresses slightly, disabled grays out
  - Inputs: focus shows blue ring, error shows red ring and message below
  - Cards: hover elevates with shadow, selected shows blue left border
- **Icon Selection**: 
  - Plus (add prompt/tag), FloppyDisk (save), Clock (versions), ChatCircle (comments), 
  - Sparkle (improve), Tag, Folder (projects), FunnelSimple (filter), MagnifyingGlass (search)
- **Spacing**: 
  - Container padding: 8 (32px)
  - Card padding: 6 (24px)
  - Section gaps: 6 (24px)
  - Element gaps within cards: 4 (16px)
  - Tight inline gaps: 2 (8px)
- **Mobile**: 
  - Stack sidebar navigation to bottom nav bar
  - Full-width cards in list view
  - Editor becomes full-screen modal on mobile
  - Touch-friendly 44px minimum tap targets
  - Swipe gestures for navigation (back/forward)
