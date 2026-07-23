$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ArtifactsDir = "$RepoRoot\artifacts"

if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null
}

Write-Output "Cleaning old artifacts..."
Get-ChildItem -Path $ArtifactsDir -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "Publishing AjazzBattery.App v1.1.1 as a single-file executable..."
dotnet publish "$RepoRoot\src\AjazzBattery.App\AjazzBattery.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o "$ArtifactsDir\publish_temp"

$PublishedExe = Get-ChildItem -Path "$ArtifactsDir\publish_temp" -Filter "*.exe" | Select-Object -First 1
if ($PublishedExe) {
    Copy-Item $PublishedExe.FullName -Destination "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.1.1.exe" -Force
    # Maintain alias
    Copy-Item $PublishedExe.FullName -Destination "$ArtifactsDir\AjazzBatteryMonitor-win-x64.exe" -Force
    Remove-Item -Recurse -Force "$ArtifactsDir\publish_temp"
    Write-Output "Published EXE to: $ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.1.1.exe"
} else {
    Write-Error "Could not find the published executable."
}
