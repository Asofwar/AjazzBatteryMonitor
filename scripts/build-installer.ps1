$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$publishDir = Join-Path $repoRoot 'artifacts\publish_temp'
$installerOutput = Join-Path $repoRoot 'artifacts'
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $iscc) { throw 'Inno Setup 6 (ISCC.exe) was not found.' }
if (-not (Test-Path (Join-Path $publishDir 'AjazzBattery.App.exe'))) {
    throw 'Published executable not found. Run scripts/publish.ps1 first.'
}

$env:AJAZZ_PUBLISH_DIR = $publishDir
$env:AJAZZ_INSTALLER_OUTPUT = $installerOutput
& $iscc (Join-Path $repoRoot 'installer\AjazzBatteryMonitor.iss')
if ($LASTEXITCODE -ne 0) { throw "ISCC failed with exit code $LASTEXITCODE." }
