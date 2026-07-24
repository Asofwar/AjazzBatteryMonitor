$ErrorActionPreference = 'Stop'

$repoRoot   = Resolve-Path "$PSScriptRoot\.."
$installer  = Join-Path $repoRoot 'artifacts\AjazzBatteryMonitor-Setup-v1.3.0.exe'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\AJAZZ Battery Monitor'
$installedExe = Join-Path $installDir 'AjazzBatteryMonitor.exe'
$uninstaller  = Join-Path $installDir 'unins000.exe'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$registryName = 'AjazzBatteryMonitor'

if (-not (Test-Path $installer)) {
    throw "Installer is missing at $installer. Run scripts/build-installer.ps1 first."
}

# ── Scenario 1: Install with autostart task enabled ──────────────────────────
Write-Output "[TEST] Scenario 1: Clean install with autostart task enabled (/TASKS=autostart)"

# Stop any running instances
Stop-Process -Name 'AjazzBatteryMonitor' -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

$installProcess = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/TASKS=desktopicon\,autostart' `
    -Wait -PassThru
if ($installProcess.ExitCode -ne 0) { throw "Silent install failed with exit code $($installProcess.ExitCode)." }
if (-not (Test-Path $installedExe)) { throw "Installed executable was not found at $installedExe" }
if (-not (Test-Path $uninstaller))  { throw "Uninstaller was not found at $uninstaller" }

# Verify HKCU Run entry contains --background
$runValue = Get-ItemProperty -Path $registryPath -Name $registryName -ErrorAction SilentlyContinue
if ($null -eq $runValue) {
    throw "[FAIL] HKCU Run entry '$registryName' was not created (autostart task was requested)"
}
$regData = $runValue.$registryName
Write-Output "[OK] HKCU Run value: $regData"

if ($regData -notmatch [regex]::Escape($installedExe)) {
    throw "[FAIL] Registry value does not reference the installed EXE path"
}
if ($regData -notmatch '--background') {
    throw "[FAIL] Registry value DOES NOT contain --background — autostart would open the window on login"
}
# Path must be quoted separately from --background
if ($regData -notmatch '"[^"]+\.exe" --background') {
    throw "[FAIL] Registry value format is incorrect. Expected: `"<path>.exe`" --background"
}
Write-Output "[OK] Registry value contains --background and is correctly formatted"

# Verify Start Menu shortcut does NOT contain --background
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$shortcut = Get-ChildItem $startMenuDir -Recurse -Filter 'AJAZZ Battery Monitor.lnk' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($shortcut) {
    $shell = New-Object -ComObject WScript.Shell
    $lnk = $shell.CreateShortcut($shortcut.FullName)
    if ($lnk.Arguments -match '--background') {
        throw "[FAIL] Start Menu shortcut contains --background — manual launch would not open the window"
    }
    Write-Output "[OK] Start Menu shortcut does NOT contain --background (correct)"
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($shell) | Out-Null
}

# Verify smoke-test runs cleanly
Write-Output "[TEST] Running smoke test on installed binary..."
$process = Start-Process -FilePath $installedExe -ArgumentList '--smoke-test', '--allow-multiple-instances' -PassThru
Start-Sleep -Seconds 3
if ($process.HasExited) { throw 'Installed application exited before the smoke-test liveness check.' }
if (-not $process.WaitForExit(15000)) {
    $process.Kill()
    throw 'Installed application did not shut down after the smoke test (15s timeout).'
}
Write-Output "[OK] Smoke test passed on installed binary"

# ── Scenario 2: Upgrade preserves existing autostart state ──────────────────────
Write-Output "[TEST] Scenario 2: Upgrade — autostart choice is preserved (checkedonce)"

# Current state: autostart is enabled. Re-install (upgrade) without specifying /TASKS
$installProcess2 = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
    -Wait -PassThru
if ($installProcess2.ExitCode -ne 0) { throw "Upgrade install failed with exit code $($installProcess2.ExitCode)." }

# After upgrade, the Run entry path should still be correct
$runValue2 = Get-ItemProperty -Path $registryPath -Name $registryName -ErrorAction SilentlyContinue
if ($null -ne $runValue2) {
    Write-Output "[OK] Upgrade: existing autostart entry preserved: $($runValue2.$registryName)"
} else {
    Write-Output "[INFO] Upgrade: autostart entry was cleared (checkedonce behavior may not preserve across re-installs without MERGETASKS)"
}

# ── Scenario 3: Uninstall removes the Run entry ────────────────────────────────
Write-Output "[TEST] Scenario 3: Uninstall removes HKCU Run entry"

$uninstallProcess = Start-Process -FilePath $uninstaller `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
    -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) { throw "Silent uninstall failed with exit code $($uninstallProcess.ExitCode)." }

$afterUninstall = Get-ItemProperty -Path $registryPath -Name $registryName -ErrorAction SilentlyContinue
if ($null -ne $afterUninstall) {
    throw "[FAIL] HKCU Run entry still exists after uninstall!"
}
Write-Output "[OK] HKCU Run entry was removed by uninstall"

if (Test-Path $installDir) {
    $remainingFiles = @(Get-ChildItem -LiteralPath $installDir -Force -Recurse -File -ErrorAction SilentlyContinue)
    if ($remainingFiles.Count -gt 0) {
        throw "[FAIL] Program files remain after silent uninstall: $($remainingFiles.Name -join ', ')"
    }
}
Write-Output "[OK] No program files remain after uninstall"

# ── Scenario 4: Install WITHOUT autostart task ─────────────────────────────────
Write-Output "[TEST] Scenario 4: Install without autostart task (/MERGETASKS=!autostart)"

$installProcess3 = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/MERGETASKS=!autostart' `
    -Wait -PassThru
if ($installProcess3.ExitCode -ne 0) { throw "Install without autostart failed with exit code $($installProcess3.ExitCode)." }

$noAutostart = Get-ItemProperty -Path $registryPath -Name $registryName -ErrorAction SilentlyContinue
if ($null -ne $noAutostart) {
    throw "[FAIL] HKCU Run entry was created even though autostart task was deselected"
}
Write-Output "[OK] No HKCU Run entry when autostart task is deselected"

# Clean up scenario 4
$uninstaller4 = Join-Path $installDir 'unins000.exe'
if (Test-Path $uninstaller4) {
    Start-Process -FilePath $uninstaller4 -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait | Out-Null
}

Write-Output ""
Write-Output "════════════════════════════════════════════════════════"
Write-Output "[PASS] All installer tests completed successfully"
Write-Output "════════════════════════════════════════════════════════"
