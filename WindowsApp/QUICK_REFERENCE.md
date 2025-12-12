# Quick Reference: Server Process Management

## What Was Fixed?
Both the **Vite development server** (port 5000) and **LocalStorage HTTP server** (port 5001) now properly shut down when you stop debugging in Visual Studio (Shift+F5), preventing orphaned processes.

## Managed Servers

| Server | Port | Process | Manager |
|--------|------|---------|---------|
| Vite Dev Server | 5000 | node.exe | ViteProcessManager |
| Storage HTTP API | 5001 | PromptArq.exe | StorageServerManager |

## How to Test the Fix

### Before Testing:
1. Build the solution (Ctrl+Shift+B)
2. Kill any existing processes:
   ```powershell
   taskkill /F /IM node.exe
   taskkill /F /IM PromptArq.exe
   ```

### Test Procedure:
1. **Start the app** (F5)
2. Wait for the app to fully load
3. **Stop the debugger** (Shift+F5)
4. **Run verification script**:
   ```powershell
   cd WindowsApp
   .\TestAllServers.ps1
   ```

### Expected Result:
```
? Port 5000 is free
? Port 5001 is free
? No Vite processes found
? PromptArq process has terminated
? ALL CHECKS PASSED!
```

## Files Changed

- ? `ViteProcessManager.cs` - NEW: Vite process lifecycle manager
- ? `StorageServerManager.cs` - NEW: Storage server lifecycle manager
- ? `Program.cs` - Enhanced with exit handlers for both servers
- ? `MainForm.cs` - Integrated with both managers
- ? `TestAllServers.ps1` - NEW: Comprehensive test script
- ? `SERVER_MANAGEMENT.md` - NEW: Detailed documentation

## Key Features

### Vite Server Management
1. **Process tree termination**: Kills npm?node?vite chain
2. **Orphan detection**: Finds leaked node.exe processes
3. **Multiple kill strategies**: taskkill, Process.Kill, WMI scan

### Storage Server Management
1. **Clean HttpListener shutdown**: Stops listening on port 5001
2. **Resource disposal**: Properly releases all resources
3. **Thread-safe cleanup**: Can be called from any thread

## Debug Output to Look For

When stopping the app, check the Output window for:
```
[Program] ApplicationExit event triggered
[ViteManager] Stopping Vite process 12345
[ViteManager] Successfully stopped process 12345
[StorageManager] Stopping storage server
[StorageManager] Storage server stopped successfully
```

## Troubleshooting

### If Vite (port 5000) still running:

1. **Check Output window** for ViteManager logs
2. **Manually kill**:
   ```powershell
   taskkill /F /IM node.exe
   ```
3. **Check port**:
   ```powershell
   netstat -ano | findstr :5000
   ```

### If Storage (port 5001) still in use:

1. **Check Output window** for StorageManager logs
2. **Find process using port**:
   ```powershell
   netstat -ano | findstr :5001
   ```
3. **Kill by PID** (replace PID with actual number):
   ```powershell
   taskkill /F /PID <PID>
   ```

## Quick Commands

**Kill all node processes** (nuclear option for Vite):
```powershell
taskkill /F /IM node.exe
```

**Kill PromptArq** (if hung):
```powershell
taskkill /F /IM PromptArq.exe
```

**Check both ports**:
```powershell
netstat -ano | findstr "5000 5001"
```

**Free both ports** (automated):
```powershell
$ports = @(5000, 5001)
foreach ($port in $ports) {
    $pid = (netstat -ano | findstr :$port | ForEach-Object { $_.Split(' ')[-1] } | Select-Object -First 1)
    if ($pid) { 
        Write-Host "Killing process on port $port (PID: $pid)"
        taskkill /F /PID $pid 
    }
}
```

## Exit Scenarios Coverage

| Scenario | Handler | Both Servers Stop |
|----------|---------|-------------------|
| Click X button | FormClosing | ? |
| Shift+F5 (Debug Stop) | ProcessExit | ? |
| System tray Exit | Application.Exit | ? |
| Unhandled exception | UnhandledException | ? |
| Force close | finally block | ? |

## Success Indicators

? No orphaned node.exe processes
? Port 5000 becomes available immediately
? Port 5001 becomes available immediately
? No "port in use" errors on restart
? Debug output shows successful cleanup
? TestAllServers.ps1 passes all checks

## Port Usage

- **5000**: Vite development server (HTTP)
  - Serves React app with HMR
  - Proxies API requests
  - WebSocket for hot reload

- **5001**: LocalStorage HTTP server (HTTP)
  - REST API for shared storage
  - SQLite backend
  - CORS enabled for localhost

## Related Files

- `SERVER_MANAGEMENT.md` - Comprehensive documentation
- `VITE_SHUTDOWN_FIX.md` - Vite-specific details
- `TestAllServers.ps1` - Full test suite
- `TestViteCleanup.ps1` - Vite-only test

---

**Need Help?**
- See `SERVER_MANAGEMENT.md` for detailed explanation
- Check Output window (Debug ? Windows ? Output) for logs
- Run `TestAllServers.ps1` to verify current state
- Check both ports: `netstat -ano | findstr "5000 5001"`
