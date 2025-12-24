# promptArq - User Guide

Welcome to promptArq! This guide will help you get started with managing your AI prompts effectively.

## Quick Start

### Accessing promptArq

**Live Demo (GitHub Spark):**
Visit: https://arqioly-prompt-atom--tamaygz.github.app/

**Local Development:**
```bash
git clone https://github.com/tamaygz/promptArq.git
cd promptArq
npm install
npm run dev
```

### First Time Setup

1. **Authentication** (Optional but recommended for teams)
   - Click "Sign in with GitHub" in the top right
   - Authorize the application
   - Your prompts will be saved to your account

2. **Without Authentication**
   - All features available except team collaboration
   - Prompts saved to browser localStorage
   - Data persists across sessions on same device

## Core Concepts

### Prompts
The fundamental unit - a piece of text you use with AI models. Each prompt has:
- **Title** - Short, descriptive name
- **Description** - What the prompt does
- **Content** - The actual prompt text
- **Tags** - Labels for organization
- **Project & Category** - Hierarchical organization

### Projects
Top-level containers for organizing related prompts. Examples:
- "Customer Support" project
- "Content Writing" project  
- "Code Review" project

### Categories
Subdivisions within projects. Example:
- Project: "Customer Support"
  - Category: "Email Templates"
  - Category: "Chat Responses"

### Tags
Cross-project labels for finding prompts. Examples:
- `#technical`
- `#beginner-friendly`
- `#reviewed`

### System Prompts
Reusable components you can reference in prompts. Think of them as "include" statements for common instructions.

### Versions
Each time you significantly change a prompt, create a new version. View history, compare versions, and revert if needed.

## Features Guide

### Creating Prompts

#### From Scratch
1. Click **"+ New Prompt"** button (top bar)
2. Enter title and description
3. Select project and category
4. Add tags
5. Write prompt content
6. Click **Save** or press `Ctrl+S` / `⌘S`

#### From Template
1. Click **"Templates"** button (top bar)
2. Browse available templates
3. Click **"Use Template"**
4. Fill in placeholders (like `{{topic}}`, `{{tone}}`)
5. Template converts to prompt
6. Edit and save

**Popular Templates:**
- Code Review
- Email Response
- Content Outline
- Technical Documentation
- Social Media Post
- Bug Report

### Editing Prompts

1. Click any prompt in the list
2. Edit in right panel
3. Save changes (Ctrl+S / ⌘S)

**Versioning:**
- Minor changes: Just save
- Major changes: Click **"New Version"**
  - Add change note
  - Creates version history entry

**Keyboard Shortcuts:**
- `Ctrl+S` / `⌘S` - Save
- `Ctrl+I` / `⌘I` - Improve with AI (when configured)

### Using & Executing Prompts

#### Executing Prompts with Placeholders
If your prompt has placeholders like `{{topic}}` or `{{style}}`:

1. Open the prompt
2. Click **"Fill Placeholders"** button
3. A dialog appears with fields for each placeholder
4. Fill in the values
5. Click **"Execute"** to generate the final prompt
6. Copy the result to use with your AI tool

**Placeholder Features:**
- Values are remembered for next time
- Clear individual values or all at once
- Skip execution and just copy filled template

#### Using Project Variables
Project variables are defined once at the project level and automatically replaced in all prompts within that project. This is perfect for company names, product names, URLs, or any repeated values.

**Syntax Difference:**
- **Manual Placeholders**: `{{placeholder}}` (2 braces) - You fill these each time
- **Project Variables**: `{{{variable}}}` (3 braces) - Auto-replaced from project settings

**Creating Project Variables:**
1. Click **"Projects"** button (gear icon in top bar)
2. Go to the **"Variables"** tab
3. Select a project from the dropdown
4. Enter variable name (e.g., `company_name`)
5. Enter the value (e.g., `Acme Corporation`)
6. Click **"Add Variable"**

**Using Project Variables in Prompts:**
```
Dear {{customer_name}},

Thank you for contacting {{{company_name}}}.
You can reach us at {{{support_email}}} or {{{company_phone}}}.

Best regards,
{{{company_name}}} Support Team
```

When you execute this prompt:
- `{{{company_name}}}`, `{{{support_email}}}`, and `{{{company_phone}}}` are automatically replaced
- Only `{{customer_name}}` requires manual input

**Benefits:**
- **Consistency**: Same values across all prompts in a project
- **Efficiency**: No need to fill repeated values
- **Maintainability**: Update once, affects all prompts
- **Clarity**: Clear distinction between fixed and variable content

**Managing Variables:**
- View all variables grouped by project in the Variables tab
- Delete variables by clicking the trash icon
- Variables are stored with the project and sync across devices

#### Direct Execution
For prompts without placeholders:

1. Open the prompt
2. Click **"Execute"** button  
3. Optionally select a system prompt
4. Copy the result to use with your AI tool

#### Execution Modes

Prompts can be executed in two ways:

**Direct Execution (default):**
- Prompt content is copied directly to clipboard
- Faster, no LLM processing
- Ideal for templates, snippets, or pre-written content
- Look for standard prompts without an "LLM" badge

**LLM Execution:**
- Prompt is processed through LLM pipeline
- Combined with system prompts
- Can include preprocessing and transformations
- Indicated by a sparkle "LLM" badge on the prompt

**To enable LLM execution:**
1. Open prompt editor
2. Check **"Execute through LLM"** at the bottom
3. Save the prompt
4. Badge appears in prompt list

#### Copying & Using Prompts
- **Copy Button** - Quick copy to clipboard
- **Execute Dialog** - Fill placeholders and preview
- **Export** - Download as JSON for external use

### Organizing Prompts

#### Projects & Categories
1. Click **"Projects"** button (gear icon)
2. Create new project with color
3. Add categories to project
4. Assign prompts to project/category

#### Tags
1. Edit prompt
2. In tags field, type and press Enter
3. Tags autocomplete from existing
4. Create new tags on the fly

**Tag Colors:**
Tags automatically get colors. Same tag = same color everywhere.

#### Archiving
- Toggle **"Show Archived"** to see archived prompts
- Archive from prompt menu (⋮)
- Keeps prompts but removes from active view

### Searching & Filtering

**Search Bar:**
- Real-time search
- Searches title, description, content, tags
- Case-insensitive

**Project Filter:**
- Dropdown in top bar
- Select "All" or specific project
- Narrows visible prompts

**Tag Filter:**
- Click tags in tag list (left sidebar)
- Multiple tags = AND filter
- Clear filters to show all

**Combined Filtering:**
Project filter + Tag filter + Search = powerful organization

### Version Control

**View History:**
1. Open prompt
2. Scroll to **"Version History"** section
3. See all versions with change notes

**Compare Versions:**
1. Click **"View Diff"** on any version
2. Side-by-side comparison
3. Green = added, Red = removed

**Restore Version:**
1. View old version
2. Copy content
3. Paste in current version
4. Save as new version

### Team Collaboration

#### Creating Teams
1. Click **"Teams"** button (top bar)
2. Create new team
3. Enter name and description
4. Generate invite link

#### Inviting Members
1. Open team settings
2. Click **"Copy Invite Link"**
3. Share link with team members
4. They click link and join automatically

#### Team Features
- Shared prompt library (future)
- Comments on prompts
- User directory
- Collaborative editing (future)

### Commenting

1. Open prompt
2. Scroll to **"Comments"** section
3. Write comment
4. Submit
5. Comments visible to team members

### Sharing Prompts

#### Generate Share Link
1. Open prompt
2. Click **"Share"** button
3. Click **"Generate Share Link"**
4. Copy link and share

#### Access Shared Prompt
1. Click shared link
2. View read-only prompt
3. Click **"Copy to Library"** to save to your account

**Share Link Features:**
- Public access (no auth required)
- Read-only view
- Copy content button
- Save to your library option

### MCP Integration

**What is MCP?**
Model Context Protocol lets AI assistants (like Claude Desktop) access your prompts directly.

**Setup:**
1. Mark prompts for MCP exposure:
   - Edit prompt
   - Toggle **"Expose to MCP Server"**
   - Save

2. Configure Claude Desktop:
   - Click **"MCP Server"** button (top bar)
   - Review exposed prompts
   - Click **"Copy Configuration"**
   - Paste in Claude Desktop config

3. Use in Claude:
   - Type `/` in Claude
   - See your prompts as tools
   - Select prompt to insert

**[Full MCP Setup Guide](./MCP.md)**

### Model Configurations

Save AI model settings for reuse.

**Create Model Config:**
1. Click **"Model Config"** button (top bar)
2. Click **"Add Configuration"**
3. Enter settings:
   - Name (e.g., "GPT-4 Creative")
   - Provider (OpenAI, Anthropic, etc.)
   - Model name
   - Temperature
   - Max tokens
   - Top P
   - Penalties

4. Save configuration

**Use Model Config:**
- Reference in prompts
- Export for use in API calls
- Share with team

### System Prompts

Reusable instructions you can include in prompts.

**Create System Prompt:**
1. Click **"System Prompts"** button (top bar)
2. Add new system prompt
3. Enter name, category, content
4. Save

**Use System Prompt:**
- Reference in prompt content
- Include via template placeholder
- Combine multiple system prompts

**Categories:**
- Role definitions (e.g., "Expert Python developer")
- Output formats (e.g., "Respond in JSON")
- Constraints (e.g., "Use simple language")

### Importing & Exporting

**Export All Prompts:**
1. Click profile menu (top right)
2. Select **"Export Data"**
3. Download JSON file
4. Contains all prompts, projects, categories, tags

**Import Prompts:**
1. Click profile menu
2. Select **"Import Data"**
3. Choose JSON file
4. Select which items to import
5. Merge or replace existing data

**Export Single Prompt:**
- Copy content from editor
- Use share feature for formatted export

## Tips & Best Practices

### Naming Prompts
- Be descriptive: "Code Review for Python APIs" not "Code Review 1"
- Include use case: "Email: Customer Complaint Response"
- Version in name if needed: "Blog Post Generator v3"

### Writing Prompts
- Start with clear objective
- Include examples when helpful
- Specify output format
- Add constraints or requirements
- Use placeholders for variables: `{{topic}}`, `{{style}}`
- Use project variables for fixed values: `{{{company_name}}}`, `{{{product}}}`

### Using Variables Effectively
- **Use project variables** for company info, product names, URLs, standard terms
- **Use manual placeholders** for customer-specific, case-specific, or dynamic content
- **Naming conventions**: Use descriptive names like `company_name` not `cn`
- **Keep it simple**: Don't over-use variables; only for frequently repeated values
- **Test replacements**: Verify variables replace correctly before sharing prompts

### Organizing
- One project per major domain
- Categories for subtopics
- Tags for cross-cutting concerns
- Archive outdated prompts (don't delete)

### Versioning
- Create version for significant changes
- Write meaningful change notes
- Keep old versions for reference
- Test new versions before replacing

### Teams
- Define team purpose clearly
- Set conventions (naming, tagging)
- Comment on prompts for context
- Share best practices

### Templates
- Create templates for repeated patterns
- Use meaningful placeholder names
- Test templates before sharing
- Keep templates generic enough for reuse

## Keyboard Shortcuts

### Editor
- `Ctrl+S` / `⌘S` - Save prompt
- `Ctrl+I` / `⌘I` - Improve prompt (AI)
- `Esc` - Close editor

### Navigation
- `/` - Focus search
- `Ctrl+K` / `⌘K` - Quick actions (future)

### General
- `Ctrl+Z` / `⌘Z` - Undo
- `Ctrl+Shift+Z` / `⌘Shift+Z` - Redo

## Troubleshooting

### Prompts not saving
- Check browser localStorage quota (5-10MB limit)
- Try exporting and importing data
- Clear old/archived prompts
- Use GitHub Spark deployment for unlimited storage

### Authentication issues
- Clear browser cache
- Check OAuth configuration
- Verify redirect URLs
- Try incognito mode

### Search not finding prompts
- Check filters (project, tags)
- Verify "Show Archived" state
- Search is case-insensitive
- Try partial words

### MCP not working
- Verify prompts marked "Expose to MCP"
- Check Claude Desktop configuration
- Restart Claude Desktop
- See full [MCP Guide](./MCP.md)

### Performance slow
- Reduce number of prompts displayed
- Archive old prompts
- Clear browser cache
- Disable animations (future setting)

## FAQs

**Q: Where is my data stored?**
A: GitHub Spark deployment uses Spark KV store. Local deployment uses browser localStorage. All data stays client-side, no server storage.

**Q: Can I use without GitHub account?**
A: Yes! All features work except team collaboration. Data saves to browser localStorage.

**Q: Is there a mobile app?**
A: Not yet, but the web app is mobile-responsive. Use in mobile browser.

**Q: How many prompts can I store?**
A: Spark deployment: unlimited. localStorage: ~5-10MB (hundreds of prompts). Export regularly as backup.

**Q: Can I collaborate in real-time?**
A: Not yet. Currently async collaboration via comments and shared prompts. Real-time editing is planned.

**Q: How do I backup my data?**
A: Export all data regularly via profile menu. Stores JSON file you can re-import anytime.

**Q: Can I use my own AI models?**
A: Model configs store settings but don't execute. Use exported prompts with any AI service.

**Q: Is there an API?**
A: Not yet. MCP integration provides similar functionality for Claude Desktop.

**Q: How do I report bugs?**
A: Create issue on [GitHub repository](https://github.com/tamaygz/promptArq/issues).

## Getting Help

- **Documentation:** `/docs` folder
- **GitHub Issues:** Bug reports and feature requests
- **Discussions:** Community support
- **Demo Site:** Try features live

## Next Steps

- Read [Architecture Guide](./Architecture.md) for technical details
- See [Developer Guide](./DeveloperGuide.md) for contributing
- Check [MCP Setup](./MCP.md) for Claude Desktop integration
- Visit [Windows App](../WindowsApp/README.md) for desktop version
