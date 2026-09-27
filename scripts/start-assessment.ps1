param([switch]$DatabaseOnly, [int]$Port = 5078)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
New-Item -ItemType Directory -Force .demo | Out-Null
$settingsPath = Join-Path (Get-Location) '.demo/assessment-local.json'
if (!(Test-Path -LiteralPath $settingsPath)) {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    @{Password = [Convert]::ToBase64String($bytes)} | ConvertTo-Json | Set-Content -LiteralPath $settingsPath
}
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$env:SMARTFLEET_LOCAL_PG_PASSWORD = $settings.Password
docker compose --progress plain -p smartfleet-assessment -f compose.assessment.yml up -d --wait
if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL did not start. Check Docker Desktop.' }
$connection = "Host=127.0.0.1;Port=55432;Database=smartfleet_assessment;Username=smartfleet_local;Password=$($settings.Password)"
$env:SMARTFLEET_TEST_POSTGRES = $connection
$env:ConnectionStrings__DefaultConnection = $connection
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__Provider = 'PostgreSQL'
$env:Database__ApplyMigrations = 'true'
$env:Simulation__Enabled = 'true'
$env:Cors__AllowedOrigins__0 = 'http://localhost:5173'
$env:Cors__AllowedOrigins__1 = 'http://localhost:5180'
Write-Host 'Isolated PostgreSQL is ready on localhost:55432. Existing SQLite and cloud databases are unchanged.'
if ($DatabaseOnly) { return }
Write-Host "Assessment API: http://localhost:$Port (PostgreSQL; explicit demo weather/accounts)"
dotnet run --project backend/backend.csproj --no-launch-profile --urls "http://localhost:$Port"
