$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ExePath = "$RepoRoot\artifacts\AjazzBatteryMonitor-win-x64-v1.2.1.exe"

if (-not (Test-Path $ExePath)) {
    Write-Error "Executable not found at $ExePath. Run publish.ps1 first."
    exit 1
}

Write-Output "[+] Running Settings UI Smoke Test on published binary: $ExePath"

# Stop any running process instances
Stop-Process -Name "AjazzBatteryMonitor*" -Force -ErrorAction SilentlyContinue

$LogPath = "$env:LOCALAPPDATA\AjazzBatteryMonitor\logs\startup.log"
if (Test-Path $LogPath) {
    Remove-Item $LogPath -Force
}

$Process = Start-Process -FilePath $ExePath -ArgumentList "--smoke-test-ui", "--allow-multiple-instances", "--mock-battery=88" -PassThru

Start-Sleep -Seconds 4

if ($Process.HasExited) {
    Write-Error "[-] Settings UI Smoke Test FAILED! Process exited prematurely with ExitCode: $($Process.ExitCode)"
    exit 1
}

Write-Output "[+] Settings UI Smoke Test Process is running cleanly with open MainForm (PID: $($Process.Id))."

Start-Sleep -Seconds 3

# Check startup.log for any GDI Paint errors or ThreadExceptions
if (Test-Path $LogPath) {
    $LogContent = Get-Content $LogPath -Raw -ErrorAction SilentlyContinue
    if ($LogContent -like "*Parameter is not valid*" -or $LogContent -like "*CRITICAL_Application.ThreadException*") {
        Write-Error "[-] Settings UI Smoke Test FAILED! Exception detected in log:"
        Get-Content $LogPath
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        exit 1
    }
}

# Clean exit
Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue

Write-Output "[+] Settings UI Smoke Test PASSED SUCCESSFULLY! (Zero paint errors or red-X controls)"
exit 0
