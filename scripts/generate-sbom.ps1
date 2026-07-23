$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$artifacts = Join-Path $repoRoot 'artifacts'
$toolDir = Join-Path $env:TEMP ('ajazz-cyclonedx-' + [guid]::NewGuid().ToString('N'))
$tool = Join-Path $toolDir 'dotnet-CycloneDX.exe'
$dotnetCandidates = @("$env:ProgramFiles\dotnet\dotnet.exe", "${env:ProgramFiles(x86)}\dotnet\dotnet.exe")
$dotnet = $dotnetCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $dotnet) { throw '.NET SDK was not found.' }
$env:PATH = "$(Split-Path -Parent $dotnet);$env:PATH"
if (-not (Test-Path $tool)) {
    New-Item -ItemType Directory -Force -Path $toolDir | Out-Null
    & $dotnet tool install CycloneDX --tool-path $toolDir
    if ($LASTEXITCODE -ne 0) { throw "CycloneDX installation failed with exit code $LASTEXITCODE." }
}

& $tool "$repoRoot\AjazzBattery.sln" --output $artifacts --filename 'AjazzBatteryMonitor-v1.2.0.sbom.json' --output-format Json --runtime win-x64 --configuration Release --exclude-test-projects --disable-package-restore --set-name 'AJAZZ Battery Monitor' --set-version '1.2.0'
if ($LASTEXITCODE -ne 0) { throw "SBOM generation failed with exit code $LASTEXITCODE." }
