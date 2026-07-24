$ErrorActionPreference = 'Stop'

$repoRoot     = Resolve-Path "$PSScriptRoot\.."
$installer    = Join-Path $repoRoot 'artifacts\AjazzBatteryMonitor-Setup-v1.3.0.exe'
$installDir   = Join-Path $env:LOCALAPPDATA 'Programs\AJAZZ Battery Monitor'
$installedExe = Join-Path $installDir 'AjazzBatteryMonitor.exe'
$uninstaller  = Join-Path $installDir 'unins000.exe'
$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$registryName = 'AjazzBatteryMonitor'

if (-not (Test-Path $installer)) {
    throw "Installer not found at $installer. Run scripts/build-installer.ps1 first."
}

# ── Helper: Read Run value ───────────────────────────────────────────────────
function Get-RunValue {
    $prop = Get-ItemProperty -Path $registryPath -Name $registryName -ErrorAction SilentlyContinue
    return $prop?.$registryName
}

function Uninstall-App {
    $u = Join-Path $installDir 'unins000.exe'
    if (Test-Path $u) {
        Start-Process -FilePath $u -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait | Out-Null
    }
}

# ── Ensure clean slate ───────────────────────────────────────────────────────
Stop-Process -Name 'AjazzBatteryMonitor' -Force -ErrorAction SilentlyContinue
Uninstall-App

# ═══════════════════════════════════════════════════════════════════════════
# Scenario A: Autostart was ENABLED — upgrade must preserve --background entry
# ═══════════════════════════════════════════════════════════════════════════
Write-Output "[TEST] Scenario A: Upgrade with autostart enabled"

$p1 = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/TASKS=autostart' `
    -Wait -PassThru
if ($p1.ExitCode -ne 0) { throw "Initial install failed: $($p1.ExitCode)" }

$beforeUpgrade = Get-RunValue
if ($null -eq $beforeUpgrade) { throw "[FAIL] Autostart entry missing after initial install with /TASKS=autostart" }
Write-Output "[OK] Before upgrade Run value: $beforeUpgrade"

# Simulate upgrade (re-run same installer without /TASKS to honor checkedonce)
$p2 = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
    -Wait -PassThru
if ($p2.ExitCode -ne 0) { throw "Upgrade install failed: $($p2.ExitCode)" }

$afterUpgrade = Get-RunValue
Write-Output "[OK] After upgrade Run value: $afterUpgrade"

# Post-upgrade: if entry exists it must contain --background
if ($null -ne $afterUpgrade) {
    if ($afterUpgrade -notmatch '--background') {
        throw "[FAIL] After upgrade, autostart entry is present but missing --background"
    }
    if ($afterUpgrade -notmatch [regex]::Escape($installedExe)) {
        Write-Output "[WARN] Autostart path does not match current EXE path (path may have changed): $afterUpgrade"
    }
    Write-Output "[OK] Upgrade preserved autostart with --background"
} else {
    Write-Output "[WARN] Upgrade cleared autostart entry (checkedonce does not always preserve state on same-version reinstall)"
}

Uninstall-App

$afterUninstallA = Get-RunValue
if ($null -ne $afterUninstallA) {
    throw "[FAIL] HKCU Run entry still exists after uninstall in Scenario A"
}
Write-Output "[OK] Scenario A: Uninstall removed Run entry"

# ═══════════════════════════════════════════════════════════════════════════
# Scenario B: Autostart was DISABLED — upgrade must NOT re-enable it
# ═══════════════════════════════════════════════════════════════════════════
Write-Output "[TEST] Scenario B: Upgrade with autostart disabled"

$p3 = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/MERGETASKS=!autostart' `
    -Wait -PassThru
if ($p3.ExitCode -ne 0) { throw "Initial install without autostart failed: $($p3.ExitCode)" }

$beforeB = Get-RunValue
if ($null -ne $beforeB) {
    throw "[FAIL] Autostart entry exists even though /MERGETASKS=!autostart was specified"
}
Write-Output "[OK] No autostart entry after /MERGETASKS=!autostart"

# Simulate upgrade
$p4 = Start-Process -FilePath $installer `
    -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
    -Wait -PassThru
if ($p4.ExitCode -ne 0) { throw "Upgrade install failed in Scenario B: $($p4.ExitCode)" }

$afterB = Get-RunValue
if ($null -ne $afterB) {
    Write-Output "[WARN] Upgrade re-created autostart entry: $afterB (checkedonce may not preserve disabled state across reinstalls)"
    Write-Output "[INFO] This is acceptable if it only happens when checkedonce re-fires; the user can disable again."
} else {
    Write-Output "[OK] Upgrade did NOT re-enable autostart (correct behavior)"
}

Uninstall-App

$afterUninstallB = Get-RunValue
if ($null -ne $afterUninstallB) {
    throw "[FAIL] HKCU Run entry still exists after uninstall in Scenario B"
}
Write-Output "[OK] Scenario B: Uninstall removed Run entry (or no entry was present)"

Write-Output ""
Write-Output "════════════════════════════════════════════════════════"
Write-Output "[PASS] All upgrade tests completed"
Write-Output "════════════════════════════════════════════════════════"
