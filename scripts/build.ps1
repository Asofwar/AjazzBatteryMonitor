$ErrorActionPreference = 'Stop'
Write-Output "Building the solution..."
dotnet build --configuration Release --no-restore
