# Fix: Command Palette Shows Old Data

## Problem
Command Palette shows old data that was created before the storage server was integrated. This data is invisible in the current app.

## Root Cause
The Command Palette **queries localStorage directly** using JavaScript, but the React app now uses the **HTTP Storage Server** (SQLite database). 

**Data Location Mismatch:**
- **Old Data**: WebView2's localStorage (`promptarq_prompts`)
- **New Data**: SQLite database via HTTP server
- **Command Palette**: Queries localStorage (sees old data)
- **React App**: Uses HTTP storage adapter (sees new data)

## Architecture Problem

```
???????????????????????????
? Command Palette (C#)    ?
? GetPromptsFromWebApp()  ?
???????????????????????????
           ?
           ? ExecuteScriptAsync
    localStorage.getItem()  ? OLD DATA (before storage server)
           
           
???????????????????????????
? React App (TypeScript)  ?
? useStorage hook         ?
???????????????????????????
           ?
           ?
    HTTP Storage Adapter ? localhost:5001 ? SQLite  ? NEW DATA
```

## Solution
Make the Command Palette fetch from the **HTTP Storage Server** directly, just like the React app does.

### Implementation

Replace the `GetPromptsFromWebApp()` method in `WindowsApp/MainForm.cs`:

1. **Primary**: Fetch from HTTP storage server (`localhost:5001`)
2. **Fallback**: Query localStorage (for backwards compatibility)

This ensures Command Palette sees the SAME data as the React app.

### Files to Update

#### 1. Add new method to `WindowsApp/MainForm.cs`

See `WindowsApp/FIX_COMMAND_PALETTE_STORAGE.cs.txt` for:
- `FetchPromptsFromStorageServer()` - Fetches from HTTP server
- Updated `GetPromptsFromWebApp()` - Uses HTTP first, localStorage fallback

#### 2. Location in MainForm.cs

Add after line ~560 (after the existing `GetPromptsFromWebApp()` method).

### How It Works

**New Flow:**
```csharp
ShowCommandPalette()
  ? GetPromptsFromWebApp()
    ? Try: FetchPromptsFromStorageServer()  // HTTP GET to localhost:5001
      ? Success ? Return data from SQLite
      ? Fail ? Fall back to localStorage query
```

**HTTP Requests Made:**
```
GET http://localhost:5001/get?key=promptarq_prompts
GET http://localhost:5001/get?key=promptarq_projects
GET http://localhost:5001/get?key=promptarq_categories
GET http://localhost:5001/get?key=promptarq_tags
```

### Benefits

? Command Palette sees SAME data as React app
? No more "ghost" old data
? Backwards compatible (falls back to localStorage)
? Fast (direct HTTP GET, no JavaScript execution)

### Testing

1. **Build and run** the updated app
2. **Create a new team** in the React app
3. **Press Ctrl+K** to open Command Palette
4. **Should see**:
   - New team data
   - NO old data from before storage server

### Verify in Logs

Check Debug Output for:
```
Fetched X prompts from storage server
```

If you see this, it's working! If you see:
```
Failed to fetch from storage server, falling back to localStorage
```

Then the storage server isn't running or port 5001 is blocked.

### Optional: Clean Up Old Data

To remove the old localStorage data completely:

1. Open DevTools (F12) in the Windows app
2. Go to **Application** ? **Local Storage** ? `http://localhost:5000`
3. Delete keys starting with `promptarq_`
4. Refresh

Now ONLY the SQLite database will have data!

### Summary

| Before | After |
|--------|-------|
| Command Palette ? localStorage (old data) | Command Palette ? HTTP Server ? SQLite (current data) |
| React App ? HTTP Server ? SQLite (current data) | React App ? HTTP Server ? SQLite (current data) |
| ? Data mismatch! | ? Single source of truth! |

This makes the Command Palette consistent with the rest of the application.
