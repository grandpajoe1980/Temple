param(
    [int]$ApiPort = 5005,
    [int]$FrontendPort = 5173
)

Write-Host "[start-dev] Starting Temple dev environment..." -ForegroundColor Cyan

function Fail($msg) {
    Write-Error "[start-dev][FAIL] $msg"; exit 1
}

# 1. Wait for Postgres to accept connections (simple retry on port)
$pgReady = $false
for($i=1; $i -le 20; $i++){
    try {
        $tcp = New-Object System.Net.Sockets.TcpClient
        $async = $tcp.BeginConnect('127.0.0.1',5432,$null,$null)
        $wait = $async.AsyncWaitHandle.WaitOne(1000)
        if($wait -and $tcp.Connected){ $pgReady=$true; $tcp.Close(); break }
        $tcp.Close()
    } catch {}
    Start-Sleep -Milliseconds 500
}
if(-not $pgReady){ Fail "Postgres not reachable on 5432 after timeout" }
Write-Host "[start-dev] Postgres reachable." -ForegroundColor Green

# 2. Start API (dotnet) in background
$apiPath = Join-Path $PSScriptRoot "..\src\Server\Temple.Api"
if(-not (Test-Path (Join-Path $apiPath 'Temple.Api.csproj'))){ Fail "API project not found at $apiPath" }
Write-Host "[start-dev] Ensuring no previous API instance is running..." -ForegroundColor DarkGray
Get-Process -Name Temple.Api -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "[start-dev] Starting API on port $ApiPort..." -ForegroundColor Yellow
$apiLog = Join-Path $PSScriptRoot "api.log"
Start-Process powershell -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',"cd `"$apiPath`"; dotnet run --urls http://localhost:$ApiPort *>$apiLog") -WindowStyle Hidden

# 3. Wait for API health
$apiHealthy=$false
for($i=1;$i -le 40;$i++){
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:$ApiPort/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if($resp.StatusCode -eq 200){ $apiHealthy=$true; break }
    } catch {}
    Start-Sleep -Milliseconds 500
}
if(-not $apiHealthy){
    Write-Host "[start-dev][ERROR] API failed to become healthy. Tail of log:" -ForegroundColor Red
    if(Test-Path $apiLog){ Get-Content $apiLog -Tail 40 }
    Fail "API not healthy"
}
Write-Host "[start-dev] API healthy at http://localhost:$ApiPort" -ForegroundColor Green

# 4. Frontend install deps if needed & start
$webPath = Join-Path $PSScriptRoot "..\src\Client\Temple.Web"
if(-not (Test-Path (Join-Path $webPath 'package.json'))){ Fail "Frontend package.json not found at $webPath" }
if(-not (Test-Path (Join-Path $webPath 'node_modules'))){
    Write-Host "[start-dev] Installing frontend dependencies..." -ForegroundColor Yellow
    pushd $webPath | Out-Null
    npm install | Out-Null
    if($LASTEXITCODE -ne 0){ Fail "npm install failed" }
    popd | Out-Null
}
Write-Host "[start-dev] Starting frontend (Vite) on port $FrontendPort..." -ForegroundColor Yellow
$feLog = Join-Path $PSScriptRoot "frontend.log"
Start-Process powershell -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-Command',"cd `"$webPath`"; npm run dev *>$feLog") -WindowStyle Hidden

# 5. Wait for Vite dev server
$feReady=$false
for($i=1;$i -le 40;$i++){
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:$FrontendPort" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if($resp.StatusCode -eq 200){ $feReady=$true; break }
    } catch {}
    Start-Sleep -Milliseconds 500
}
if(-not $feReady){
    Write-Host "[start-dev][ERROR] Frontend failed to start. Tail of log:" -ForegroundColor Red
    if(Test-Path $feLog){ Get-Content $feLog -Tail 40 }
    Fail "Frontend not responding"
}
Write-Host "[start-dev] Frontend ready at http://localhost:$FrontendPort" -ForegroundColor Green

# 6. Summary
Write-Host "[start-dev] All services started successfully." -ForegroundColor Cyan
Write-Host " API:      http://localhost:$ApiPort" -ForegroundColor DarkCyan
Write-Host " Frontend: http://localhost:$FrontendPort" -ForegroundColor DarkCyan
Write-Host " Logs: $apiLog , $feLog" -ForegroundColor DarkGray

# Optional: test login endpoint skeleton (disabled by default)
# try {
#   $login = Invoke-WebRequest -Uri "http://localhost:$ApiPort/api/v1/auth/login" -Method POST -Body '{"email":"admin@admin.com","password":"Admin#123"}' -ContentType 'application/json' -UseBasicParsing -TimeoutSec 5
#   Write-Host "[start-dev] Test login status: $($login.StatusCode)" -ForegroundColor Gray
# } catch { Write-Host "[start-dev] Test login failed (expected if creds differ)." -ForegroundColor DarkYellow }
