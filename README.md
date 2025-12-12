# promptArq

<div align="center">
  <img src="./src/assets/images/logo_icon_boxed.png" alt="promptArq Logo" width="120" height="120">
  
  ### promptArq - architeqt ur prompts <3
  
  Create, version, improve, and collaborate on AI prompts with your team. Built for prompt engineers, developers, and AI practitioners.

  [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
  [![TypeScript](https://img.shields.io/badge/TypeScript-5.7-blue)](https://www.typescriptlang.org/)
  [![React](https://img.shields.io/badge/React-19.0-blue)](https://react.dev/)
</div>

---

########################################################################################################################################################################################################

D I S C L A I M E R

Everything here is AI Generated. UI, Code, Content, ALL OF IT. 
Backstory: For a while I was lookin for a  reason to play with github spark and also parallel for a way to not write my prompts over and over again. 
Existing software either didnt meet my needs or was to expensive, so I created this spark project. Happy to chat about it, there's no "active" development.
Use it, leave it, up to you :-) 

Here's the public spark url / DEMO: https://arqioly-prompt-atom--tamaygz.github.app/

########################################################################################################################################################################################################

## 🪟 Windows Desktop Application

**New!** A native Windows desktop application is now available in the [`/WindowsApp`](./WindowsApp) directory. 

Features:
- Runs the Vite app in a native Windows window using WebView2
- Global hotkeys for quick access (Ctrl+Alt+P to show/hide, etc.)
- System tray integration
- Automatic Vite server management
- Configurable keyboard shortcuts

**[Quick Start Guide](./WindowsApp/QUICKSTART.md)** | **[Full Documentation](./WindowsApp/README.md)**

---

## 🎯 What is promptArq?

**promptArq** is a comprehensive prompt management system designed for teams and individuals working with Large Language Models (LLMs). It provides a structured, collaborative environment for creating, versioning, improving, and sharing AI prompts across your organization.

Think of it as **Git for AI prompts** - with built-in intelligence to help you write better prompts, track changes over time, and expose them to AI agents through MCP (Model Context Protocol).


<img width="2523" height="1246" alt="image" src="https://github.com/user-attachments/assets/439de6e8-f65f-4669-a6d7-1d841699e795" />



## ✨ Key Features

### 📝 **Prompt Management**
- Create and edit prompts with rich text editor
- Support for variable placeholders (`{{variableName}}`)
- Fill placeholders and execute prompts directly in the UI
- Auto-generate titles using AI
- Archive/restore prompts

### 🔄 **Version Control**
- Complete version history for every prompt
- Visual diff between versions
- Restore any previous version
- Track who changed what and when

### 🤖 **AI-Powered Improvements**
- Click "Improve Prompt" to get AI-enhanced versions
- Uses specialized system prompt for prompt optimization
- Shows before/after comparison
- Accept or reject improvements

### 🎯 **Smart Organization**
- **Projects**: Top-level containers for related prompts
- **Categories**: Organize prompts within projects
- **Tags**: Flexible, cross-project labeling with colors
- Smart tag suggestions based on usage
- Full-text search across all prompts

### 👥 **Team Collaboration**
- Create teams and invite members via shareable links
- Role-based access (Owner, Admin, Editor, Viewer)
- Share individual prompts with non-team members
- Comments and threaded discussions
- User presence indicators

### ⚙️ **Advanced Configuration**
- **System Prompts**: Reusable instructions for consistent AI behavior
- **Model Configs**: Save temperature, max tokens, top_p settings
- **Priority Resolution**: System prompts apply at prompt/project/category/tag levels
- Pre-configured templates for common use cases

### 🔌 **MCP Integration**
- Expose prompts to Claude Desktop and other MCP clients
- Enable/disable individual prompts for MCP
- Organized by project for easy discovery
- Full Model Context Protocol support

### 📤 **Import/Export**
- Export all prompts to JSON
- Backup your entire prompt library
- Share templates with others

### 🎨 **Beautiful UI**
- Modern, clean interface
- Dark mode support (follows system preferences)
- Responsive mobile design
- Smooth animations and transitions
- Collapsible sidebar for focused work

### 📚 **Template Library**
- 50+ pre-built prompt templates
- Categories: Social Media, Marketing, Developer, Software Architect, QA, Business Strategy, Acquisition, and more
- One-click template usage
- Cinema mode for comfortable browsing

---

## 🚀 Getting Started

### Prerequisites

promptArq can run in two modes:

1. **Spark Mode** - No setup required, runs in Spark runtime with automatic authentication
2. **Standalone Mode** - Requires GitHub OAuth setup for authentication

### Running the Application

#### Web Version (Spark)

1. **Access the app** - Simply open the URL where promptArq is deployed
2. **Automatic authentication** - You'll be authenticated automatically via Spark
3. **Start creating** - Click "New Prompt" to create your first prompt

That's it! No database setup, no API keys, no configuration files.

#### Standalone Mode (Local Development)

For local development or standalone deployment:

1. **Clone the repository**
   ```bash
   git clone https://github.com/tamaygz/promptArq.git
   cd promptArq
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Set up GitHub OAuth** (required for authentication)
   - Copy `.env.example` to `.env`
   - Create a GitHub OAuth App at https://github.com/settings/developers
   - Add your Client ID and Client Secret to `.env`
   - See **[OAuth Setup Guide](./OAUTH_SETUP.md)** for detailed instructions

4. **Run the development server**
   ```bash
   npm run dev
   ```

5. **Open your browser** at `http://localhost:5173`

**Note:** SQLite is used for data persistence in standalone mode.

#### Windows Desktop App

For Windows users, a native desktop application is available:

1. **Navigate to the WindowsApp folder**
2. **Build the app**: Run `build.bat` or `dotnet build`
3. **Run**: Execute the generated `.exe` file
4. **Enjoy**: The Vite server starts automatically and loads in the window

See the **[Windows App Quick Start Guide](./WindowsApp/QUICKSTART.md)** for detailed instructions.

### First Steps

#### 1. Create a Project
Projects help organize related prompts together.

1. Click **"Projects"** in the header
2. Click **"Create Project"**
3. Give it a name and description
4. Optionally add categories (e.g., "Data Analysis", "Content Writing")

#### 2. Create Your First Prompt
1. Click **"New Prompt"** in the header
2. Fill in the details:
   - **Title**: What this prompt does (or use ✨ to auto-generate)
   - **Project**: Select the project you created
   - **Category**: Optional categorization
   - **Tags**: Add relevant tags for filtering
3. Write your prompt in the content area
4. Add variables with `{{variableName}}` syntax if needed
5. Click **"Save"**

#### 3. Improve Your Prompt
1. Open any prompt
2. Click **"Improve Prompt"** button
3. Review the AI-suggested improvements
4. Accept to create a new version, or reject to keep current

#### 4. Execute a Prompt
- **With variables**: Click "Fill Placeholders" → Enter values → Execute
- **Without variables**: Click "Execute" → Select system prompt → Run
- Copy the results to clipboard

---

## 📸 Screenshots

### Main Dashboard
*The main interface showing prompt list, search, and organization*
<img width="2065" height="1253" alt="image" src="https://github.com/user-attachments/assets/c7521d73-e6b4-4637-a2dd-72eb1fe814cb" />


### Prompt Editor
*Rich editing environment with version history and AI improvements*
<img width="2061" height="1257" alt="image" src="https://github.com/user-attachments/assets/ce40c91c-21eb-435c-a237-466e1e4ed6c4" />


### Template Library
*Browse 50+ pre-built templates across multiple categories*
<img width="2028" height="1237" alt="image" src="https://github.com/user-attachments/assets/ade7df4f-31c2-49b5-9fd1-8faf3574ffe7" />


### Team Management
*Invite team members and manage access*
<img width="2051" height="1246" alt="image" src="https://github.com/user-attachments/assets/ccdb08d6-4198-4dd3-b050-1aed7ad65ab0" />


### PROMPT PLACEHOLDERS & GENERATION
<img width="2182" height="1155" alt="image" src="https://github.com/user-attachments/assets/3c31bb37-8263-4c85-b008-4d9cf006b68c" />


---

## 🎓 Core Concepts

### Projects
Projects are top-level containers that group related prompts. Each project can have:
- Multiple categories
- Custom color schemes
- Team access controls
- Dedicated system prompts

**Example**: "Customer Support", "Content Marketing", "Engineering Workflows"

### Categories
Categories organize prompts within a project. They're project-scoped and help with navigation.

**Example**: Within "Content Marketing" → "Blog Posts", "Social Media", "Email Campaigns"

### Tags
Tags provide flexible, cross-project organization. They:
- Work across all projects
- Have custom colors
- Show usage frequency
- Support auto-suggestions

**Example**: "draft", "reviewed", "production", "high-priority"

### Placeholders
Use double-curly-brace syntax to create reusable prompt templates:

```
Write a {{tone}} blog post about {{topic}} for {{audience}}.
The post should be approximately {{word_count}} words.
```

promptArq remembers your placeholder values between uses!

### System Prompts
System prompts define the AI's behavior and personality. They apply with smart precedence:

1. Prompt-level (highest priority)
2. Project-level
3. Category-level
4. Tag-level
5. Team default (lowest priority)

### Version History
Every save creates an immutable version with:
- Full content snapshot
- Timestamp and author
- Optional change notes
- Visual diff from previous version

### Sharing
Two types of sharing:
- **Team sharing**: Invite members to your team for full collaboration
- **Prompt sharing**: Generate a read-only link for individual prompts

---

## 🔌 MCP (Model Context Protocol) Integration

promptArq implements the Model Context Protocol, allowing AI agents like Claude to access your prompts directly.

### Setup for Claude Desktop

1. Click **"MCP Server"** in promptArq
2. Copy the configuration snippet
3. Open Claude Desktop config file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
4. Add the configuration:

```json
{
  "mcpServers": {
    "promptArq-prompts": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-fetch",
        "https://your-promptarq-url/api/mcp"
      ]
    }
  }
}
```

5. Restart Claude Desktop
6. Your prompts will appear in Claude's context menu

### Enabling Prompts for MCP

1. Open any prompt in promptArq
2. Click the settings/config button
3. Toggle **"Expose to MCP"**
4. Save the prompt

The prompt will now be available to Claude and other MCP clients, organized by project.

---

## 🏗️ Technical Architecture

### Built With
- **Frontend**: React 19 + TypeScript
- **Styling**: Tailwind CSS 4 + shadcn/ui components
- **State Management**: React Hooks
- **Data Persistence**: Spark KV (key-value store)
- **Animations**: Framer Motion
- **Icons**: Phosphor Icons
- **Authentication**: Spark Runtime (automatic)

### Data Persistence
All data is automatically persisted using the Spark KV store:
- ✅ Prompts and versions
- ✅ Projects, categories, and tags
- ✅ System prompts and model configs
- ✅ Teams and team members
- ✅ User preferences and settings
- ✅ Comments and shared prompts

No database setup required - it just works!

### Security
- Automatic authentication via Spark runtime
- Row-level data isolation per user
- Team-based access control
- Secure storage of all data
- No exposed API keys or secrets

---

## 📋 How to Run (Development)

If you want to run this locally or contribute:

```bash
# Clone the repository
git clone <repository-url>
cd promptarq

# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build
```

### Environment
This is a Vite + React application configured for the Spark runtime. The `vite.config.ts` is pre-configured and should not be modified.

---

## 🎨 Customization

### Theme
The app automatically follows your system's dark/light mode preference. Colors are defined in `src/main.css` using OKLCH color space for perceptual uniformity.

### System Prompts
Built-in system prompts are defined in `src/lib/default-system-prompts.ts`. To add more:

1. Edit the file
2. Add your category and system prompt
3. Rebuild the application

### Templates
Template library is in `src/lib/default-templates.ts`. Follow the existing structure to add more templates.

---

## 📚 Additional Documentation

- **[Product Spec](./product_spec_complete.md)** - Complete product requirements
- **[PRD](./PRD.md)** - Product Requirements Document
- **[Security](./SECURITY.md)** - Security policies
- **[OAuth Setup](./OAUTH_SETUP.md)** - Authentication details
- **[MCP Setup](./MCP_SETUP.md)** - Model Context Protocol guide

---

## 🤝 Contributing

This is a Spark application. To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test in the Spark runtime
5. Submit a pull request

---

## 📄 License

The Spark Template files and resources from GitHub are licensed under the MIT License.

Copyright (c) GitHub, Inc.

---

## 🙏 Acknowledgments

- Built on [GitHub Spark](https://githubnext.com/projects/spark/)
- UI components by [shadcn/ui](https://ui.shadcn.com/)
- Icons by [Phosphor Icons](https://phosphoricons.com/)
- Inspired by the prompt engineering community

---

## 💬 Support & Community

Need help or have questions?
- Check the documentation files in this repository
- Review the PRD for feature details
- Examine the code - it's well-commented and structured

---

<div align="center">
  Made with ❤️ for the AI community
  
  **promptArq** - Professional prompt management for everyone
</div>
