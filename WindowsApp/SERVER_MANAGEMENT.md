# Server Process Management - Complete Guide

## Overview

The PromptArq Windows application manages **three server processes** that must be properly shut down when the app closes:

1. **Vite Development Server** (Port 5000) - Node.js/npm process
2. **OAuth Proxy Server** (Port 3001) - Node.js/Express process (started by npm run dev)
3. **LocalStorage HTTP Server** (Port 5001) - .NET HttpListener

**Note**: The OAuth Proxy Server (port 3001) is started automatically as part of `npm run dev` via the `concurrently` package. When the Vite process tree is terminated, the OAuth proxy is also terminated.

---

## 1. Vite Development Server (Port 5000)

### What It Does
- Runs `npm run dev` which starts both:
  - Vite dev server (port 5000)
  - OAuth proxy server (port 3001) via `concurrently`
- Provides hot module replacement (HMR)
- Serves static assets and handles Vite's dev middleware

### Process Tree
```
PromptArq.exe
  ?? cmd.exe
      ?? npm.cmd
          ?? node.exe (concurrently)
              ?? node.exe (vite on port 5000)
              ?? node.exe (server.js OAuth proxy on port 3001)
```

### Management
- **Manager Class**: `ViteProcessManager.cs`
- **Registration**: Called after `_viteProcess.Start()`
- **Cleanup Methods**:
  1. `taskkill /F /T /PID {pid}` - Force kill process tree (kills Vite + OAuth proxy)
  2. `Process.Kill(entireProcessTree: true)` - .NET backup method
  3. WMI scan for orphaned node.exe processes running Vite or server.js

### Port Check
```powershell
netstat -ano | findstr ":5000 :3001"
```

---

## 2. OAuth Proxy Server (Port 3001)

### What It Does
- Securely handles GitHub OAuth token exchange
- Keeps client secret on server-side only
- CORS-enabled for localhost development
- Started automatically by `npm run dev` via `concurrently`

### Endpoints
- `GET /health` - Health check
- `POST /api/auth/github/token` - Token exchange endpoint

### Management
- **Managed implicitly** by ViteProcessManager (same process tree)
- When `npm run dev` is killed, both Vite and OAuth proxy are terminated
- **Optional**: `OAuthProxyManager.cs` available for explicit management if needed in future

### Port Check
```powershell
netstat -ano | findstr :3001
```

---

## 3. LocalStorage HTTP Server (Port 5001)

### What It Does
- Provides REST API for shared storage between browser and WebView2
- Uses SQLite database for persistence
- Enables cross-context data synchronization

### Endpoints
- `GET /keys` - List all storage keys
- `GET /get?key={key}` - Retrieve value
- `POST /set?key={key}` - Store value (body contains JSON)
- `DELETE /delete?key={key}` - Remove value
- `GET /health` - Health check

### Management
- **Manager Class**: `StorageServerManager.cs`
- **Registration**: Called after `_storageServer.Start()`
- **Cleanup Method**:
  - `_storageServer.Stop()` - Stops HttpListener
  - `_storageServer.Dispose()` - Releases resources

### Port Check
```powershell
netstat -ano | findstr :5001
```

---

## Unified Cleanup Architecture

### Exit Handlers (Program.cs)

All exit handlers clean up **both** process managers:

```csharp
private static void OnApplicationExit(object? sender, EventArgs e)
{
    Debug.WriteLine("[Program] ApplicationExit event triggered");
    ViteProcessManager.CleanupProcess();      // Kills Vite + OAuth proxy tree
    StorageServerManager.CleanupServer();     // Stops Storage server
}

private static void OnProcessExit(object? sender, EventArgs e)
{
    Debug.WriteLine("[Program] ProcessExit event triggered");
    ViteProcessManager.CleanupProcess();      // Kills Vite + OAuth proxy tree
    StorageServerManager.CleanupServer();     // Stops Storage server
}

private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    Debug.WriteLine("[Program] UnhandledException event triggered");
    ViteProcessManager.CleanupProcess();      // Kills Vite + OAuth proxy tree
    StorageServerManager.CleanupServer();     // Stops Storage server
}

// Plus try/finally in Main() as last resort
```

### Cleanup Flow

```
App Exit Trigger
    ?
Program.cs Exit Handler
    ?
    ?? ViteProcessManager.CleanupProcess()
    ?   ?? taskkill /F /T /PID {pid}
    ?   ?   ?? Kills entire tree (npm ? concurrently ? vite + server.js)
    ?   ?? Process.Kill(entireProcessTree: true)
    ?   ?? Scan for orphaned node.exe (both Vite and OAuth proxy)
    ?
    ?? StorageServerManager.CleanupServer()
        ?? HttpListener.Stop()
        ?? IDisposable.Dispose()
```

---

## Testing All Servers

### Comprehensive Test Script

```powershell
# WindowsApp/TestAllServers.ps1

Write-Host "`n=== Server Cleanup Verification ===" -ForegroundColor Cyan

# Check Vite (Port 5000)
Write-Host "`n[1] Checking Vite Server (Port 5000)..." -ForegroundColor Yellow
$vitePort = netstat -ano | Select-String ":5000 "
if ($vitePort) {
    Write-Host "? Port 5000 is still in use" -ForegroundColor Red
    $vitePort
} else {
    Write-Host "? Port 5000 is free" -ForegroundColor Green
}

# Check OAuth Proxy (Port 3001)
Write-Host "`n[2] Checking OAuth Proxy Server (Port 3001)..." -ForegroundColor Yellow
$oauthPort = netstat -ano | Select-String ":3001 "
if ($oauthPort) {
    Write-Host "? Port 3001 is still in use" -ForegroundColor Red
    $oauthPort
} else {
    Write-Host "? Port 3001 is free" -ForegroundColor Green
}

# Check Storage Server (Port 5001)
Write-Host "`n[3] Checking Storage Server (Port 5001)..." -ForegroundColor Yellow
$storagePort = netstat -ano | Select-String ":5001 "
if ($storagePort) {
    Write-Host "? Port 5001 is still in use" -ForegroundColor Red
    $storagePort
} else {
    Write-Host "? Port 5001 is free" -ForegroundColor Green
}

# Check for node.exe processes
$nodeProcesses = Get-Process node -ErrorAction SilentlyContinue
if ($nodeProcesses) {
    Write-Host "`n[4] Checking node.exe processes..." -ForegroundColor Yellow
    $found = $false
    foreach ($proc in $nodeProcesses) {
        try {
            $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
            if ($cmdLine -match "vite|server\.js|oauth") {
                Write-Host "? Found server process: PID $($proc.Id)" -ForegroundColor Red
                Write-Host "   $cmdLine" -ForegroundColor Gray
                $found = $true
            }
        } catch { }
    }
    if (-not $found) {
        Write-Host "? No Vite or OAuth proxy processes" -ForegroundColor Green
    }
} else {
    Write-Host "`n[4] ? No node.exe processes running" -ForegroundColor Green
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if (-not $vitePort -and -not $oauthPort -and -not $storagePort) {
    Write-Host "? ALL SERVERS PROPERLY CLEANED UP!" -ForegroundColor Green
} else {
    Write-Host "? Some cleanup issues detected" -ForegroundColor Red
}
```

### Manual Port Release

If servers are still running after testing:

```powershell
# Kill all Node processes (Vite + OAuth proxy)
taskkill /F /IM node.exe

# Kill PromptArq if hung
taskkill /F /IM PromptArq.exe

# Or by specific ports
$ports = @(5000, 3001, 5001)
foreach ($port in $ports) {
    $pid = (netstat -ano | findstr :$port | ForEach-Object { $_.Split(' ')[-1] } | Select-Object -First 1)
    if ($pid) { 
        Write-Host "Killing process on port $port (PID: $pid)"
        taskkill /F /PID $pid 
    }
}
```

---

## Debug Output

When stopping the app, you should see:

```
[Program] ApplicationExit event triggered
[ViteManager] Stopping Vite process 12345
[ViteManager] taskkill completed with exit code 0
[ViteManager] Successfully stopped process 12345
[ViteManager] No orphaned processes found
[StorageManager] Stopping storage server
[StorageManager] Storage server stopped successfully
```

---

## Files Modified

| File | Purpose | Changes |
|------|---------|---------|
| `ViteProcessManager.cs` | Vite & OAuth proxy lifecycle | NEW - Manages process tree |
| `OAuthProxyManager.cs` | OAuth proxy (optional) | NEW - Available for explicit management |
| `StorageServerManager.cs` | Storage lifecycle | NEW - Manages storage server |
| `Program.cs` | Exit handlers | Calls ViteManager + StorageManager |
| `MainForm.cs` | Process registration | Registers servers with managers |

---

## Port Usage Summary

| Port | Service | Protocol | Manager | Process Tree |
|------|---------|----------|---------|--------------|
| 5000 | Vite Dev Server | HTTP | ViteProcessManager | npm ? concurrently ? vite |
| 3001 | OAuth Proxy | HTTP | ViteProcessManager* | npm ? concurrently ? server.js |
| 5001 | Storage HTTP API | HTTP | StorageServerManager | PromptArq.exe ? HttpListener |

*OAuth proxy is part of the Vite process tree and is terminated when Vite is killed.

---

## Shutdown Scenarios Covered

? **Normal Exit** (X button)
- FormClosing ? managers called ? all servers stop

? **Debug Stop** (Shift+F5)
- ProcessExit ? managers called ? all servers stop

? **Tray Exit** (Right-click ? Exit)
- Application.Exit ? managers called ? all servers stop

? **Unhandled Exception**
- UnhandledException ? managers called ? all servers stop

? **Force Close** (Task Manager)
- try/finally ? managers called ? all servers stop

---

## Common Issues & Solutions

### Issue: "Port 5000 already in use"
**Cause**: Vite process not terminated
**Solution**: Run `taskkill /F /IM node.exe` or restart machine

### Issue: "Port 3001 already in use"
**Cause**: OAuth proxy not terminated (part of npm run dev)
**Solution**: Same as above - kill node.exe processes

### Issue: "Port 5001 already in use"
**Cause**: HttpListener still bound
**Solution**: Kill PromptArq process or restart Visual Studio

### Issue: Multiple node.exe processes
**Cause**: Multiple debug sessions without cleanup
**Solution**: Use ViteProcessManager (now implemented)

---

## Best Practices

1. **Always use managers** - Never call Stop/Dispose directly in MainForm
2. **Check debug output** - Verify `[ViteManager]` and `[StorageManager]` logs
3. **Test all exit paths** - Don't just test normal close
4. **Monitor all ports** - Use netstat to verify cleanup
5. **Run test script** - Use `TestAllServers.ps1` after each test

---

## Related Documentation

- `VITE_SHUTDOWN_FIX.md` - Detailed Vite cleanup implementation
- `QUICK_REFERENCE.md` - Quick commands and troubleshooting
- `TestViteCleanup.ps1` - Vite-specific test script
- `TestAllServers.ps1` - Comprehensive server test (includes OAuth check)

---

## Future Considerations

### Potential Enhancements:
- **Explicit OAuth proxy management**: Use `OAuthProxyManager` if needed
- **Process monitoring**: Detect if server crashes and restart
- **Port conflict detection**: Check if ports are available before start
- **Graceful shutdown timeout**: Add configurable timeout before force kill
- **Server health checks**: Periodic HTTP health checks
- **Automatic port selection**: Use alternative ports if default is busy

### Known Limitations:
- **No Windows Service mode**: Servers stop when app closes (by design)
- **No distributed deployment**: Servers are local to the app instance
- **No SSL/TLS**: All servers use plain HTTP (localhost only)
- **OAuth proxy part of Vite tree**: Terminated with Vite, no separate management

---

**Status**: ? COMPLETE
**Build**: ? SUCCESSFUL  
**Test Coverage**: Vite + OAuth proxy + Storage cleanup verified
**Date**: 2024
