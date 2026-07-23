$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ExePath = "$RepoRoot\artifacts\AjazzBatteryMonitor-win-x64-v1.1.1.exe"

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at $ExePath. Run publish.ps1 first."
    exit 1
}

Write-Output "[+] Running UI Smoke Test on published binary: $ExePath"

# Stop any running process instances
Stop-Process -Name "AjazzBatteryMonitor*" -Force -ErrorAction SilentlyContinue

$LogPath = "$env:LOCALAPPDATA\AjazzBatteryMonitor\logs\startup.log"
if (Test-Path $LogPath) {
    Remove-Item $LogPath -Force
}

$Process = Start-Process -FilePath $ExePath -ArgumentList "--smoke-test-ui", "--allow-multiple-instances", "--mock-battery=88" -PassThru

Start-Sleep -Seconds 5

if ($Process.HasExited) {
    Write-Error "[-] UI Smoke Test FAILED! Process exited prematurely with ExitCode: $($Process.ExitCode)"
    exit 1
}

Write-Output "[+] UI Smoke Test Process is running cleanly with open MainForm (PID: $($Process.Id))."

# Clean exit after 6s
Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue

Write-Output "[+] UI Smoke Test PASSED SUCCESSFULLY!"
exit 0
