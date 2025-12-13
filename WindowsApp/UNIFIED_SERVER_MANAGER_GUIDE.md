# UnifiedServerManager - Quick Reference

## Overview
Single centralized manager for all PromptArq server processes.

## Managed Services
- **Vite Dev Server** (port 5000) - React/TypeScript frontend
- **OAuth Proxy Server** (port 3001) - GitHub OAuth backend (started via concurrently)
- **LocalStorage Server** (port 5001) - In-process HTTP API for shared storage

## API

### Start All Servers
```csharp
UnifiedServerManager.Start();
```
- Idempotent - safe to call multiple times
- Starts LocalStorage server first, then Vite dev (which includes OAuth proxy)
- Throws exception if startup fails

### Stop All Servers
```csharp
UnifiedServerManager.Stop();
```
- Idempotent - safe to call multiple times
- Thread-safe with lock
- Uses 5-layer cleanup strategy:
  1. Graceful shutdown
  2. Process tree kill (taskkill)
  3. Command-line detection kill
  4. Port-based kill
  5. Verification

### Check Status
```csharp
bool running = UnifiedServerManager.IsRunning;
```
- Returns true if servers are running
- Returns false during shutdown or if not started

## Usage Pattern

### MainForm.cs
```csharp
public MainForm()
{
    // ... initialization ...
    UnifiedServerManager.Start();
    // ... rest of constructor ...
}

private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
{
    UnifiedServerManager.Stop();
}

protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        UnifiedServerManager.Stop();
    }
    base.Dispose(disposing);
}
```

### Program.cs
```csharp
static void Main()
{
    Application.ApplicationExit += (s, e) => UnifiedServerManager.Stop();
    AppDomain.CurrentDomain.ProcessExit += (s, e) => UnifiedServerManager.Stop();
    
    try
    {
        Application.Run(new MainForm());
    }
    finally
    {
        UnifiedServerManager.Stop();
    }
}
```

## Architecture Highlights

### Thread Safety
- All operations protected by `lock (_lock)`
- Safe concurrent access from multiple exit handlers

### State Management
- `_isStarted` - Tracks if servers are running
- `_isShuttingDown` - Prevents re-entrance during shutdown

### Process Tracking
- `_viteDevProcess` - Main npm/cmd process
- `_storageServer` - In-process HTTP server

### Port Registry
```csharp
private static readonly int[] ManagedPorts = { 5000, 3001, 5001 };
```

## Troubleshooting

### Servers Won't Start
1. Check if ports are already in use
2. Verify Node.js and npm are installed
3. Check project root can be found
4. Look at debug output for errors

### Servers Won't Stop
1. Check debug output - shows each cleanup stage
2. Manually verify with PowerShell:
   ```powershell
   Get-NetTCPConnection -LocalPort 5000,3001,5001
   ```
3. Look for errors in taskkill output

### Orphaned Processes
If processes persist after app closes:
1. Check that all exit handlers are registered
2. Verify no exceptions during cleanup
3. Use manual cleanup PowerShell script

## Debug Output

The manager logs extensively:
```
[UnifiedServerManager] Starting all servers...
[UnifiedServerManager] Starting LocalStorage server on port 5001...
[UnifiedServerManager] LocalStorage server started
[UnifiedServerManager] Starting Vite dev server from: C:\path\to\project
[UnifiedServerManager] Vite dev process started with PID 12345
[UnifiedServerManager] All servers started successfully

[UnifiedServerManager] ========================================
[UnifiedServerManager] STOPPING ALL SERVERS
[UnifiedServerManager] ========================================
[UnifiedServerManager] Stopping storage server gracefully...
[UnifiedServerManager] Storage server stopped gracefully
[UnifiedServerManager] Force killing process trees...
[UnifiedServerManager] Killing process tree for PID 12345
[UnifiedServerManager] Killing Node.js processes by command line detection...
[UnifiedServerManager] Killed 2 Node.js process(es) by command line
[UnifiedServerManager] Port 5000 successfully released
[UnifiedServerManager] Port 3001 successfully released
[UnifiedServerManager] Port 5001 successfully released
[UnifiedServerManager] ========================================
[UnifiedServerManager] SHUTDOWN COMPLETE
[UnifiedServerManager] ========================================
```

## Migration from Old Managers

### Old Code
```csharp
// Old way - DON'T USE
ViteProcessManager.RegisterProcess(process);
ViteProcessManager.CleanupProcess();

OAuthProxyManager.RegisterProcess(process);
OAuthProxyManager.CleanupProcess();

StorageServerManager.RegisterServer(server);
StorageServerManager.CleanupServer();
```

### New Code
```csharp
// New way - USE THIS
UnifiedServerManager.Start();
UnifiedServerManager.Stop();
```

All the old manager classes have been deleted:
- ❌ ViteProcessManager.cs
- ❌ OAuthProxyManager.cs  
- ❌ StorageServerManager.cs

Use `UnifiedServerManager` for everything!
