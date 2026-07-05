# ============================================================================
#  Meridian — stop the stack
# ============================================================================
$compose = Join-Path $PSScriptRoot 'docker-compose.yml'
Write-Host "Stopping Meridian..." -ForegroundColor Cyan

# Stop the backend (dotnet) and frontend (vite/node) processes.
Get-Process dotnet, node -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Stop the database container (data is preserved in the Docker volume).
try { docker compose -f $compose stop } catch { }

Write-Host "Stopped backend, frontend, and the database container." -ForegroundColor Green
Write-Host "Database data is preserved. Use 'docker compose -f scripts\docker-compose.yml down -v' to wipe it."
