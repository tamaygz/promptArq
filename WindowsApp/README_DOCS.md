# Server Process Management - Documentation Index

This directory contains comprehensive documentation for the server process management solution implemented in PromptArq Windows application.

---

## ?? Quick Start

**New to this?** Start here:
1. Read [`COMPLETE_SOLUTION_SUMMARY.md`](COMPLETE_SOLUTION_SUMMARY.md) - High-level overview
2. Use [`QUICK_REFERENCE.md`](QUICK_REFERENCE.md) - Common commands
3. Run `TestAllServers.ps1` - Verify it works

**Need details?** See [`SERVER_MANAGEMENT.md`](SERVER_MANAGEMENT.md)

---

## ?? Documentation Files

### Primary Documents

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **[COMPLETE_SOLUTION_SUMMARY.md](COMPLETE_SOLUTION_SUMMARY.md)** | Complete overview of the solution | First read, understanding architecture |
| **[SERVER_MANAGEMENT.md](SERVER_MANAGEMENT.md)** | Detailed technical documentation | Deep dive, troubleshooting |
| **[VITE_SHUTDOWN_FIX.md](VITE_SHUTDOWN_FIX.md)** | Vite-specific implementation details | Vite issues, node.exe problems |
| **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** | Quick commands and troubleshooting | Daily use, quick lookup |

### Supporting Files

| File | Type | Purpose |
|------|------|---------|
| **TestAllServers.ps1** | Script | Test both servers are shut down |
| **TestViteCleanup.ps1** | Script | Test Vite cleanup only |
| **UpdateMainForm.ps1** | Script | Auto-update MainForm.cs for Vite |
| **UpdateMainFormStorageServer.ps1** | Script | Auto-update MainForm.cs for Storage |

---

## ?? What Problem Does This Solve?

When debugging the PromptArq Windows app and stopping with **Shift+F5** (Stop Debugging), two server processes would continue running:

1. **Vite Development Server** (port 5000) - Node.js process
2. **LocalStorage HTTP Server** (port 5001) - .NET HttpListener

This caused:
- ? Blocked ports preventing restart
- ? Orphaned node.exe processes consuming resources
- ? "Address already in use" errors
- ? Need for manual cleanup or system restart

**Now**: Both servers shut down cleanly on **all** exit paths, including debugger stop.

---

## ?? How It Works

### Architecture

```
Program.cs (Application Entry)
    ?
    ???? Exit Handlers ?????????
    ?    • ApplicationExit     ?
    ?    • ProcessExit         ?  Called on ALL exit paths
    ?    • UnhandledException  ?  (including Shift+F5)
    ?    • finally block       ?
    ?                          ?
    ???? ViteProcessManager ????????? Vite Server (port 5000)
    ?        • taskkill /F /T
    ?        • Process.Kill
    ?        • WMI orphan scan
    ?
    ???? StorageServerManager ??????? Storage Server (port 5001)
             • HttpListener.Stop()
             • Dispose()
```

### Manager Classes

| Manager | Manages | Port | Termination Strategy |
|---------|---------|------|---------------------|
| **ViteProcessManager** | Node.js/npm/Vite | 5000 | Force kill process tree + orphan detection |
| **StorageServerManager** | .NET HttpListener | 5001 | Graceful stop + dispose |

---

## ?? Testing

### Quick Test
```powershell
# 1. Start app with F5
# 2. Stop with Shift+F5
# 3. Run test:
cd WindowsApp
.\TestAllServers.ps1

# Expected: ? All checks passed
```

### Manual Verification
```powershell
# Check if ports are free
netstat -ano | findstr "5000 5001"

# Should return nothing (ports are free)
```

---

## ?? Document Map

```
Understanding the Problem
    ??? COMPLETE_SOLUTION_SUMMARY.md (Overview)
            ??? SERVER_MANAGEMENT.md (Technical Deep Dive)
            ?       ??? Vite Server Details
            ?       ??? Storage Server Details
            ?       ??? Architecture & Testing
            ?
            ??? VITE_SHUTDOWN_FIX.md (Vite-Specific)
                    ??? Process Tree Management
                    ??? Orphan Detection

Daily Usage
    ??? QUICK_REFERENCE.md
            ??? Common Commands
            ??? Quick Troubleshooting
            ??? Port Management

Testing & Verification
    ??? TestAllServers.ps1 (Both servers)
    ??? TestViteCleanup.ps1 (Vite only)

Implementation Details (Code)
    ??? ViteProcessManager.cs
    ??? StorageServerManager.cs
    ??? Program.cs (exit handlers)
```

---

## ?? Learning Path

### For New Developers
1. **Start**: COMPLETE_SOLUTION_SUMMARY.md
2. **Understand**: SERVER_MANAGEMENT.md
3. **Practice**: Run TestAllServers.ps1
4. **Reference**: Keep QUICK_REFERENCE.md handy

### For Troubleshooting
1. **Quick fix**: QUICK_REFERENCE.md
2. **Deep dive**: SERVER_MANAGEMENT.md
3. **Vite issues**: VITE_SHUTDOWN_FIX.md
4. **Verify**: Run TestAllServers.ps1

### For Maintenance
1. **Architecture**: SERVER_MANAGEMENT.md
2. **Implementation**: Review manager classes in code
3. **Testing**: TestAllServers.ps1 + manual verification
4. **Updates**: Modify managers, update docs

---

## ?? Finding Information

### "How do I...?"

| Task | Document | Section |
|------|----------|---------|
| Understand the overall solution | COMPLETE_SOLUTION_SUMMARY.md | Overview |
| Fix port conflicts | QUICK_REFERENCE.md | Troubleshooting |
| Add a new server | SERVER_MANAGEMENT.md | Maintenance |
| Test cleanup | QUICK_REFERENCE.md | Testing |
| Debug Vite issues | VITE_SHUTDOWN_FIX.md | Troubleshooting |
| Check if working | TestAllServers.ps1 | Run script |

### "What if...?"

| Scenario | Document | Solution |
|----------|----------|----------|
| Port 5000 still in use | QUICK_REFERENCE.md | Kill node.exe commands |
| Port 5001 still in use | QUICK_REFERENCE.md | Kill by PID |
| Both ports blocked | TestAllServers.ps1 | Run for diagnostics |
| Want to add server | SERVER_MANAGEMENT.md | Maintenance section |
| Debugger stop not working | VITE_SHUTDOWN_FIX.md | Verify managers |

---

## ?? Success Indicators

After implementing this solution, you should see:

? **No orphaned processes** after stop
? **Ports free** immediately after stop
? **Can restart** without errors
? **Debug output** shows cleanup logs
? **Test scripts** pass all checks

---

## ??? Files Reference

### Source Code
- `ViteProcessManager.cs` - Vite lifecycle management
- `StorageServerManager.cs` - Storage lifecycle management  
- `Program.cs` - Application exit handlers
- `MainForm.cs` - Server registration
- `LocalStorageServer.cs` - HTTP server implementation

### Scripts
- `TestAllServers.ps1` - Comprehensive test
- `TestViteCleanup.ps1` - Vite-specific test
- `UpdateMainForm.ps1` - Vite integration automation
- `UpdateMainFormStorageServer.ps1` - Storage integration automation

### Documentation
- `COMPLETE_SOLUTION_SUMMARY.md` - Complete overview
- `SERVER_MANAGEMENT.md` - Technical guide
- `VITE_SHUTDOWN_FIX.md` - Vite details
- `QUICK_REFERENCE.md` - Quick commands
- `README_DOCS.md` - This file

---

## ?? Need Help?

1. **Quick issue**: Check QUICK_REFERENCE.md
2. **Understanding**: Read COMPLETE_SOLUTION_SUMMARY.md
3. **Deep problem**: Study SERVER_MANAGEMENT.md
4. **Test status**: Run TestAllServers.ps1
5. **Still stuck**: Check debug Output window for manager logs

---

## ?? Important Notes

### Port Usage
- **5000**: Vite development server (HTTP)
- **5001**: LocalStorage HTTP API (HTTP)

### Exit Paths Covered
- ? Normal close (X button)
- ? Debug stop (Shift+F5) ? **Primary fix**
- ? Tray exit
- ? Application.Exit()
- ? Unhandled exceptions
- ? Try/finally safety net

### Best Practices
1. Always use managers (don't call Stop() directly)
2. Check debug output for cleanup logs
3. Run TestAllServers.ps1 after testing
4. Keep documentation updated

---

**Last Updated**: 2024  
**Status**: ? Complete  
**Build**: ? Successful  
**Tests**: ? Passing  

---

## ?? Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024 | Initial implementation - Both servers managed |

---

**Quick Links**:
- [Complete Solution](COMPLETE_SOLUTION_SUMMARY.md)
- [Technical Guide](SERVER_MANAGEMENT.md)
- [Quick Reference](QUICK_REFERENCE.md)
- [Test Script](TestAllServers.ps1)
