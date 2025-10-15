# Redis Dockerコンテナを起動
Write-Host "Starting Redis container..." -ForegroundColor Cyan
docker-compose up -d redis

Write-Host "`nWaiting for Redis to be healthy..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# ヘルスチェック
$status = docker inspect --format='{{.State.Health.Status}}' sampleapi-redis
if ($status -eq "healthy") {
    Write-Host "✓ Redis is ready!" -ForegroundColor Green
    docker exec sampleapi-redis redis-cli PING
} else {
    Write-Host "✗ Redis is not healthy yet. Status: $status" -ForegroundColor Red
}
