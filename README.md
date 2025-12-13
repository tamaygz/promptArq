# promptArq

<div align="center">
  <img src="./src/assets/images/logo_icon_boxed.png" alt="promptArq Logo" width="120" height="120">
  
  ### promptArq - architeqt ur prompts <3
  
  Create, version, improve, and collaborate on AI prompts with your team. Built for prompt engineers, developers, and AI practitioners.

  [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
  [![TypeScript](https://img.shields.io/badge/TypeScript-5.7-blue)](https://www.typescriptlang.org/)
  [![React](https://img.shields.io/badge/React-19.0-blue)](https://react.dev/)
  
  **[Try it now](https://arqioly-prompt-atom--tamaygz.github.app/)** | **[User Guide](./docs/UserGuide.md)** | **[Windows App](./WindowsApp/)**
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

---

## 🎯 What is promptArq?

**promptArq** is a comprehensive prompt management system for teams and individuals working with Large Language Models. It provides a structured environment for creating, versioning, improving, and sharing AI prompts.

**Think of it as Git for AI prompts** - with built-in intelligence to help you write better prompts, track changes over time, and expose them to AI agents through MCP (Model Context Protocol).


<img width="2523" height="1246" alt="image" src="https://github.com/user-attachments/assets/439de6e8-f65f-4669-a6d7-1d841699e795" />



## ✨ Key Features

### � **Flexible Authentication**
- **Spark Mode**: Automatic authentication via Spark runtime
- **Standalone Mode**: GitHub OAuth integration
- Seamless switching between modes
- Secure token management with PKCE flow
- See [OAuth Setup Guide](./OAUTH_SETUP.md) for configuration

### �📝 **Prompt Management**
- Create and edit prompts with rich text editor
- Support for variable placeholders (`{{variableName}}`)
- Fill placeholders and execute prompts directly in the UI
- Auto-generate titles using AI
- Archive/restore prompts

### 🔄 **Version Control**
## ✨ Features

### 📝 **Prompt Management**
- Rich text editor with syntax highlighting
- Version control for every change
- AI-powered prompt improvements
- Duplicate and template support
- Full-text search across all prompts

### 🗂️ **Organization**
- **Projects** - Top-level containers
- **Categories** - Within-project organization
- **Tags** - Flexible cross-project labels with colors
- Smart tag suggestions based on usage

### 🔄 **Version Control**
- Complete history for every prompt
- Visual diffs between versions
- Restore any previous version
- Track changes with timestamps

### 🤝 **Team Collaboration**
- Create teams with role-based access (Owner/Admin/Editor/Viewer)
- Share individual prompts via links
- Comments and discussions
- User presence indicators

### 🎯 **Templates & Placeholders**
- 50+ pre-built templates (Marketing, Development, QA, Strategy, etc.)
- Placeholder system: `{{variableName}}`
- Remembered values for repeated use
- Cinema mode template browser

### ⚙️ **Advanced Configuration**
- System prompts with priority resolution (prompt → project → category → tag)
- Model configs (temperature, max tokens, top_p)
- Pre-configured templates
- Import/export prompts as JSON

### 🔌 **MCP Integration**
- Expose prompts to Claude Desktop
- Model Context Protocol support
- Enable/disable per prompt
- Organized by project

### 🎨 **Modern UI**
- Clean, responsive interface
- Dark mode (follows system preference)
- Smooth animations
- Collapsible sidebar
- Mobile-friendly design

---

## 🚀 Quick Start

### Web App

**Try it now:** [https://arqioly-prompt-atom--tamaygz.github.app/](https://arqioly-prompt-atom--tamaygz.github.app/)

**Run locally:**
```bash
git clone https://github.com/tamaygz/promptArq.git
cd promptArq
npm install
npm run dev
```

Open [http://localhost:5173](http://localhost:5173)

**Note:** Requires GitHub OAuth setup for authentication. See [docs/DeveloperGuide.md](./docs/DeveloperGuide.md) for details.

### Windows Desktop App

A native Windows application with global hotkeys and system tray integration:

```bash
cd WindowsApp
build.bat
```

Run the generated `.exe` file. The Vite server starts automatically.

**Features:**
- Global hotkey (Ctrl+Alt+P) to show/hide
- System tray integration
- Automatic Vite server management
- WebView2-based native window

**[Windows App Documentation](./WindowsApp/README.md)** | **[Quick Start](./WindowsApp/docs/UserGuide.md)**

---

## 📖 Documentation

- **[User Guide](./docs/UserGuide.md)** - Complete feature walkthrough
- **[Architecture](./docs/Architecture.md)** - Technical architecture
- **[Developer Guide](./docs/DeveloperGuide.md)** - Setup, contributing, debugging
- **[MCP Integration](./docs/MCP.md)** - Claude Desktop setup
- **[Windows App](./WindowsApp/README.md)** - Desktop application

---

## 🏗️ Technology Stack

**Web App:**
- React 19 + TypeScript + Vite
- Tailwind CSS 4 + Radix UI
- Dual storage: Spark KV or localStorage
- Express OAuth proxy server

**Windows App:**
- .NET 8.0 Windows Forms
- WebView2 integration
- Global hotkey manager
- System tray support

---

## 📸 Screenshots

### Main Dashboard
<img width="2065" height="1253" alt="Main dashboard" src="https://github.com/user-attachments/assets/c7521d73-e6b4-4637-a2dd-72eb1fe814cb" />

### Prompt Editor
<img width="2061" height="1257" alt="Prompt editor" src="https://github.com/user-attachments/assets/ce40c91c-21eb-435c-a237-466e1e4ed6c4" />

### Template Library
<img width="2028" height="1237" alt="Template library" src="https://github.com/user-attachments/assets/ade7df4f-31c2-49b5-9fd1-8faf3574ffe7" />

---

## 🤝 Contributing

Contributions welcome! See [docs/DeveloperGuide.md](./docs/DeveloperGuide.md) for setup instructions.

1. Fork the repository
2. Create feature branch: `git checkout -b feature/my-feature`
3. Make changes and test
4. Commit: `git commit -m "Add my feature"`
5. Push: `git push origin feature/my-feature`
6. Open Pull Request

---

## 📄 License

MIT License. See [LICENSE](./LICENSE) for details.

---

## 🙏 Acknowledgments

- Built with [GitHub Spark](https://githubnext.com/projects/spark/)
- UI by [shadcn/ui](https://ui.shadcn.com/) and [Radix UI](https://www.radix-ui.com/)
- Icons by [Phosphor Icons](https://phosphoricons.com/)

---

<div align="center">
  
Made with ❤️ for prompt engineers

</div>
