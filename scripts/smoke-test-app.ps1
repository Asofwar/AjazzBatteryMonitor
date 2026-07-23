$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ExePath = "$RepoRoot\artifacts\AjazzBatteryMonitor-win-x64-v1.1.2.exe"

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at $ExePath. Run publish.ps1 first."
    exit 1
}

Write-Output "[+] Running smoke test on published binary: $ExePath"

# Stop any running process instances
Stop-Process -Name "AjazzBatteryMonitor*" -Force -ErrorAction SilentlyContinue

$LogPath = "$env:LOCALAPPDATA\AjazzBatteryMonitor\logs\startup.log"
if (Test-Path $LogPath) {
    Remove-Item $LogPath -Force
}

$Process = Start-Process -FilePath $ExePath -ArgumentList "--smoke-test", "--allow-multiple-instances" -PassThru

Start-Sleep -Seconds 5

if ($Process.HasExited) {
    Write-Error "[-] Smoke Test FAILED! Process exited prematurely within 5 seconds with ExitCode: $($Process.ExitCode)"
    if (Test-Path $LogPath) {
        Write-Output "--- Startup Log Output ---"
        Get-Content $LogPath
    }
    exit 1
}

Write-Output "[+] Process is running cleanly after 5 seconds (PID: $($Process.Id))."

# Wait for smoke test auto-shutdown (10s total)
Start-Sleep -Seconds 7

if (Test-Path $LogPath) {
    Write-Output "--- Startup Log Output ---"
    Get-Content $LogPath
    Write-Output "--------------------------"
}

$LogContent = Get-Content $LogPath -Raw -ErrorAction SilentlyContinue
if ($LogContent -like "*Process started*" -and $LogContent -like "*Application startup completed*") {
    Write-Output "[+] Smoke Test PASSED SUCCESSFULLY!"
    exit 0
} else {
    Write-Error "[-] Smoke Test FAILED: Startup log missing required completion markers."
    exit 1
}
