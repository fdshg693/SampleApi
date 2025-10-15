# Redis統合ロードマップ

## 概要
既存のSampleApi（ASP.NET Core Minimal API + SvelteKit）にRedisキャッシュ層を統合し、パフォーマンスとスケーラビリティを向上させる。

## 目標
1. **TODOデータの永続化**: インメモリ（ConcurrentDictionary）からRedisへ移行
2. **チャット履歴のキャッシュ**: 最近の会話をRedisにキャッシュ（オプション）
3. **セッション管理**: 将来的なマルチユーザー対応の準備
4. **Docker環境**: ローカル開発用にDocker ComposeでRedisを起動

## 技術スタック
- **Redis**: Docker公式イメージ（`redis:7-alpine`）
- **ASP.NET Clientライブラリ**: StackExchange.Redis（最も人気のあるC#クライアント）
- **開発環境**: Docker Compose
- **本番環境**: Azure Cache for Redis / AWS ElastiCache（将来の展望）

---

## Phase 1: Docker環境セットアップ（30分）

### 1.1 Docker Composeファイル作成
**ファイル**: `docker-compose.yml`（プロジェクトルート）

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    container_name: sampleapi-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5
    restart: unless-stopped

volumes:
  redis-data:
    driver: local
```

**特徴**:
- `appendonly yes`: データ永続化（AOF: Append Only File）
- ヘルスチェック: コンテナの状態監視
- ボリューム: データをホストに保存

### 1.2 Redis起動・確認スクリプト
**ファイル**: `scripts/redis-up.ps1`

```powershell
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
```

**ファイル**: `scripts/redis-cli.ps1`

```powershell
# Redis CLIに接続
docker exec -it sampleapi-redis redis-cli
```

### 1.3 動作確認コマンド
```powershell
# Redisコンテナ起動
docker-compose up -d redis

# 動作確認
docker exec sampleapi-redis redis-cli PING
# Expected: PONG

# データ書き込みテスト
docker exec sampleapi-redis redis-cli SET test "Hello Redis"
docker exec sampleapi-redis redis-cli GET test
# Expected: "Hello Redis"

# 停止
docker-compose down

# データ永続化確認（再起動後もデータが残る）
docker-compose up -d redis
docker exec sampleapi-redis redis-cli GET test
```

---

## Phase 2: バックエンド統合（1-2時間）

### 2.1 NuGetパッケージ追加
```powershell
cd api
dotnet add package StackExchange.Redis --version 2.8.16
```

**`SampleApi.csproj`に追加される**:
```xml
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />
```

### 2.2 設定ファイル更新
**ファイル**: `api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenAI": {
    "ApiKey": "",
    "Model": "gpt-4o-mini"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,abortConnect=false",
    "InstanceName": "SampleApi:"
  }
}
```

**本番用**: `api/appsettings.json`

```json
{
  "Redis": {
    "ConnectionString": "",  // Azure/AWS接続文字列
    "InstanceName": "SampleApi:",
    "Enabled": true
  }
}
```

### 2.3 Redis接続サービス作成
**新規ファイル**: `api/Services/RedisConnectionService.cs`

```csharp
using StackExchange.Redis;

namespace SampleApi.Services;

/// <summary>
/// Redisへの接続を管理するシングルトンサービス
/// StackExchange.Redisの推奨パターン: 1アプリケーション = 1接続
/// </summary>
public class RedisConnectionService : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly ILogger<RedisConnectionService> _logger;

    public RedisConnectionService(IConfiguration configuration, ILogger<RedisConnectionService> logger)
    {
        _logger = logger;
        var connectionString = configuration["Redis:ConnectionString"] 
            ?? throw new InvalidOperationException("Redis:ConnectionString is not configured");

        _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            try
            {
                _logger.LogInformation("Connecting to Redis: {ConnectionString}", 
                    connectionString.Split(',')[0]); // Hide credentials
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        });
    }

    public IConnectionMultiplexer Connection => _connection.Value;
    
    public IDatabase GetDatabase(int db = -1) => Connection.GetDatabase(db);

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
```

### 2.4 TodoServiceをRedis対応に変更
**既存ファイル**: `api/Services/TodoService.cs`

```csharp
using System.Text.Json;
using StackExchange.Redis;
using SampleApi.Models;

namespace SampleApi.Services;

/// <summary>
/// TODOのCRUD操作を管理するサービス（Redis版）
/// </summary>
public class TodoService
{
    private readonly IDatabase _redis;
    private readonly string _keyPrefix;
    private readonly string _allTodosKey;
    private readonly ILogger<TodoService> _logger;

    public TodoService(
        RedisConnectionService redisConnection,
        IConfiguration configuration,
        ILogger<TodoService> logger)
    {
        _redis = redisConnection.GetDatabase();
        _keyPrefix = configuration["Redis:InstanceName"] ?? "SampleApi:";
        _allTodosKey = $"{_keyPrefix}todos:all";
        _logger = logger;
    }

    /// <summary>
    /// 全TODOアイテムを取得
    /// </summary>
    public async Task<GetTodosResponse> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var todoIds = await _redis.SetMembersAsync(_allTodosKey);
            var todos = new List<TodoItem>();

            foreach (var id in todoIds)
            {
                var todo = await GetByIdAsync(id.ToString(), ct);
                if (todo != null)
                {
                    todos.Add(todo);
                }
            }

            var sorted = todos.OrderByDescending(t => t.CreatedAt).ToList();
            return new GetTodosResponse
            {
                Todos = sorted,
                Total = sorted.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all todos from Redis");
            throw;
        }
    }

    /// <summary>
    /// IDで特定のTODOアイテムを取得
    /// </summary>
    public async Task<TodoItem?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var key = $"{_keyPrefix}todo:{id}";
            var json = await _redis.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<TodoItem>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get todo {Id} from Redis", id);
            return null;
        }
    }

    /// <summary>
    /// 新規TODOアイテムを作成
    /// </summary>
    public async Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var todo = new TodoItem(
                Id: Guid.NewGuid().ToString(),
                Title: request.Title,
                Description: request.Description,
                IsCompleted: false,
                CreatedAt: now,
                UpdatedAt: now
            );

            var key = $"{_keyPrefix}todo:{todo.Id}";
            var json = JsonSerializer.Serialize(todo);

            // トランザクション的な操作
            var tran = _redis.CreateTransaction();
            _ = tran.StringSetAsync(key, json);
            _ = tran.SetAddAsync(_allTodosKey, todo.Id);
            await tran.ExecuteAsync();

            _logger.LogInformation("Created todo {Id}: {Title}", todo.Id, todo.Title);
            return todo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create todo in Redis");
            throw;
        }
    }

    /// <summary>
    /// 既存のTODOアイテムを更新
    /// </summary>
    public async Task<TodoItem?> UpdateAsync(string id, UpdateTodoRequest request, CancellationToken ct = default)
    {
        try
        {
            var existing = await GetByIdAsync(id, ct);
            if (existing == null)
            {
                return null;
            }

            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Description = request.Description ?? existing.Description,
                IsCompleted = request.IsCompleted ?? existing.IsCompleted,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var key = $"{_keyPrefix}todo:{id}";
            var json = JsonSerializer.Serialize(updated);
            await _redis.StringSetAsync(key, json);

            _logger.LogInformation("Updated todo {Id}", id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update todo {Id} in Redis", id);
            throw;
        }
    }

    /// <summary>
    /// TODOアイテムを削除
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var key = $"{_keyPrefix}todo:{id}";
            
            var tran = _redis.CreateTransaction();
            _ = tran.KeyDeleteAsync(key);
            _ = tran.SetRemoveAsync(_allTodosKey, id);
            var result = await tran.ExecuteAsync();

            if (result)
            {
                _logger.LogInformation("Deleted todo {Id}", id);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete todo {Id} from Redis", id);
            return false;
        }
    }
}
```

### 2.5 Program.cs更新
**既存ファイル**: `api/Program.cs`

```csharp
// Redis接続（シングルトン）
builder.Services.AddSingleton<RedisConnectionService>();

// Services（既存のTodoServiceをRedis版に置き換え）
builder.Services.AddSingleton<TodoService>();
builder.Services.AddSingleton<AiChatService>();
```

---

## Phase 3: 動作確認（30分）

### 3.1 統合テストの追加
**新規ファイル**: `api/Tests/RedisIntegrationTests.cs`（テストプロジェクトがある場合）

```csharp
[Fact]
public async Task TodoService_CreateAndGet_Success()
{
    // Arrange
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:InstanceName"] = "Test:"
        })
        .Build();
    
    var redisConnection = new RedisConnectionService(config, logger);
    var todoService = new TodoService(redisConnection, config, logger);

    // Act
    var created = await todoService.CreateAsync(new CreateTodoRequest
    {
        Title = "Test Todo",
        Description = "Integration test"
    });

    var retrieved = await todoService.GetByIdAsync(created.Id);

    // Assert
    Assert.NotNull(retrieved);
    Assert.Equal("Test Todo", retrieved.Title);
    
    // Cleanup
    await todoService.DeleteAsync(created.Id);
}
```

### 3.2 手動テスト手順
```powershell
# 1. Redisコンテナ起動
docker-compose up -d redis

# 2. APIサーバー起動
cd api
dotnet run

# 3. 別のターミナルでSwagger UIを開く
start https://localhost:7082/swagger

# 4. Swagger UIで操作
# - POST /api/todos で新規TODO作成
# - GET /api/todos で取得確認
# - PUT /api/todos/{id} で更新
# - DELETE /api/todos/{id} で削除

# 5. Redis CLIで直接確認
docker exec -it sampleapi-redis redis-cli

# Redis CLI内で:
> KEYS SampleApi:*
> GET SampleApi:todo:{取得したID}
> SMEMBERS SampleApi:todos:all
```

### 3.3 curlでのテスト例
```powershell
# TODO作成
curl -X POST https://localhost:7082/api/todos `
  -H "Content-Type: application/json" `
  -d '{"title":"Buy milk","description":"From Redis!"}'

# 一覧取得
curl https://localhost:7082/api/todos

# Redisで確認
docker exec sampleapi-redis redis-cli KEYS "SampleApi:*"
```

---

## Phase 4: 高度な機能（オプション、2-3時間）

### 4.1 チャット履歴のキャッシュ
**新規ファイル**: `api/Services/ChatCacheService.cs`

```csharp
public class ChatCacheService
{
    private readonly IDatabase _redis;
    private readonly string _keyPrefix;
    private readonly TimeSpan _expiration = TimeSpan.FromHours(24);

    public ChatCacheService(RedisConnectionService redisConnection, IConfiguration configuration)
    {
        _redis = redisConnection.GetDatabase();
        _keyPrefix = configuration["Redis:InstanceName"] ?? "SampleApi:";
    }

    /// <summary>
    /// セッションIDでチャット履歴を保存（最大100件）
    /// </summary>
    public async Task SaveChatHistoryAsync(string sessionId, List<ChatMessage> messages)
    {
        var key = $"{_keyPrefix}chat:{sessionId}";
        var json = JsonSerializer.Serialize(messages);
        await _redis.StringSetAsync(key, json, _expiration);
    }

    /// <summary>
    /// チャット履歴を取得
    /// </summary>
    public async Task<List<ChatMessage>?> GetChatHistoryAsync(string sessionId)
    {
        var key = $"{_keyPrefix}chat:{sessionId}";
        var json = await _redis.StringGetAsync(key);
        
        if (json.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<ChatMessage>>(json!);
    }
}
```

### 4.2 Rate Limiting（レート制限）
**NuGetパッケージ**: `dotnet add package RedisRateLimiting`

```csharp
public class RateLimitService
{
    private readonly IDatabase _redis;
    
    /// <summary>
    /// スライディングウィンドウでレート制限チェック
    /// </summary>
    public async Task<bool> IsAllowedAsync(string clientId, int maxRequests, TimeSpan window)
    {
        var key = $"ratelimit:{clientId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (long)window.TotalSeconds;

        // 古いエントリを削除
        await _redis.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);

        // 現在のカウントを取得
        var count = await _redis.SortedSetLengthAsync(key);

        if (count >= maxRequests)
        {
            return false; // 制限超過
        }

        // 新しいリクエストを記録
        await _redis.SortedSetAddAsync(key, Guid.NewGuid().ToString(), now);
        await _redis.KeyExpireAsync(key, window);

        return true;
    }
}
```

### 4.3 パフォーマンス監視
**Program.cs**に追加:

```csharp
// Redis接続状態のヘルスチェック
builder.Services.AddHealthChecks()
    .AddCheck("redis", () =>
    {
        try
        {
            var redis = app.Services.GetRequiredService<RedisConnectionService>();
            redis.GetDatabase().Ping();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    });

// ヘルスチェックエンドポイント
app.MapHealthChecks("/health/redis");
```

---

## Phase 5: 本番環境対応（1時間）

### 5.1 Azure Cache for Redis設定
**appsettings.json**（本番環境）:

```json
{
  "Redis": {
    "ConnectionString": "your-redis.redis.cache.windows.net:6380,password=YOUR_KEY,ssl=true,abortConnect=false",
    "InstanceName": "SampleApi:",
    "Enabled": true
  }
}
```

### 5.2 環境変数での設定（推奨）
```powershell
# Azure App Serviceで設定
az webapp config appsettings set `
  --name seiwan-sampleApi `
  --resource-group YourResourceGroup `
  --settings `
    "Redis__ConnectionString=your-redis.redis.cache.windows.net:6380,password=***,ssl=true"
```

### 5.3 Docker Compose本番構成
**docker-compose.prod.yml**:

```yaml
version: '3.8'

services:
  api:
    build:
      context: ./api
      dockerfile: Dockerfile
    environment:
      - Redis__ConnectionString=redis:6379
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - redis
    ports:
      - "8080:8080"

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass YOUR_STRONG_PASSWORD --appendonly yes
    volumes:
      - redis-prod-data:/data
    restart: always

volumes:
  redis-prod-data:
```

---

## Phase 6: マイグレーション戦略（30分）

### 6.1 既存データの移行
既存のインメモリデータは存在しないため、**新規スタート**で問題なし。

### 6.2 段階的ロールアウト（フィーチャーフラグ）
```csharp
// appsettings.json
{
  "Features": {
    "UseRedis": true  // false にするとインメモリに戻る
  }
}

// Program.csで切り替え
if (builder.Configuration.GetValue<bool>("Features:UseRedis"))
{
    builder.Services.AddSingleton<RedisConnectionService>();
    builder.Services.AddSingleton<TodoService>(); // Redis版
}
else
{
    builder.Services.AddSingleton<TodoServiceInMemory>(); // 旧版
}
```

---

## トラブルシューティング

### 問題1: Redisに接続できない
```powershell
# Docker確認
docker ps | Select-String redis

# ポート確認
Test-NetConnection -ComputerName localhost -Port 6379

# ログ確認
docker logs sampleapi-redis
```

### 問題2: データが保存されない
```powershell
# Redis CLIで直接確認
docker exec -it sampleapi-redis redis-cli

# 全キー表示
> KEYS *

# 特定のキー取得
> GET SampleApi:todo:xxx

# AOF確認
> BGREWRITEAOF
```

### 問題3: パフォーマンス低下
- **接続プール**: StackExchange.Redisはシングルトンで使用（既に実装済み）
- **バッチ操作**: `CreateBatch()`を使用して複数コマンドをパイプライン化
- **圧縮**: 大きなオブジェクトにはGzip圧縮を検討

---

## 実装チェックリスト

### Phase 1: Docker（必須）
- [ ] `docker-compose.yml`作成
- [ ] Redis起動確認（`docker-compose up -d redis`）
- [ ] PING テスト（`docker exec sampleapi-redis redis-cli PING`）

### Phase 2: バックエンド（必須）
- [ ] StackExchange.Redis NuGetパッケージ追加
- [ ] `appsettings.Development.json`にRedis設定追加
- [ ] `RedisConnectionService.cs`作成
- [ ] `TodoService.cs`をRedis版に書き換え
- [ ] `Program.cs`でDI登録

### Phase 3: テスト（必須）
- [ ] Swagger UIでCRUD操作確認
- [ ] Redis CLIでデータ永続化確認
- [ ] フロントエンド（SvelteKit）で動作確認

### Phase 4: 高度な機能（オプション）
- [ ] チャット履歴キャッシュ
- [ ] Rate Limiting実装
- [ ] ヘルスチェックエンドポイント追加

### Phase 5: 本番環境（後回し可）
- [ ] Azure Cache for Redis / AWS ElastiCache設定
- [ ] SSL/TLS接続設定
- [ ] 環境変数での設定管理

---

## 推定工数
| フェーズ | 時間 | 優先度 |
|---------|------|--------|
| Phase 1: Docker環境 | 30分 | 高 |
| Phase 2: バックエンド統合 | 1-2時間 | 高 |
| Phase 3: 動作確認 | 30分 | 高 |
| Phase 4: 高度な機能 | 2-3時間 | 中 |
| Phase 5: 本番環境対応 | 1時間 | 低 |
| **合計** | **5-7時間** | - |

最小構成（Phase 1-3のみ）: **2-3時間**

---

## 次のステップ
1. このロードマップをレビュー
2. Phase 1から順に実装開始
3. 各フェーズ完了後にコミット
4. 問題があれば`.github/copilot-instructions.md`を更新
