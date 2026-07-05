# ============================================================================
#  Meridian — start the whole stack (PostgreSQL + backend API + frontend SPA)
#  Usage:  double-click start.bat   OR   run  ./start.ps1  in PowerShell
# ============================================================================
$ErrorActionPreference = 'Stop'
# This script lives in <repo>/scripts; the repo root is its parent.
$root = Split-Path $PSScriptRoot -Parent
$compose = Join-Path $PSScriptRoot 'docker-compose.yml'

function Info($m)  { Write-Host $m -ForegroundColor Cyan }
function Step($m)  { Write-Host "`n$m" -ForegroundColor Yellow }
function Ok($m)    { Write-Host $m -ForegroundColor Green }

Info "Meridian - starting the full stack..."

# ---- Prerequisite checks ---------------------------------------------------
foreach ($tool in 'docker','dotnet','npm') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        Write-Error "'$tool' was not found on PATH. Please install it (or start Docker Desktop) and try again."
        exit 1
    }
}

# ---- Ensure the HTTPS dev certificate is trusted (idempotent) --------------
try { dotnet dev-certs https --trust | Out-Null } catch { }

# ---- 1/3  Database (Docker) ------------------------------------------------
Step "[1/3] Starting PostgreSQL (docker compose)..."
try {
    docker compose -f $compose up -d db
} catch {
    Write-Error "Could not start the database container. Is Docker Desktop running?"
    exit 1
}

Write-Host "Waiting for the database to become healthy..."
$tries = 0
do {
    Start-Sleep -Seconds 2
    $status = (docker inspect --format '{{.State.Health.Status}}' meridian-db 2>$null)
    $tries++
} until ($status -eq 'healthy' -or $tries -ge 30)
if ($status -eq 'healthy') { Ok "Database is healthy." } else { Write-Warning "Database not healthy yet; continuing." }

# ---- 2/3  Backend API (new window) -----------------------------------------
Step "[2/3] Starting backend API (https://localhost:7100)..."
$backendDir = Join-Path $root 'BACKEND\src\Meridian.Api'
Start-Process powershell -ArgumentList @(
    '-NoExit','-Command',
    "Set-Location '$backendDir'; Write-Host 'Meridian API' -ForegroundColor Cyan; dotnet run --launch-profile https"
)

# ---- 3/3  Frontend SPA (new window) ----------------------------------------
Step "[3/3] Starting frontend (https://localhost:5173)..."
$frontendDir = Join-Path $root 'FRONTEND'
if (-not (Test-Path (Join-Path $frontendDir 'node_modules'))) {
    Write-Host "Installing frontend dependencies (first run only)..."
    Push-Location $frontendDir; npm install; Pop-Location
}
Start-Process powershell -ArgumentList @(
    '-NoExit','-Command',
    "Set-Location '$frontendDir'; Write-Host 'Meridian SPA' -ForegroundColor Cyan; npm run dev"
)

# ---- Done ------------------------------------------------------------------
Ok "`nAll services are starting up:"
Write-Host "  Frontend : https://localhost:5173"
Write-Host "  API      : https://localhost:7100  (http://localhost:5286 redirects here)"
Write-Host "  Database : localhost:5432 (Docker container 'meridian-db')"
Write-Host ""
Write-Host "Two new windows opened for the backend and frontend logs."
Write-Host "First time in a browser: accept the self-signed warning for https://localhost:5173 once."
Write-Host "To stop everything: run stop.bat (in this scripts folder), or close the two windows to stop the app."
