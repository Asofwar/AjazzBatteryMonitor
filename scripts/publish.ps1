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

Write-Output "Publishing AjazzBattery.App v1.3.0 as a single-file executable..."
& $dotnet restore "$RepoRoot\AjazzBattery.sln" --force-evaluate --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw "Restore failed with exit code $LASTEXITCODE." }
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
    $releaseExe = "$ArtifactsDir\AjazzBatteryMonitor-win-x64-v1.3.0.exe"
    $copied = $false
    for ($attempt = 1; $attempt -le 5 -and -not $copied; $attempt++) {
        try {
            Copy-Item $PublishedExe.FullName -Destination $releaseExe -Force -ErrorAction Stop
            # Also copy without version suffix for smoke tests and installer
            Copy-Item $PublishedExe.FullName -Destination "$ArtifactsDir\AjazzBatteryMonitor-win-x64.exe" -Force -ErrorAction Stop
            $copied = $true
        }
        catch {
            if ($attempt -eq 5) { throw "Could not replace the release executable after $attempt attempts: $($_.Exception.Message)" }
            Start-Sleep -Seconds 2
        }
    }
    Write-Output "Published EXE to: $releaseExe"
} else {
    Write-Error "Could not find the published executable."
}
