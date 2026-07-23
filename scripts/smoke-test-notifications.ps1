$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ExePath = "$RepoRoot\artifacts\AjazzBatteryMonitor-win-x64-v1.1.0.exe"

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at $ExePath. Run publish.ps1 first."
    exit 1
}

Write-Output "[+] Running Notification Engine Smoke Test on published binary: $ExePath"

# Stop any running process instances
Stop-Process -Name "AjazzBatteryMonitor*" -Force -ErrorAction SilentlyContinue

$Process = Start-Process -FilePath $ExePath -ArgumentList "--smoke-test-notification", "--allow-multiple-instances" -PassThru

Start-Sleep -Seconds 4

if ($Process.HasExited) {
    Write-Error "[-] Notification Smoke Test FAILED! Process exited prematurely with ExitCode: $($Process.ExitCode)"
    exit 1
}

Write-Output "[+] Notification Smoke Test Process executed cleanly (PID: $($Process.Id))."

Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue

Write-Output "[+] Notification Smoke Test PASSED SUCCESSFULLY!"
exit 0
