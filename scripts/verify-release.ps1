$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$artifacts = Join-Path $repoRoot 'artifacts'
$checksumFile = Join-Path $artifacts 'SHA256SUMS.txt'
if (-not (Test-Path $checksumFile)) { throw 'SHA256SUMS.txt is missing.' }

foreach ($line in Get-Content -LiteralPath $checksumFile) {
    if ($line -notmatch '^(?<hash>[a-f0-9]{64})  (?<name>.+)$') { throw "Invalid checksum line: $line" }
    $path = Join-Path $artifacts $Matches.name
    if (-not (Test-Path $path)) { throw "Checksum asset is missing: $($Matches.name)" }
    $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Matches.hash) { throw "Checksum mismatch: $($Matches.name)" }
}

Write-Output 'Release assets and SHA-256 checksums verified.'
