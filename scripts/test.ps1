$ErrorActionPreference = 'Stop'
Write-Output "Running tests..."
dotnet test --configuration Release --no-build
