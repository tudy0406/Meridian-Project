# ============================================================================
#  Meridian — stop the stack
# ============================================================================
$root = $PSScriptRoot
Write-Host "Stopping Meridian..." -ForegroundColor Cyan

# Stop the backend (dotnet) and frontend (vite/node) processes.
Get-Process dotnet, node -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Stop the database container (data is preserved in the Docker volume).
Push-Location $root
try { docker compose stop } catch { }
Pop-Location

Write-Host "Stopped backend, frontend, and the database container." -ForegroundColor Green
Write-Host "Database data is preserved. Use 'docker compose down -v' to wipe it."
