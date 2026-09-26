$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Simulation__Enabled = 'true'
Write-Host 'SmartFleet demo API: http://localhost:5078'
Write-Host 'In a second terminal run: npm.cmd run dev --prefix web -- --host 127.0.0.1'
Write-Host 'Demo accounts: supervisor@demo.smartfleet, operator@demo.smartfleet, technician@demo.smartfleet'
Write-Host 'Default demo password: DemoFleet!2026 (override Simulation__Password before first startup)'
dotnet run --project backend/backend.csproj --no-launch-profile --urls http://localhost:5078
