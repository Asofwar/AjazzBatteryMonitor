$ErrorActionPreference = 'Stop'
Write-Output "Restoring dependencies..."
dotnet restore
