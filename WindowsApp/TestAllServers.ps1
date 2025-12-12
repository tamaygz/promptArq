# Comprehensive server cleanup verification script
# Tests Vite (5000), OAuth proxy (3001), and Storage Server (5001)

Write-Host "`n=== Server Cleanup Verification ===" -ForegroundColor Cyan
Write-Host "Checking if all servers have been properly cleaned up..." -ForegroundColor Gray
Write-Host ""

$allGood = $true

# Check Vite Server (Port 5000)
Write-Host "[1] Checking Vite Development Server (Port 5000)..." -ForegroundColor Yellow

$vitePort = netstat -ano | Select-String ":5000 "
if ($vitePort) {
    Write-Host "  ? Port 5000 is still in use:" -ForegroundColor Red
    $vitePort | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
    $allGood = $false
} else {
    Write-Host "  ? Port 5000 is free" -ForegroundColor Green
}

# Check OAuth Proxy (Port 3001)
Write-Host "`n[2] Checking OAuth Proxy Server (Port 3001)..." -ForegroundColor Yellow

$oauthPort = netstat -ano | Select-String ":3001 "
if ($oauthPort) {
    Write-Host "  ? Port 3001 is still in use:" -ForegroundColor Red
    $oauthPort | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
    $allGood = $false
} else {
    Write-Host "  ? Port 3001 is free" -ForegroundColor Green
}

# Check for node.exe processes running Vite or server.js
$nodeProcesses = Get-Process node -ErrorAction SilentlyContinue
if ($nodeProcesses) {
    $serverProcessFound = $false
    Write-Host "  Checking $($nodeProcesses.Count) node.exe process(es)..." -ForegroundColor Gray
    
    foreach ($proc in $nodeProcesses) {
        try {
            $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
            if ($cmdLine -match "vite|server\.js|npm run dev|oauth") {
                Write-Host "  ? Server process still running:" -ForegroundColor Red
                Write-Host "    PID: $($proc.Id)" -ForegroundColor White
                Write-Host "    Command: $cmdLine" -ForegroundColor Gray
                $serverProcessFound = $true
                $allGood = $false
            }
        } catch {
            # Ignore processes we can't access
        }
    }
    
    if (-not $serverProcessFound) {
        Write-Host "  ? No Vite or OAuth proxy processes in node.exe instances" -ForegroundColor Green
    }
} else {
    Write-Host "  ? No node.exe processes running" -ForegroundColor Green
}

# Check Storage Server (Port 5001)
Write-Host "`n[3] Checking LocalStorage HTTP Server (Port 5001)..." -ForegroundColor Yellow

$storagePort = netstat -ano | Select-String ":5001 "
if ($storagePort) {
    Write-Host "  ? Port 5001 is still in use:" -ForegroundColor Red
    $storagePort | ForEach-Object { Write-Host "    $_" -ForegroundColor White }
    $allGood = $false
} else {
    Write-Host "  ? Port 5001 is free" -ForegroundColor Green
}

# Check if PromptArq process is still running
Write-Host "`n[4] Checking PromptArq Process Status..." -ForegroundColor Yellow

$promptArqProcess = Get-Process PromptArq -ErrorAction SilentlyContinue
if ($promptArqProcess) {
    Write-Host "  ? PromptArq is still running:" -ForegroundColor Yellow
    Write-Host "    PID: $($promptArqProcess.Id)" -ForegroundColor White
    Write-Host "    This is normal if you haven't closed the app yet" -ForegroundColor Gray
} else {
    Write-Host "  ? PromptArq process has terminated" -ForegroundColor Green
}

# Database check
Write-Host "`n[5] Checking Storage Database..." -ForegroundColor Yellow

$dbPath = Join-Path $env:APPDATA "PromptArq\promptarq.db"
if (Test-Path $dbPath) {
    $dbSize = (Get-Item $dbPath).Length / 1KB
    Write-Host "  ? Database exists at:" -ForegroundColor Green
    Write-Host "    $dbPath" -ForegroundColor Gray
    Write-Host "    Size: $([math]::Round($dbSize, 2)) KB" -ForegroundColor Gray
} else {
    Write-Host "  ? Database not found (expected on first run)" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan

if ($allGood -and -not $promptArqProcess) {
    Write-Host "? ALL CHECKS PASSED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "All servers (Vite, OAuth proxy, Storage) have been properly cleaned up." -ForegroundColor Gray
    Write-Host "You can safely restart the application." -ForegroundColor Gray
} elseif ($allGood -and $promptArqProcess) {
    Write-Host "? SERVERS ARE CLEAN" -ForegroundColor Green
    Write-Host ""
    Write-Host "PromptArq is still running, which is expected if you haven't closed it." -ForegroundColor Gray
    Write-Host "Stop the app and run this script again to verify cleanup." -ForegroundColor Gray
} else {
    Write-Host "? CLEANUP ISSUES DETECTED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Some servers are still running. To manually clean up:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Kill all node.exe (Vite + OAuth proxy):" -ForegroundColor White
    Write-Host "  taskkill /F /IM node.exe" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Kill PromptArq (if hung):" -ForegroundColor White
    Write-Host "  taskkill /F /IM PromptArq.exe" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or kill by specific ports:" -ForegroundColor White
    Write-Host '  $ports = @(5000, 3001, 5001)' -ForegroundColor Gray
    Write-Host '  foreach ($port in $ports) {' -ForegroundColor Gray
    Write-Host '    $pid = (netstat -ano | findstr :$port | ForEach-Object { $_.Split('' '')[-1] } | Select-Object -First 1)' -ForegroundColor Gray
    Write-Host '    if ($pid) { taskkill /F /PID $pid }' -ForegroundColor Gray
    Write-Host '  }' -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or restart Visual Studio/your computer for a clean slate." -ForegroundColor Gray
}

Write-Host ""

# Return exit code for CI/CD
if ($allGood -and -not $promptArqProcess) {
    exit 0
} else {
    exit 1
}
