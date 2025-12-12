# Solution: Shared Storage Between WebView2 and Browser

## Problem
- **Windows App (WebView2)** and **Browser** both use `localStorage`
- But they have **separate storage locations** (security isolation)
- Data created in Windows app is NOT visible in browser and vice versa

## Root Cause
The storage-adapter already had SQLite support, but it only activates in **Node.js environments**.  
WebView2 and browsers are **JavaScript environments**, so they fall back to localStorage.

## Solution: Local HTTP Storage Server

Create a local HTTP server (running in the Windows app) that:
1. **Runs on `localhost:5001`** (different from Vite's 5000)
2. **Uses SQLite database** as backend
3. **Provides REST API** for storage operations
4. **Both WebView2 AND browser** connect to it via HTTP

### Architecture

```
???????????????????????      HTTP        ????????????????????????
?  Windows App        ?  ???????????????? ?  Storage Server      ?
?  (WebView2)         ?   localhost:5001  ?  (SQLite Backend)    ?
???????????????????????                   ????????????????????????
                                                    ?
???????????????????????      HTTP                  ?
?  Chrome Browser     ?  ??????????????????????????
?                     ?   localhost:5001
???????????????????????
```

### Files Created

1. **`WindowsApp/LocalStorageServer.cs`** - HTTP server with SQLite backend
2. **`src/lib/http-storage-adapter.ts`** - JavaScript client for HTTP storage
3. Updated **`src/lib/storage-adapter.ts`** - Use HTTP storage on localhost

### Integration Steps

#### 1. Add SQLite Package
Already done - added to `PromptArqApp.csproj`:
```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
```

#### 2. Update MainForm.cs

Add these changes:

```csharp
// At the top with other fields:
private LocalStorageServer? _storageServer;

// In constructor, BEFORE StartViteServer():
_storageServer = new LocalStorageServer();
_storageServer.Start();

// In MainForm_FormClosing, BEFORE StopViteServer():
_storageServer?.Stop();
_storageServer?.Dispose();
```

#### 3. Build and Run

```bash
# Restore packages
dotnet restore WindowsApp/PromptArqApp.csproj

# Build
dotnet build WindowsApp/PromptArqApp.csproj

# Run
dotnet run --project WindowsApp/PromptArqApp.csproj
```

### How It Works

1. **Windows app starts** ? Launches storage server on port 5001
2. **Vite app loads** (in WebView2 or browser) ? Detects `localhost` URL
3. **Storage adapter** ? Uses `HttpStorageAdapter` to connect to port 5001
4. **All storage operations** ? Go through HTTP API ? Stored in SQLite
5. **Shared database** ? `%APPDATA%\PromptArq\promptarq.db`

### API Endpoints

The storage server provides:

- `GET /health` - Health check
- `GET /keys` - List all keys
- `GET /get?key=XXX` - Get value for key
- `POST /set?key=XXX` - Set value (body = JSON)
- `DELETE /delete?key=XXX` - Delete key

### Testing

1. **Start Windows app**
2. **Create a prompt** in the Windows app
3. **Open `http://localhost:5000` in Chrome**
4. **You should see the same prompt!**

### Database Location

```
Windows: C:\Users\<YourName>\AppData\Roaming\PromptArq\promptarq.db
```

You can inspect this with any SQLite tool (e.g., DB Browser for SQLite).

### Fallback Behavior

If the storage server isn't running:
- JavaScript logs: `?? Storage server not reachable`
- Falls back to localStorage (separate storage)
- No errors, but data won't be shared

### Security Note

The HTTP server:
- Only listens on `localhost` (not accessible from network)
- No authentication needed (local-only access)
- CORS enabled for local development
- Safe for local development use

### Benefits

? **Single source of truth** - One SQLite database  
? **Works in both** - WebView2 and browser  
? **No data sync needed** - Always up-to-date  
? **Persistent** - Data survives app restarts  
? **Fast** - Local SQLite is very fast  
? **Simple** - Just HTTP REST API  

### Troubleshooting

**Problem**: Browser shows "no prompts"
- Check if Windows app is running
- Check console for `? Storage server connected`
- Check if port 5001 is blocked by firewall

**Problem**: "Failed to get keys from storage server"
- Storage server might not be running
- Check Windows app console for errors
- Try restarting the Windows app

**Problem**: Different data in app vs browser
- Make sure both are using HTTP storage (check console logs)
- Clear browser localStorage: `localStorage.clear()`
- Restart Windows app

## Summary

The solution transforms the architecture from:
- ? **Before**: WebView2 localStorage ? Browser localStorage (isolated)
- ? **After**: Both ? HTTP Server ? SQLite (shared)

This gives you a single, shared database that works seamlessly across both the Windows app and any browser!
