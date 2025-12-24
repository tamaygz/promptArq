# AGENTS.md
This file provides guidance to agents when working with code in this repository.
Runtime order: Vite (port 5000) → OAuth proxy (port 3001) → LocalStorage server (port 5001).
Windows app expects the web app to be available.
Storage adapter detection: see src/lib/storage-adapter.ts for full logic.
HTTP storage adapter connects to http://localhost:5001 (see src/lib/http-storage-adapter.ts).
Browser/WebView fallback and hook: src/hooks/use-storage.ts and WindowsApp/LocalStorageServer.cs.
HTTP adapter strips prefix 'promptarq_' when exchanging keys (src/lib/http-storage-adapter.ts).
LocalStorage adapter also uses prefix 'promptarq_' (src/lib/storage-adapter.ts).
SQLite adapter initializes promptarq.db and table kv_store (src/lib/storage-adapter.ts).
Windows SQLite DB location: %APPDATA%/PromptArq/promptarq.db (see WindowsApp/LocalStorageServer.cs).
Settings file location: %APPDATA%/PromptArq/settings.json (see WindowsApp/Settings.cs).
Windows logging and server lifecycle coordinated by WindowsApp/UnifiedServerManager.cs.
Windows build scripts: WindowsApp/Scripts/build.bat, WindowsApp/Scripts/build-publish.bat, WindowsApp/Scripts/run.bat.
Required build order: npm run build → copy web artifacts into WindowsApp/www → dotnet build.
Inspect package.json for test runner presence before adding tests.
Always use storage adapter APIs in src/lib/storage-adapter.ts instead of direct localStorage access.
Use helper methods in src/lib/windows-api.ts instead of calling window.chrome.webview directly.
Use the hook in src/hooks/use-storage.ts for persisted state across adapters.
Prefer HttpStorageAdapter for shared browser/WebView persistence during local dev (src/lib/http-storage-adapter.ts).