$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ArtifactsDir = "$RepoRoot\artifacts"
$ExePath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64.exe"
$ZipPath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-portable.zip"

if (-not (Test-Path $ExePath)) {
    Write-Error "Could not find $ExePath. Please run publish.ps1 first."
    exit 1
}

Write-Output "Packaging to ZIP..."
if (Test-Path $ZipPath) {
    Remove-Item -Force $ZipPath
}

Compress-Archive -Path $ExePath -DestinationPath $ZipPath -Force
Write-Output "Successfully packaged $ZipPath"
