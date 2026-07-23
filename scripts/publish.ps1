$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path "$PSScriptRoot\.."
$ArtifactsDir = "$RepoRoot\artifacts"
$dotnetCandidates = @(
    "$env:ProgramFiles\dotnet\dotnet.exe",
    "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
)
$dotnet = $dotnetCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $dotnet) { throw '.NET SDK was not found. Install .NET SDK 8 or add dotnet to PATH.' }

if (-not (Test-Path $ArtifactsDir)) {
    New-Item -ItemType Directory -Force -Path $ArtifactsDir | Out-Null
}

Write-Output "Cleaning old artifacts..."
Get-ChildItem -Path $ArtifactsDir -Recurse -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "Publishing AjazzBattery.App v1.2.0 as a single-file executable..."
& $dotnet restore "$RepoRoot\AjazzBattery.sln" --locked-mode --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw "Locked restore failed with exit code $LASTEXITCODE." }
& $dotnet publish "$RepoRoot\src\AjazzBattery.App\AjazzBattery.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    --no-restore `
    -o "$ArtifactsDir\publish_temp"

if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

$PublishedExe = Get-ChildItem -Path "$ArtifactsDir\publish_temp" -Filter "*.exe" | Select-Object -First 1
if ($PublishedExe) {
    Copy-Item $PublishedExe.FullName -Destination "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.2.0.exe" -Force
    Write-Output "Published EXE to: $ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.2.0.exe"
} else {
    Write-Error "Could not find the published executable."
}
