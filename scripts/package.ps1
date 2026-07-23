$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ArtifactsDir = "$RepoRoot\artifacts"
$ExePath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.0.2.exe"
$ZipPath = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.0.2-portable.zip"
$DiagZipPath = "$ArtifactsDir\AjazzBatteryMonitor-v1.0.2-diagnostics.zip"

if (-not (Test-Path $ExePath)) {
    Write-Error "Could not find $ExePath. Please run publish.ps1 first."
    exit 1
}

Write-Output "Packaging Portable ZIP..."
if (Test-Path $ZipPath) { Remove-Item -Force $ZipPath }
Compress-Archive -Path $ExePath -DestinationPath $ZipPath -Force

Write-Output "Packaging Diagnostics ZIP..."
if (Test-Path $DiagZipPath) { Remove-Item -Force $DiagZipPath }

$DiagTempDir = "$ArtifactsDir\diag_temp"
if (Test-Path $DiagTempDir) { Remove-Item -Recurse -Force $DiagTempDir }
New-Item -ItemType Directory -Force -Path $DiagTempDir | Out-Null

if (Test-Path "$RepoRoot\docs\runtime-failure-analysis.md") { Copy-Item "$RepoRoot\docs\runtime-failure-analysis.md" "$DiagTempDir\" }
if (Test-Path "$RepoRoot\docs\device-inventory.md") { Copy-Item "$RepoRoot\docs\device-inventory.md" "$DiagTempDir\" }
if (Test-Path "$RepoRoot\docs\protocol-aj179-apex.md") { Copy-Item "$RepoRoot\docs\protocol-aj179-apex.md" "$DiagTempDir\" }

$LogPath = "$env:LOCALAPPDATA\AjazzBatteryMonitor\logs\startup.log"
if (Test-Path $LogPath) {
    Copy-Item $LogPath "$DiagTempDir\startup.log"
}

Compress-Archive -Path "$DiagTempDir\*" -DestinationPath $DiagZipPath -Force
Remove-Item -Recurse -Force $DiagTempDir

Write-Output "Successfully packaged:"
Write-Output "  - $ZipPath"
Write-Output "  - $DiagZipPath"
