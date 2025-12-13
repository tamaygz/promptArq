# Server Management Refactoring

## Problem Analysis

### Root Cause
The Windows application was experiencing persistent server processes (Vite, OAuth Proxy, LocalStorage) that continued running after the application terminated. Through deep analysis, we identified several critical issues:

1. **Dual Ownership Problem**: MainForm maintained local references (`_viteProcess`, `_storageServer`) while also registering with static manager classes (`ViteProcessManager`, `OAuthProxyManager`, `StorageServerManager`). This created confusion about cleanup ownership.

2. **Complex Process Tree**: The `npm run dev` command spawned a deep process tree:
   ```
   cmd.exe → npm → node → concurrently → [node (vite), node (server.js)]
   ```
   Even with `Kill(entireProcessTree: true)`, Windows didn't reliably terminate all descendant processes across multiple shell layers.

3. **Missing OAuth Proxy Tracking**: The OAuth proxy server (server.js) was started as a child of the Vite process via `concurrently`, but was never registered with `OAuthProxyManager`. The manager existed but was never used.

4. **No Central Coordination**: Three separate manager classes operated independently with no coordination, leading to potential race conditions and incomplete cleanup.

5. **Multiple Cleanup Handlers**: Cleanup was attempted from multiple places (FormClosing, Dispose, Program.cs exit handlers) without proper synchronization.

## Solution Architecture

### UnifiedServerManager

Created a single, centralized `UnifiedServerManager` class that:

- **Single Source of Truth**: Manages ALL servers (Vite + OAuth Proxy via npm, LocalStorage in-process)
- **Thread-Safe**: Uses locks to prevent race conditions during startup/shutdown
- **Idempotent**: Safe to call `Start()` and `Stop()` multiple times
- **Stateful**: Tracks whether servers are running with `IsRunning` property
- **Comprehensive Cleanup**: Uses five progressive cleanup strategies

### Five-Layer Cleanup Strategy

The `UnifiedServerManager.Stop()` method employs a belt-and-suspenders approach:

#### 1. Graceful Shutdown
- Stops LocalStorageServer by calling its `Stop()` method
- Attempts to close Vite dev process gracefully
- Cancels output reading and waits for natural exit

#### 2. Process Tree Kill
- Uses `taskkill /F /T /PID` to forcefully terminate process trees
- Kills the main npm/cmd process and all its children
- Reads output for debugging

#### 3. Command-Line Detection
- Enumerates all `node.exe` processes
- Uses WMI to read command lines
- Kills any process containing "vite", "server.js", "npm run dev", or "concurrently"
- Catches orphaned processes that escaped tree kill

#### 4. Port-Based Kill
- Nuclear option: finds processes listening on ports 5000, 3001, 5001
- Uses `netstat -ano` to map ports to PIDs
- Kills any process holding these ports

#### 5. Verification
- Waits 500ms for OS to release ports
- Verifies all managed ports are free
- Logs warnings if ports remain in use

## Implementation Changes

### Files Created
- `UnifiedServerManager.cs` - New centralized server manager (550+ lines)

### Files Modified
- `MainForm.cs` - Removed local server management, now uses UnifiedServerManager
- `Program.cs` - Simplified to only call UnifiedServerManager.Stop()
- `MainForm.Designer.cs` - Updated Dispose to use UnifiedServerManager

### Files Deleted
- `ViteProcessManager.cs` - Obsolete, functionality merged into UnifiedServerManager
- `OAuthProxyManager.cs` - Obsolete, functionality merged into UnifiedServerManager
- `StorageServerManager.cs` - Obsolete, functionality merged into UnifiedServerManager

## Code Flow

### Startup Sequence
```
1. MainForm constructor
   └─> UnifiedServerManager.Start()
       ├─> StartStorageServer() - Port 5001
       └─> StartViteDevServer() - Starts "npm run dev"
           ├─> Vite on port 5000
           └─> OAuth Proxy on port 3001 (via concurrently)

2. MainForm.MonitorViteStartup()
   └─> Polls http://localhost:5000 until ready
       └─> Sets _isViteReady = true
```

### Shutdown Sequence
```
Multiple entry points → UnifiedServerManager.Stop() (thread-safe)
├─> Strategy 1: Graceful shutdown
├─> Strategy 2: Force kill process trees
├─> Strategy 3: Kill by command line
├─> Strategy 4: Kill by port
└─> Strategy 5: Verify cleanup

Entry points:
- MainForm_FormClosing
- MainForm.Dispose (Designer.cs)
- Program.OnApplicationExit
- Program.OnProcessExit
- Program.OnUnhandledException
- Program.Main finally block
```

## Key Design Patterns

### Singleton Pattern
`UnifiedServerManager` uses static methods and state, ensuring only one instance manages all servers.

### Idempotent Operations
All methods can be called multiple times safely:
- `Start()` - Only starts if not already started
- `Stop()` - Only stops if started, handles already-stopped state

### Defense in Depth
Multiple cleanup strategies ensure no process escapes:
1. Try graceful (polite)
2. Force kill tree (forceful)
3. Kill by command line (targeted)
4. Kill by port (nuclear)
5. Verify (paranoid)

### Thread Safety
Critical sections protected by locks to prevent race conditions during concurrent shutdown attempts.

## Testing Recommendations

Test these scenarios to verify proper cleanup:

1. **Normal Exit**: Close application normally
   - ✓ All servers should stop
   - ✓ Ports 5000, 3001, 5001 should be released

2. **Alt+F4**: Force close window
   - ✓ FormClosing should trigger cleanup

3. **Task Manager Kill**: Kill PromptArq.exe from Task Manager
   - ✓ ProcessExit handler should cleanup

4. **Debugger Stop**: Stop debugging in Visual Studio
   - ✓ Finally block should execute cleanup

5. **Unhandled Exception**: Cause an exception
   - ✓ UnhandledException handler should cleanup

6. **Multiple Stops**: Call Stop() multiple times rapidly
   - ✓ Should be idempotent, no errors

## Verification Commands

Check if servers are still running:

```powershell
# Check Vite (port 5000)
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue

# Check OAuth Proxy (port 3001)
Get-NetTCPConnection -LocalPort 3001 -ErrorAction SilentlyContinue

# Check LocalStorage (port 5001)
Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue

# Check all node.exe processes
Get-Process node -ErrorAction SilentlyContinue | Format-Table Id, ProcessName, StartTime
```

Force cleanup if needed:

```powershell
# Kill all node processes (nuclear option)
Get-Process node -ErrorAction SilentlyContinue | Stop-Process -Force

# Kill by port
$pids = (Get-NetTCPConnection -LocalPort 5000,3001,5001 -ErrorAction SilentlyContinue).OwningProcess | Select-Object -Unique
if ($pids) { Stop-Process -Id $pids -Force }
```

## Benefits

1. **Reliability**: Five-layer cleanup ensures processes don't escape
2. **Maintainability**: Single class to manage, not three
3. **Debuggability**: Comprehensive logging at each stage
4. **Thread Safety**: No race conditions during shutdown
5. **Simplicity**: Clear ownership - UnifiedServerManager owns everything
6. **Robustness**: Handles edge cases, debugger stops, crashes

## Future Improvements

1. **Port Availability Check**: Before starting, verify ports are free
2. **Restart Capability**: Add `Restart()` method for development
3. **Health Checks**: Periodic ping to verify servers are responsive
4. **Better Error Reporting**: Surface startup failures to UI
5. **Configurable Timeouts**: Make wait times configurable
6. **Event Notifications**: Raise events when servers start/stop

## Conclusion

This refactoring solves the persistent server process problem through comprehensive, defense-in-depth cleanup strategies and centralized lifecycle management. The `UnifiedServerManager` provides a single, reliable point of control for all server components, ensuring clean startup and shutdown regardless of how the application exits.
