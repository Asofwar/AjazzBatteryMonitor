[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$prohibitedExtensions = '\.(png|jpe?g|gif|bmp|webp|mp4|mov|avi|log|zip)$'
$prohibitedPaths = '(^|/)(artifacts|diagnostics|screenshots?|mockups?)(/|$)'
$trackedFiles = git ls-files
$violations = @($trackedFiles | Where-Object {
    $_ -match $prohibitedExtensions -or $_ -match $prohibitedPaths
})

if ($violations.Count -gt 0) {
    throw "Tracked publication-prohibited paths found: $($violations -join ', ')"
}

$historyPattern = '(BEGIN [A-Z ]+PRIVATE KEY|client_secret|password=|api[_-]?key|\\\\\\?\\HID#|[A-Za-z]:\\Users\\[A-Za-z0-9._-]+)'
$commits = git rev-list --all
$historyMatches = @(git grep -n -I -E $historyPattern $commits -- . ':!**/bin/**' ':!**/obj/**' ':!scripts/scan-publication.ps1')
if ($LASTEXITCODE -eq 0 -and $historyMatches.Count -gt 0) {
    throw 'Potential private data found in Git history. Inspect locally; do not publish the matched content.'
}

$gitleaks = Get-Command gitleaks -ErrorAction SilentlyContinue
if ($null -eq $gitleaks) {
    throw 'gitleaks is required for the publication scan.'
}

& $gitleaks.Source git . --log-opts='--all' --redact --no-banner
if ($LASTEXITCODE -ne 0) {
    throw 'gitleaks detected a potential secret.'
}

Write-Host 'Publication scan passed: no prohibited tracked assets or detected secrets.'
