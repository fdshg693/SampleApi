# Redis 運用ガイド

## 概要
SampleApiでは、TODOデータの永続化とキャッシュにRedisを使用しています。
このドキュメントでは、開発環境でのRedis操作と状態確認の方法を説明します。

---

## Docker環境のセットアップ

### Redisコンテナの起動
```powershell
# プロジェクトルートで実行
cd C:\CodeStudy\SampleApi
docker-compose up -d redis
```

### 起動確認
```powershell
# コンテナの状態確認
docker ps | Select-String redis

# Expected output:
# CONTAINER ID   IMAGE           STATUS                    PORTS                    NAMES
# xxxxx          redis:7-alpine  Up X minutes (healthy)    0.0.0.0:6379->6379/tcp   sampleapi-redis
```

### スクリプトを使った起動
```powershell
# 自動ヘルスチェック付き起動
.\scripts\redis-up.ps1
```

---

## 基本的なコマンド

### 接続テスト
```powershell
# PING テスト（接続確認）
docker exec sampleapi-redis redis-cli PING
# Expected: PONG
```

### Redis CLIへの接続
```powershell
# インタラクティブモードで接続
docker exec -it sampleapi-redis redis-cli

# または、スクリプトを使用
.\scripts\redis-cli.ps1
```

---

## データ操作コマンド

### 基本的なキー操作
```powershell
# データの書き込み（SET）
docker exec sampleapi-redis redis-cli SET test "Hello Redis"
# Expected: OK

# データの読み取り（GET）
docker exec sampleapi-redis redis-cli GET test
# Expected: Hello Redis

# キーの削除（DEL）
docker exec sampleapi-redis redis-cli DEL test
# Expected: (integer) 1

# キーの存在確認（EXISTS）
docker exec sampleapi-redis redis-cli EXISTS test
# Expected: (integer) 0 (存在しない) または 1 (存在する)
```

### 全キーの確認
```powershell
# すべてのキーを表示
docker exec sampleapi-redis redis-cli KEYS "*"

# SampleApi関連のキーのみ表示
docker exec sampleapi-redis redis-cli KEYS "SampleApi:*"

# TODO関連のキーのみ表示
docker exec sampleapi-redis redis-cli KEYS "SampleApi:todo:*"

# Expected output example:
# 1) "SampleApi:todos:all"
# 2) "SampleApi:todo:abc123"
# 3) "SampleApi:todo:def456"
```

### キーの個数確認
```powershell
# データベース内の全キー数
docker exec sampleapi-redis redis-cli DBSIZE
# Expected: (integer) 5
```

---

## TODOデータの確認

### TODO一覧の取得
```powershell
# TODOのIDリストを取得（Set型）
docker exec sampleapi-redis redis-cli SMEMBERS "SampleApi:todos:all"

# Expected output:
# 1) "abc123"
# 2) "def456"
```

### 特定のTODOデータ取得
```powershell
# JSON形式でTODOデータを取得
docker exec sampleapi-redis redis-cli GET "SampleApi:todo:abc123"

# Expected output (JSON):
# {"Id":"abc123","Title":"Buy milk","Description":"From store","IsCompleted":false,"CreatedAt":"2025-10-16T02:30:00Z","UpdatedAt":"2025-10-16T02:30:00Z"}
```

### TODOデータの手動作成（テスト用）
```powershell
# JSON形式でTODOを作成
$json = '{"Id":"test123","Title":"Test Todo","Description":"Manual test","IsCompleted":false,"CreatedAt":"2025-10-16T00:00:00Z","UpdatedAt":"2025-10-16T00:00:00Z"}'
docker exec sampleapi-redis redis-cli SET "SampleApi:todo:test123" $json

# TODO一覧に追加
docker exec sampleapi-redis redis-cli SADD "SampleApi:todos:all" "test123"
```

---

## ヘルスチェック

### コンテナのヘルス状態確認
```powershell
# ヘルスチェック状態を取得
docker inspect --format='{{.State.Health.Status}}' sampleapi-redis
# Expected: healthy

# 詳細なヘルス情報
docker inspect sampleapi-redis | ConvertFrom-Json | Select-Object -ExpandProperty State | Select-Object -ExpandProperty Health
```

### Redis サーバー情報
```powershell
# サーバー情報の取得
docker exec sampleapi-redis redis-cli INFO server

# メモリ使用状況
docker exec sampleapi-redis redis-cli INFO memory

# 統計情報
docker exec sampleapi-redis redis-cli INFO stats

# レプリケーション情報
docker exec sampleapi-redis redis-cli INFO replication
```

### パフォーマンス確認
```powershell
# 応答時間の確認（PING）
Measure-Command { docker exec sampleapi-redis redis-cli PING }

# スループット測定（redis-benchmark）
docker exec sampleapi-redis redis-benchmark -q -n 10000 -c 10 -P 5
```

---

## データ管理

### データのバックアップ
```powershell
# RDB スナップショットの手動作成
docker exec sampleapi-redis redis-cli BGSAVE
# Expected: Background saving started

# スナップショットの状態確認
docker exec sampleapi-redis redis-cli LASTSAVE
# Expected: (integer) 1729012345 (Unix timestamp)

# AOF ファイルの再書き込み
docker exec sampleapi-redis redis-cli BGREWRITEAOF
# Expected: Background append only file rewriting started
```

### データのクリア
```powershell
# 現在のデータベース（DB0）をクリア
docker exec sampleapi-redis redis-cli FLUSHDB
# Expected: OK

# 全データベースをクリア
docker exec sampleapi-redis redis-cli FLUSHALL
# Expected: OK
```

### データ永続化の確認
```powershell
# コンテナ再起動
docker-compose restart redis

# 少し待つ
Start-Sleep -Seconds 2

# データが残っているか確認
docker exec sampleapi-redis redis-cli KEYS "*"
```

---

## コンテナの管理

### コンテナの停止・起動
```powershell
# コンテナ停止
docker-compose stop redis

# コンテナ起動
docker-compose start redis

# コンテナ再起動
docker-compose restart redis

# コンテナ停止＆削除（データは残る）
docker-compose down

# コンテナとボリュームを削除（データも削除）
docker-compose down -v
```

### ログの確認
```powershell
# リアルタイムログ
docker logs -f sampleapi-redis

# 最新100行のログ
docker logs --tail 100 sampleapi-redis

# タイムスタンプ付きログ
docker logs -t sampleapi-redis
```

### リソース使用状況
```powershell
# CPU・メモリ使用状況
docker stats sampleapi-redis --no-stream

# Expected output:
# CONTAINER ID   NAME              CPU %     MEM USAGE / LIMIT     MEM %
# xxxxx          sampleapi-redis   0.50%     10MiB / 8GiB         0.12%
```

---

## トラブルシューティング

### 問題1: 接続できない
```powershell
# コンテナが起動しているか確認
docker ps | Select-String redis

# ポートが開いているか確認
Test-NetConnection -ComputerName localhost -Port 6379

# コンテナのログを確認
docker logs sampleapi-redis
```

### 問題2: パフォーマンスが遅い
```powershell
# スロークエリログを確認
docker exec sampleapi-redis redis-cli SLOWLOG GET 10

# 接続数を確認
docker exec sampleapi-redis redis-cli CLIENT LIST

# メモリ使用状況を確認
docker exec sampleapi-redis redis-cli INFO memory | Select-String "used_memory"
```

### 問題3: データが消えた
```powershell
# AOF の状態確認
docker exec sampleapi-redis redis-cli INFO persistence

# ボリュームの存在確認
docker volume ls | Select-String redis

# ボリュームの詳細
docker volume inspect sampleapi_redis-data
```

---

## Redis CLI 内での操作

### Redis CLIに接続後の便利コマンド
```bash
# Redis CLIに入る
docker exec -it sampleapi-redis redis-cli

# 以下は Redis CLI 内で実行
127.0.0.1:6379> PING
PONG

# データベース選択（デフォルトは DB0）
127.0.0.1:6379> SELECT 0
OK

# 全キー表示
127.0.0.1:6379> KEYS *

# パターンマッチでキー検索
127.0.0.1:6379> KEYS SampleApi:todo:*

# キーの型を確認
127.0.0.1:6379> TYPE "SampleApi:todos:all"
set

# キーのTTL（有効期限）を確認
127.0.0.1:6379> TTL "SampleApi:todo:abc123"
(integer) -1  # -1 = 無期限

# Set型の要素数を取得
127.0.0.1:6379> SCARD "SampleApi:todos:all"
(integer) 5

# 文字列の長さを取得
127.0.0.1:6379> STRLEN "SampleApi:todo:abc123"
(integer) 256

# CLIを終了
127.0.0.1:6379> EXIT
```

---

## 開発時の便利なワークフロー

### 1. API開発時の確認手順
```powershell
# 1. Redisコンテナ起動
docker-compose up -d redis

# 2. API実行前にデータクリア
docker exec sampleapi-redis redis-cli FLUSHDB

# 3. APIサーバー起動
cd api
dotnet run

# 4. Swagger UIでAPI操作
start https://localhost:7082/swagger

# 5. Redisにデータが入ったか確認
docker exec sampleapi-redis redis-cli KEYS "SampleApi:*"

# 6. 特定のTODOデータを確認
docker exec sampleapi-redis redis-cli GET "SampleApi:todo:{取得したID}"
```

### 2. テストデータの投入
```powershell
# 複数のテストTODOを一括作成
for ($i=1; $i -le 5; $i++) {
    $id = "test$i"
    $json = "{`"Id`":`"$id`",`"Title`":`"Test Todo $i`",`"Description`":`"Test`",`"IsCompleted`":false,`"CreatedAt`":`"2025-10-16T00:00:00Z`",`"UpdatedAt`":`"2025-10-16T00:00:00Z`"}"
    docker exec sampleapi-redis redis-cli SET "SampleApi:todo:$id" $json
    docker exec sampleapi-redis redis-cli SADD "SampleApi:todos:all" $id
}

# 結果確認
docker exec sampleapi-redis redis-cli KEYS "SampleApi:todo:*"
```

### 3. デバッグ時のデータ監視
```powershell
# リアルタイムでRedisコマンドを監視
docker exec sampleapi-redis redis-cli MONITOR

# 別のターミナルでAPIを操作すると、MONITORでコマンドが流れる
# Ctrl+C で終了
```

---

## 本番環境での注意事項

### セキュリティ
```powershell
# 本番環境では必ずパスワードを設定
# docker-compose.prod.yml で設定
command: redis-server --requirepass YOUR_STRONG_PASSWORD

# 接続時
docker exec sampleapi-redis redis-cli -a YOUR_STRONG_PASSWORD PING
```

### パフォーマンス
- `KEYS *` コマンドは本番環境で使用しない（ブロッキング操作）
- 代わりに `SCAN` を使用
```powershell
docker exec sampleapi-redis redis-cli SCAN 0 MATCH "SampleApi:*"
```

### 監視
- Redis の `INFO` コマンドで定期的に監視
- メモリ使用量、接続数、ヒット率などを確認
- Azure Cache for Redis の場合はポータルのメトリクスを活用

---

## 関連ドキュメント
- [Docker Compose設定](../docker-compose.yml)
- [Redis統合ロードマップ](../feature/3_redis.md)
- [TodoService実装](../api/Services/TodoService.cs)
- [RedisConnectionService](../api/Services/RedisConnectionService.cs)

## 参考リンク
- [Redis コマンドリファレンス](https://redis.io/commands/)
- [StackExchange.Redis ドキュメント](https://stackexchange.github.io/StackExchange.Redis/)
- [Docker Redis 公式イメージ](https://hub.docker.com/_/redis)