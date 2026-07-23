$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$installer = Join-Path $repoRoot 'artifacts\AjazzBatteryMonitor-Setup-v1.2.1.exe'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\AJAZZ Battery Monitor'
$dataDir = Join-Path $env:LOCALAPPDATA 'AjazzBatteryMonitor'
$marker = Join-Path $dataDir 'upgrade-preservation-test.txt'

if (-not (Test-Path $installer)) { throw 'Installer is missing. Run scripts/build-installer.ps1 first.' }
New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
Set-Content -LiteralPath $marker -Value 'preserve-me' -NoNewline

$initialInstall = Start-Process -FilePath $installer -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($initialInstall.ExitCode -ne 0) { throw "Initial silent install failed with exit code $($initialInstall.ExitCode)." }
$upgrade = Start-Process -FilePath $installer -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($upgrade.ExitCode -ne 0) { throw "Silent upgrade failed with exit code $($upgrade.ExitCode)." }
if (-not (Test-Path $marker)) { throw 'User data marker was not preserved by upgrade.' }

$uninstaller = Join-Path $installDir 'unins000.exe'
$uninstallProcess = Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -Wait -PassThru
if ($uninstallProcess.ExitCode -ne 0) { throw "Silent uninstall failed with exit code $($uninstallProcess.ExitCode)." }
if (-not (Test-Path $marker)) { throw 'User data marker was not preserved by uninstall.' }
Remove-Item -LiteralPath $marker -Force

Write-Output 'Installer upgrade and user-data preservation passed.'
