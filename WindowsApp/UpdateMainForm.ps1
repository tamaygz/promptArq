# PowerShell script to update MainForm.cs with ViteProcessManager integration
$filePath = "C:\Users\Tamay\vscode\promptArq\WindowsApp\MainForm.cs"
$content = Get-Content $filePath -Raw

# Change 1: Add ViteProcessManager.RegisterProcess after _viteProcess.Start()
$content = $content -replace `
    '(\s+_viteProcess\.Start\(\);)\s+(_viteProcess\.BeginOutputReadLine)',
    "`$1`r`n                    ViteProcessManager.RegisterProcess(_viteProcess);`r`n                    `$2"

# Change 2: Replace StopViteServer method
$oldStopMethod = @'
        private void StopViteServer\(\)
        \{
            if \(_viteProcess != null && !_viteProcess\.HasExited\)
            \{
                try
                \{
                    KillProcessAndChildren\(_viteProcess\.Id\);

                    if \(!_viteProcess\.WaitForExit\(3000\)\)
                    \{
                        _viteProcess\.Kill\(\);
                        _viteProcess\.WaitForExit\(2000\);
                    \}
                \}
                catch \(Exception ex\)
                \{
                    Debug\.WriteLine\(\$"Error stopping Vite: \{ex\.Message\}"\);
                \}
            \}
        \}
'@

$newStopMethod = @'
        private void StopViteServer()
        {
            Debug.WriteLine("[MainForm] Stopping Vite server via ViteProcessManager");
            ViteProcessManager.CleanupProcess();
            
            // Clean up local reference
            _viteProcess?.Dispose();
            _viteProcess = null;
        }
'@

$content = $content -replace $oldStopMethod, $newStopMethod

# Change 3: Remove KillProcessAndChildren method
$killMethodPattern = @'
        private void KillProcessAndChildren\(int pid\)[\s\S]*?\{[\s\S]*?\}\s+private void RegisterHotkeys
'@

$content = $content -replace $killMethodPattern, '        private void RegisterHotkeys'

# Write back
Set-Content $filePath -Value $content -NoNewline

Write-Host "MainForm.cs has been updated successfully!" -ForegroundColor Green
Write-Host "Changes made:" -ForegroundColor Cyan
Write-Host "  1. Added ViteProcessManager.RegisterProcess() call after _viteProcess.Start()" -ForegroundColor Yellow
Write-Host "  2. Replaced StopViteServer() to use ViteProcessManager" -ForegroundColor Yellow
Write-Host "  3. Removed obsolete KillProcessAndChildren() method" -ForegroundColor Yellow
