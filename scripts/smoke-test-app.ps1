$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ExePath = "$RepoRoot\artifacts\AjazzBatteryMonitor-win-x64-v1.0.1.exe"

if (-not (Test-Path $ExePath)) {
    # Fallback check
    $ExePath = "$RepoRoot\artifacts\AjazzBatteryMonitor-win-x64.exe"
}

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at $ExePath. Run publish.ps1 first."
    exit 1
}

Write-Output "[+] Running smoke test on published binary: $ExePath"

# Remove old log if present to verify fresh creation
$LogPath = "$env:LOCALAPPDATA\AjazzBatteryMonitor\logs\startup.log"
if (Test-Path $LogPath) {
    Remove-Item -Force $LogPath -ErrorAction SilentlyContinue
}

$Process = Start-Process -FilePath $ExePath -ArgumentList "--smoke-test" -PassThru

Start-Sleep -Seconds 5
$Process.Refresh()

if ($Process.HasExited) {
    Write-Error "[-] Smoke Test FAILED: Process exited unexpectedly early with code $($Process.ExitCode)."
    exit 1
}

Write-Output "[+] Process is running cleanly after 5 seconds (PID: $($Process.Id))."

# Wait up to 10 more seconds for smoke test auto-exit
$Process.WaitForExit(10000) | Out-Null

if (Test-Path $LogPath) {
    $LogContent = Get-Content $LogPath -Raw
    Write-Output "--- Startup Log Output ---"
    Write-Output $LogContent
    Write-Output "--------------------------"

    if (-not ($LogContent -match "Tray icon made visible")) {
        Write-Error "[-] Smoke Test FAILED: Log does not contain 'Tray icon made visible'."
        exit 1
    }

    if (-not ($LogContent -match "Application startup completed")) {
        Write-Error "[-] Smoke Test FAILED: Log does not contain 'Application startup completed'."
        exit 1
    }
} else {
    Write-Error "[-] Smoke Test FAILED: Startup log was not created at $LogPath."
    exit 1
}

Write-Output "[+] Smoke Test PASSED SUCCESSFULLY!"
exit 0
