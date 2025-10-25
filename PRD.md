# arqioly – Prompt Management Web App (MVP)

A streamlined prompt management system for teams to create, organize, version, and improve LLM prompts collaboratively.

**Experience Qualities**:
1. **Professional** - Clean, confident interface that conveys expertise and reliability
2. **Efficient** - Minimal friction from idea to improved prompt with clear workflows
3. **Collaborative** - Transparent team interactions with versioning and commenting

**Complexity Level**: Light Application (multiple features with basic state)
- Core features include prompt management, versioning, tagging, and AI-powered improvements without requiring backend infrastructure

## Essential Features

### Prompt Library
- **Functionality**: Central repository showing all prompts with metadata
- **Purpose**: Quick access and overview of team's prompt inventory
- **Trigger**: Landing page after app load
- **Progression**: View grid → Filter by tag/project → Click prompt → Open editor
- **Success criteria**: All prompts visible, filterable, and accessible within 2 clicks

### Prompt Editor
- **Functionality**: Create and edit prompts with title, content, tags, project, category
- **Purpose**: Primary workspace for prompt authoring
- **Trigger**: Click "New Prompt" or select existing prompt
- **Progression**: Enter details → Save version → Add change note → Persist to storage
- **Success criteria**: Changes saved immediately, version history preserved

### Version History
- **Functionality**: View all versions with timestamps, authors, and change notes
- **Purpose**: Track evolution and enable rollback
- **Trigger**: Click "History" in prompt editor
- **Progression**: View list → Select version → Preview content → Restore if desired
- **Success criteria**: Every save creates immutable version, easy comparison

### AI Prompt Improvement
- **Functionality**: Analyze prompt and suggest improvements using LLM
- **Purpose**: Automate prompt optimization with AI assistance
- **Trigger**: Click "Improve Prompt" button
- **Progression**: Click button → LLM analyzes → View suggestion → Accept or reject → New version created if accepted
- **Success criteria**: Suggestions appear within 5s, acceptance creates new version with metadata

### Organization System
- **Functionality**: Projects, categories, and tags for taxonomy
- **Purpose**: Structure growing prompt library
- **Trigger**: Assign during prompt creation/editing
- **Progression**: Create project/category/tag → Assign to prompt → Filter library by taxonomy
- **Success criteria**: Multi-dimensional filtering, consistent taxonomy across team

### Comments & Collaboration
- **Functionality**: Thread discussions on prompts and versions
- **Purpose**: Async feedback and decision documentation
- **Trigger**: Click comment icon on prompt
- **Progression**: View existing → Add comment → Mention user → Receive notification
- **Success criteria**: Comments persist with versions, mentions work

## Edge Case Handling

- **No prompts state** - Welcoming empty state with quick-start guide and "Create First Prompt" CTA
- **Improve fails** - Clear error message, option to retry, doesn't lose user's work
- **Long content** - Scrollable editor with fixed action bar, line numbers for reference
- **Duplicate names** - Allow duplicates but show project/category context for disambiguation
- **Version limit** - No artificial limit in MVP; warn if storage approaching capacity
- **Offline mode** - Show connection status, queue changes for sync when back online

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
