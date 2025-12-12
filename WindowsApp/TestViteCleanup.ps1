# Test script to verify Vite process cleanup
# Run this AFTER stopping the PromptArq app to check if Vite is still running

Write-Host "`n=== Vite Process Cleanup Verification ===" -ForegroundColor Cyan
Write-Host "This script checks if the Vite server has been properly cleaned up.`n" -ForegroundColor Gray

# Check for node.exe processes
Write-Host "[1] Checking for node.exe processes..." -ForegroundColor Yellow
$nodeProcesses = Get-Process node -ErrorAction SilentlyContinue

if ($nodeProcesses) {
    Write-Host "Found $($nodeProcesses.Count) node.exe process(es):" -ForegroundColor White
    
    $viteProcessFound = $false
    foreach ($proc in $nodeProcesses) {
        try {
            $cmdLine = (Get-WmiObject Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
            
            if ($cmdLine -match "vite|npm run dev") {
                Write-Host "  ? PID $($proc.Id): $cmdLine" -ForegroundColor Red
                $viteProcessFound = $true
            } else {
                Write-Host "  ? PID $($proc.Id): Not a Vite process" -ForegroundColor Green
            }
        } catch {
            Write-Host "  ? PID $($proc.Id): Could not check command line" -ForegroundColor Gray
        }
    }
    
    if ($viteProcessFound) {
        Write-Host "`n? FAIL: Vite processes are still running!" -ForegroundColor Red
        Write-Host "The fix may not be working correctly." -ForegroundColor Red
    } else {
        Write-Host "`n? PASS: No Vite processes found" -ForegroundColor Green
    }
} else {
    Write-Host "? No node.exe processes found" -ForegroundColor Green
}

# Check if port 5000 is in use
Write-Host "`n[2] Checking if port 5000 is in use..." -ForegroundColor Yellow
$portCheck = netstat -ano | Select-String ":5000 "

if ($portCheck) {
    Write-Host "? Port 5000 is still in use:" -ForegroundColor Red
    $portCheck | ForEach-Object {
        Write-Host "  $_" -ForegroundColor White
    }
} else {
    Write-Host "? Port 5000 is free" -ForegroundColor Green
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if (-not $nodeProcesses -and -not $portCheck) {
    Write-Host "? All checks passed! Vite server cleanup is working correctly." -ForegroundColor Green
    Write-Host "The fix has been successfully applied." -ForegroundColor Green
} elseif ($viteProcessFound -or $portCheck) {
    Write-Host "? Some checks failed. There may be orphaned processes." -ForegroundColor Red
    Write-Host "`nTo manually clean up, run:" -ForegroundColor Yellow
    Write-Host "  taskkill /F /IM node.exe" -ForegroundColor White
} else {
    Write-Host "? Some node.exe processes exist, but they don't appear to be Vite." -ForegroundColor Yellow
    Write-Host "This is usually OK." -ForegroundColor Gray
}

Write-Host "`n" -ForegroundColor Gray
