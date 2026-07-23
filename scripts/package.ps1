$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ArtifactsDir = "$RepoRoot\artifacts"
$ExePath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.2.0.exe"
$ZipPath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.2.0-portable.zip"
$PackageDir = "$ArtifactsDir\portable-package"

if (-not (Test-Path $ExePath)) {
    Write-Error "Could not find $ExePath. Please run publish.ps1 first."
    exit 1
}

Write-Output "Packaging Portable ZIP..."
if (Test-Path $ZipPath) { Remove-Item -Force $ZipPath }
Copy-Item "$RepoRoot\THIRD-PARTY-NOTICES.md" "$ArtifactsDir\THIRD-PARTY-NOTICES.txt" -Force
if (Test-Path $PackageDir) { Remove-Item -Recurse -Force $PackageDir }
New-Item -ItemType Directory -Path $PackageDir | Out-Null
Copy-Item $ExePath $PackageDir
Copy-Item "$RepoRoot\LICENSE" $PackageDir
Copy-Item "$RepoRoot\THIRD-PARTY-NOTICES.md" "$PackageDir\THIRD-PARTY-NOTICES.txt"
Compress-Archive -Path "$PackageDir\*" -DestinationPath $ZipPath -Force
Remove-Item -Recurse -Force $PackageDir

Write-Output "Successfully packaged:"
Write-Output "  - $ZipPath"
