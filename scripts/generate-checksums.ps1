$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$artifacts = Join-Path $repoRoot 'artifacts'
$files = @(
    'AjazzBatteryMonitor-win-x64-v1.2.0.exe',
    'AjazzBatteryMonitor-win-x64-v1.2.0-portable.zip',
    'AjazzBatteryMonitor-Setup-v1.2.0.exe',
    'AjazzBatteryMonitor-v1.2.0.sbom.json'
)

$lines = foreach ($name in $files) {
    $path = Join-Path $artifacts $name
    if (-not (Test-Path $path)) { throw "Missing release asset: $name" }
    "{0}  {1}" -f (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant(), $name
}

[System.IO.File]::WriteAllLines((Join-Path $artifacts 'SHA256SUMS.txt'), $lines)
