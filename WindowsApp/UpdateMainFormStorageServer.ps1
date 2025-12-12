# PowerShell script to update MainForm.cs with StorageServerManager integration
$filePath = "C:\Users\Tamay\vscode\promptArq\WindowsApp\MainForm.cs"
$content = Get-Content $filePath -Raw

# Change 1: Register storage server after _storageServer.Start()
$content = $content -replace `
    '(\s+_storageServer\.Start\(\);)',
    "`$1`r`n            StorageServerManager.RegisterServer(_storageServer);"

# Change 2: Update MainForm_FormClosing to use StorageServerManager
$oldClosing = @'
            // ? ADDED: Stop storage server
            _storageServer\?\.Stop\(\);
            _storageServer\?\.Dispose\(\);
'@

$newClosing = @'
            // Stop storage server via manager
            StorageServerManager.CleanupServer();
'@

$content = $content -replace $oldClosing, $newClosing

# Write back
Set-Content $filePath -Value $content -NoNewline

Write-Host "`nMainForm.cs has been updated successfully!" -ForegroundColor Green
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "  1. Added StorageServerManager.RegisterServer() call after server start" -ForegroundColor Yellow
Write-Host "  2. Updated FormClosing to use StorageServerManager.CleanupServer()" -ForegroundColor Yellow
