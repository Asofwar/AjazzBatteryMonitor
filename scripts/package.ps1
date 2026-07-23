$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ArtifactsDir = "$RepoRoot\artifacts"
$ExePath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.2.0.exe"
$ZipPath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.2.0-portable.zip"

if (-not (Test-Path $ExePath)) {
    Write-Error "Could not find $ExePath. Please run publish.ps1 first."
    exit 1
}

Write-Output "Packaging Portable ZIP..."
if (Test-Path $ZipPath) { Remove-Item -Force $ZipPath }
Compress-Archive -Path $ExePath -DestinationPath $ZipPath -Force
Copy-Item "$RepoRoot\THIRD-PARTY-NOTICES.md" "$ArtifactsDir\THIRD-PARTY-NOTICES.txt" -Force

Write-Output "Successfully packaged:"
Write-Output "  - $ZipPath"
