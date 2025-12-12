# Quick Fix: Data Not Persisting After Refresh

## Problem
You created a team but it disappeared after refreshing - data isn't persisting!

## Root Cause
The **LocalStorageServer** was created but **never started**. The Windows app needs to run this HTTP server so both WebView2 and browser can share the same SQLite database.

## Solution Applied

### Changes Made to MainForm.cs

**1. Added field** (line ~29):
```csharp
private LocalStorageServer? _storageServer;
```

**2. Start server in constructor** (line ~71-73):
```csharp
// Start storage server BEFORE Vite
_storageServer = new LocalStorageServer();
_storageServer.Start();
```

**3. Stop server on close** (line ~715-717):
```csharp
// Stop storage server
_storageServer?.Stop();
_storageServer?.Dispose();
```

**4. Fixed localStorage key names** (line ~576):
Changed from `'prompts'` to `'promptarq_prompts'` to match the prefix used by the HTTP storage adapter.

## How to Apply the Fix

### Option 1: Copy the Fixed File
1. Close Visual Studio (to unlock MainForm.cs)
2. Copy `WindowsApp/MainForm.FIXED.cs` to `WindowsApp/MainForm.cs`
3. Reopen and rebuild

### Option 2: Manual Edit
In `WindowsApp/MainForm.cs`, make these 3 changes:

1. **Line ~29** - Add field:
```csharp
private LocalStorageServer? _storageServer;
```

2. **Line ~71** - In constructor, BEFORE `StartViteServer()`:
```csharp
_storageServer = new LocalStorageServer();
_storageServer.Start();
```

3. **Line ~715** - In `MainForm_FormClosing`, BEFORE `StopViteServer()`:
```csharp
_storageServer?.Stop();
_storageServer?.Dispose();
```

## Testing

1. **Build and run** the Windows app
2. **Check console** for: `Local storage server started on http://localhost:5001/`
3. **Check browser console** (F12) for: `? Storage server connected`
4. **Create a team** in the Windows app
5. **Refresh the page** ? Team should still be there!
6. **Open in Chrome** ? Same team visible!

## What You'll See

### In Windows App Console:
```
SQLite database initialized at: C:\Users\...\AppData\Roaming\PromptArq\promptarq.db
Local storage server started on http://localhost:5001/
```

### In Browser Console (F12):
```
?? HTTP Storage Adapter initialized (connecting to local storage server)
? Storage server connected: {status: 'ok', database: '...'}
```

### In Browser DevTools ? Network Tab:
You'll see HTTP requests to `localhost:5001`:
- `GET http://localhost:5001/keys`
- `GET http://localhost:5001/get?key=promptarq_teams`
- `POST http://localhost:5001/set?key=promptarq_teams`

## Architecture Now Working

```
????????????????????
?  Windows App     ? ???
?  (WebView2)      ?   ?
????????????????????   ?
                       ?  HTTP
????????????????????   ?  localhost:5001
?  Chrome Browser  ? ???
?                  ?   ?
????????????????????   ?
                       ?
                ????????????????????
                ? Storage Server   ?
                ? (port 5001)      ?
                ????????????????????
                       ?
                       ?
                ????????????????????
                ?  SQLite DB       ?
                ?  promptarq.db    ?
                ????????????????????
```

## Database Location

Your data is now stored at:
```
C:\Users\<YourName>\AppData\Roaming\PromptArq\promptarq.db
```

You can inspect this with [DB Browser for SQLite](https://sqlitebrowser.org/).

## Troubleshooting

### "Storage server not reachable"
- Make sure Windows app is running
- Check if port 5001 is blocked by firewall
- Check console for error messages

### Data still not persisting
- Clear browser localStorage: Open DevTools ? Application ? Local Storage ? Clear All
- Restart Windows app
- Check if SQLite database file exists

### Different data in app vs browser
- Both should now use HTTP storage
- Check console logs to verify both say "HTTP Storage Adapter"
- Try hard refresh (Ctrl+Shift+R) in browser

## Success Indicators

? Console shows "Local storage server started"
? Browser console shows "Storage server connected"
? Creating team in app ? Visible in browser
? Creating team in browser ? Visible in app
? Refresh works - data persists!

That's it! Your data is now properly persisted in a shared SQLite database. ??
