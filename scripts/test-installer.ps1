$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$installer = Join-Path $repoRoot 'artifacts\AjazzBatteryMonitor-Setup-v1.3.0.exe'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\AJAZZ Battery Monitor'
$installedExe = Join-Path $installDir 'AjazzBattery.App.exe'
$uninstaller = Join-Path $installDir 'unins000.exe'

if (-not (Test-Path $installer)) { throw 'Installer is missing. Run scripts/build-installer.ps1 first.' }

$installProcess = Start-Process -FilePath $installer -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($installProcess.ExitCode -ne 0) { throw "Silent install failed with exit code $($installProcess.ExitCode)." }
if (-not (Test-Path $installedExe)) { throw 'Installed executable was not found.' }
if (-not (Test-Path $uninstaller)) { throw 'Uninstaller was not found.' }

$process = Start-Process -FilePath $installedExe -ArgumentList '--smoke-test', '--allow-multiple-instances' -PassThru
Start-Sleep -Seconds 3
if ($process.HasExited) { throw 'Installed application exited before the smoke-test liveness check.' }
if (-not $process.WaitForExit(15000)) { throw 'Installed application did not shut down after the smoke test.' }

$uninstallProcess = Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) { throw "Silent uninstall failed with exit code $($uninstallProcess.ExitCode)." }
if (Test-Path $installDir) {
    $remainingFiles = @(Get-ChildItem -LiteralPath $installDir -Force -Recurse -File)
    if ($remainingFiles.Count -gt 0) { throw 'Program files remain after silent uninstall.' }
}

Write-Output 'Installer silent install, application liveness, and silent uninstall passed.'
