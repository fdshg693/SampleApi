# SampleApi

このリポジトリは、**SvelteKit（フロントエンド）** と **ASP.NET Core Minimal API（バックエンド）** で構成されたフルスタック・チャット＋TODOアプリケーションのサンプルです。

## 🎯 プロジェクト概要

- **バックエンド**: ASP.NET Core Minimal API (.NET 9) - REST API、OpenAI Chat Completions 統合、インメモリTODO管理
- **フロントエンド**: SvelteKit + Vite - モダンなUIフレームワークと高速開発環境
- **API連携**: Vite開発サーバーが `/api/*` を .NET API（HTTPS）にプロキシ
- **型安全**: Orval を使用して OpenAPI 仕様から TypeScript クライアントと Zod スキーマを自動生成
- **バリデーション**: バックエンド（FluentValidation）とフロントエンド（Zod）の両方でリクエストを検証
- **AI統合**: OpenAI Chat Completions API（キー未設定時はスタブ応答で動作確認可能）

## 📁 ディレクトリ構成

```
SampleApi/
├── api/              # ASP.NET Core Minimal API (.NET 9)
│   ├── Models/       # リクエスト/レスポンスDTO
│   ├── Services/     # ビジネスロジック（AiChatService, TodoService）
│   ├── Validators/   # FluentValidationバリデーター
│   ├── Properties/   # launchSettings.json（ポート設定）
│   └── Program.cs    # エンドポイント定義とDI設定
├── front/            # SvelteKit + Vite フロントエンド
│   ├── src/
│   │   ├── lib/
│   │   │   ├── api.ts             # APIクライアント関数
│   │   │   ├── schemas.ts         # Zodスキーマ（再エクスポート）
│   │   │   └── generated/         # Orvalによる自動生成コード
│   │   │       ├── chat/          # - chat.ts, chat.zod.ts
│   │   │       ├── todos/         # - todos.ts, todos.zod.ts
│   │   │       ├── health/        # - health.ts
│   │   │       └── models/        # - TypeScript型定義
│   │   └── routes/
│   │       ├── +page.svelte       # チャット画面
│   │       └── todos/+page.svelte # TODO管理画面
│   ├── orval.config.ts            # Orval設定（fetch + Zod生成）
│   └── vite.config.ts             # Viteプロキシ設定
├── docs/             # ドキュメント（OpenAPI, Orval, FluentValidation設定など）
├── architecture/     # アーキテクチャドキュメント
├── feature/          # 機能仕様・実装メモ
├── openapi-spec.json # OpenAPI仕様書（生成済み）
└── .github/
    ├── copilot-instructions.md    # Copilot向けテクニカルガイド
    ├── prompts/                   # プロンプトテンプレート
    └── workflows/                 # GitHub Actions（Azure Web App デプロイ）
```

## 🚀 主な機能

### 1. ヘルスチェック
- **GET** `/api/health` - APIの稼働状態とサーバー時刻を返却

### 2. AIチャット
- **POST** `/api/chat` - OpenAI Chat Completions API を使用したチャット機能
  - OpenAIキー設定時は実際のAI応答
  - キー未設定時はスタブ（エコー）応答
  - モデル指定可能（デフォルト: `gpt-4o-mini`）

### 3. TODO管理（CRUD）
- **GET** `/api/todos` - 全TODO取得
- **POST** `/api/todos` - 新規TODO作成
- **PUT** `/api/todos/{id}` - TODO更新
- **DELETE** `/api/todos/{id}` - TODO削除
- インメモリストレージ使用（`ConcurrentDictionary`）

## 📋 前提条件

- **Node.js**: 18 以上（推奨: 20+）
- **パッケージマネージャー**: pnpm（推奨）または npm
- **.NET SDK**: 9.0（`dotnet --version` で確認）
- **開発用HTTPS証明書**: 信頼済み（`dotnet dev-certs https --trust`）
- **オプション**: OpenAI API キー（実際のAI応答を使用する場合）

## 🔧 起動手順（ローカル開発）

### 1️⃣ バックエンド（API）を起動

```powershell
# プロジェクトディレクトリに移動
cd api

# HTTPS開発証明書を信頼（初回のみ）
dotnet dev-certs https --trust

# 依存関係を復元
dotnet restore

# （オプション）OpenAI APIキーを設定
# 一時的に設定（現在のセッションのみ）:
$env:OPENAI_API_KEY = "sk-..."
# 永続的に設定（システム環境変数）:
# setx OPENAI_API_KEY "sk-..."

# API起動
dotnet run
```

**起動URL**:
- HTTPS: `https://localhost:7082`（推奨）
- HTTP: `http://localhost:5073`
- OpenAPI仕様: `https://localhost:7082/openapi/v1.json`（開発環境のみ）
- Swagger UI: `https://localhost:7082/swagger`（開発環境のみ）

### 2️⃣ フロントエンドを起動

**別のターミナル**で以下を実行:

```powershell
# フロントエンドディレクトリに移動
cd front

# 依存関係をインストール
pnpm install  # または npm install

# 開発サーバー起動
pnpm dev      # または npm run dev
```

**起動URL**:
- フロントエンド: `http://localhost:5173`
- `/api/*` へのリクエストは Vite プロキシ経由で `https://localhost:7082` に転送されます

### 3️⃣ ブラウザでアクセス

- **チャット画面**: http://localhost:5173/
- **TODO管理画面**: http://localhost:5173/todos

## 🔄 APIクライアントの自動生成（Orval）

OpenAPI仕様から TypeScript クライアントコードと Zod バリデーションスキーマを自動生成できます:

```powershell
cd front

# APIクライアントとZodスキーマを生成
pnpm generate:api
```

**生成されるファイル**:
- `src/lib/generated/chat/chat.ts` - Chat API クライアント（fetch）
- `src/lib/generated/chat/chat.zod.ts` - Chat Zod スキーマ
- `src/lib/generated/todos/todos.ts` - TODO API クライアント（fetch）
- `src/lib/generated/todos/todos.zod.ts` - TODO Zod スキーマ
- `src/lib/generated/health/health.ts` - Health API クライアント
- `src/lib/generated/models/` - TypeScript 型定義

**Orval設定** (`orval.config.ts`):
- **api**: fetch ベースの API クライアント生成（tags-split モード）
- **apiZod**: Zod スキーマ生成（同じタグ分割、`.zod.ts` 拡張子）
- **入力**: `../openapi-spec.json`（ローカルファイル使用で安定動作）

**注意**: 生成されたファイルは手動編集せず、`pnpm generate:api` で再生成してください。

## 🔗 フロントエンドとバックエンドの連携

### アーキテクチャ

```
Browser (localhost:5173)
    ↓ HTTP Request to /api/*
Vite Dev Server (proxy)
    ↓ Forward to https://localhost:7082/api/*
ASP.NET Core API
    ↓ Response
Browser
```

### APIクライアント（`front/src/lib/api.ts`）

フロントエンドのすべてのAPI呼び出しは `api.ts` に集約されています:

| 関数 | HTTPメソッド | エンドポイント | 説明 |
|------|-------------|---------------|------|
| `health()` | GET | `/api/health` | ヘルスチェック |
| `sendChat(request)` | POST | `/api/chat` | チャット送信 |
| `getTodos()` | GET | `/api/todos` | TODO一覧取得 |
| `createTodo(request)` | POST | `/api/todos` | TODO作成 |
| `updateTodo(id, request)` | PUT | `/api/todos/{id}` | TODO更新 |
| `deleteTodo(id)` | DELETE | `/api/todos/{id}` | TODO削除 |

### 画面コンポーネント

- **チャット画面** (`front/src/routes/+page.svelte`)
  - マウント時に `health()` で疎通確認
  - ユーザー入力を `sendChat()` で送信
  - AI応答をメッセージリストに追加表示

- **TODO管理画面** (`front/src/routes/todos/+page.svelte`)
  - マウント時に `getTodos()` で一覧取得
  - 作成・更新・削除の各操作を対応するAPI関数で実行
  - インメモリストレージのためリロード時にデータはリセット

## 📖 API仕様

### 1. ヘルスチェック

**GET** `/api/health`

```json
// レスポンス例
{
  "status": "ok",
  "time": "2025-10-15T12:34:56.789Z"
}
```

### 2. チャット

**POST** `/api/chat`

```json
// リクエスト
{
  "messages": [
    { "role": "user", "content": "こんにちは" }
  ],
  "model": "gpt-4o-mini"  // 省略可（デフォルト: gpt-4o-mini）
}

// レスポンス
{
  "reply": "こんにちは！何かお手伝いできることはありますか？",
  "isStub": false  // trueの場合はスタブ応答
}
```

**エラーハンドリング**:
- `400 Bad Request`: `messages` が空または未指定
- `isStub: true`: OpenAI API キー未設定または外部APIエラー時

### 3. TODO管理（CRUD）

#### 一覧取得
**GET** `/api/todos`

```json
{
  "todos": [
    {
      "id": "abc123",
      "title": "買い物",
      "description": "牛乳、卵、パン",
      "isCompleted": false,
      "createdAt": "2025-10-15T10:00:00Z"
    }
  ],
  "total": 1
}
```

#### 作成
**POST** `/api/todos`

```json
// リクエスト
{
  "title": "買い物",
  "description": "牛乳、卵、パン"  // 省略可
}

// レスポンス: 作成されたTodoItem
```

**エラー**: `400 Bad Request` - `title` が空

#### 更新
**PUT** `/api/todos/{id}`

```json
// リクエスト（全フィールド省略可）
{
  "title": "買い物（完了）",
  "description": "全て購入済み",
  "isCompleted": true
}

// レスポンス: 更新されたTodoItem
```

**エラー**: `404 Not Found` - 存在しないID

#### 削除
**DELETE** `/api/todos/{id}`

- **成功**: `204 No Content`
- **エラー**: `404 Not Found` - 存在しないID

詳細は以下を参照:
- エンドポイント定義: `api/Program.cs`
- ビジネスロジック: `api/Services/AiChatService.cs`, `api/Services/TodoService.cs`
- データモデル: `api/Models/ChatModels.cs`, `api/Models/TodoModels.cs`

## ⚙️ 設定と環境変数

### OpenAI API設定

OpenAI Chat Completions API を使用するには、以下のいずれかの方法でAPIキーを設定します:

#### 方法1: 環境変数（推奨）

```powershell
# 一時的（現在のセッションのみ）
$env:OPENAI_API_KEY = "sk-..."

# 永続的（ユーザー環境変数）
setx OPENAI_API_KEY "sk-..."
```

#### 方法2: appsettings.json

```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o-mini"
  }
}
```

**優先順位**: `appsettings.json` > 環境変数

**デフォルトモデル**: `gpt-4o-mini`

### 開発環境設定

| 項目 | 値 | 設定ファイル |
|------|-----|------------|
| API HTTPS ポート | 7082 | `api/Properties/launchSettings.json` |
| API HTTP ポート | 5073 | `api/Properties/launchSettings.json` |
| フロント ポート | 5173 | Vite デフォルト |
| CORS 許可オリジン | `localhost:5173`, `localhost:3000` | `api/Program.cs` |
| Vite プロキシ先 | `https://localhost:7082` | `front/vite.config.ts` |

## 🛠️ トラブルシューティング

### 問題: フロントエンドから404/ネットワークエラー

**原因と解決策**:
1. **APIが起動していない**
   - `https://localhost:7082/api/health` に直接アクセスして確認
   - バックエンドターミナルでエラーがないか確認

2. **HTTPS証明書が信頼されていない**
   ```powershell
   dotnet dev-certs https --trust
   ```

3. **ポート設定の不一致**
   - `api/Properties/launchSettings.json` のHTTPSポート
   - `front/vite.config.ts` のプロキシ `target` URL
   - 両方が `https://localhost:7082` で一致しているか確認

### 問題: CORSエラー

**原因**: フロントエンドのオリジンがAPI側で許可されていない

**解決策**: `api/Program.cs` の CORS 設定を確認
```csharp
.WithOrigins(
    "http://localhost:5173", // Vite
    "http://localhost:3000"  // Next.js/CRA
)
```

新しいポートを使用する場合は、ここに追加します。

### 問題: 400 Bad Request（/api/chat）

**原因**: リクエストボディの検証エラー

**解決策**:
- `messages` 配列に少なくとも1件のメッセージが必要
- 各メッセージに `role` と `content` が必要

```json
{
  "messages": [
    { "role": "user", "content": "何か質問" }
  ]
}
```

### 問題: 400 Bad Request（/api/todos POST）

**原因**: `title` が空または未指定

**解決策**: `title` フィールドを必ず含める
```json
{
  "title": "タスク名",
  "description": "詳細（省略可）"
}
```

### 問題: Orval APIクライアント生成時のエラー

**原因**: OpenAPI仕様ファイルが見つからない、またはバックエンドが起動していない

**解決策**:
1. **ローカルファイルを使用（推奨）**: 
   ```powershell
   # プロジェクトルートにopenapi-spec.jsonが存在することを確認
   # orval.config.tsで target: '../openapi-spec.json' を使用
   cd front
   pnpm generate:api
   ```

2. **API経由で生成する場合**:
   ```powershell
   # バックエンドを起動
   cd api
   dotnet run
   
   # 別ターミナルでフロントエンド生成
   cd front
   $env:NODE_TLS_REJECT_UNAUTHORIZED='0'  # SSL証明書検証を無効化
   pnpm generate:api
   Remove-Item env:NODE_TLS_REJECT_UNAUTHORIZED
   ```

## 🚀 デプロイ

### GitHub Actions（Azure Web App）

`.github/workflows/master_seiwan-sampleapi.yml` で自動デプロイ設定済み:
- トリガー: 手動ディスパッチ（`workflow_dispatch`）
- ビルド: .NET 9
- デプロイ先: Azure Web App `seiwan-sampleApi`

## 📚 関連ドキュメント

- **テクニカルガイド**: `.github/copilot-instructions.md` - Copilot向けの詳細な実装ガイド
- **OpenAPI仕様**: `docs/openapi.md` - API仕様の詳細
- **Orval設定**: `docs/orval.md` - APIクライアント自動生成の設定
- **FluentValidation設定**: `docs/FluentValidation.md` - バックエンドバリデーションの設定
- **Zod設定**: `docs/zod.md` - フロントエンドバリデーションの設定
- **機能仕様**: `feature/` - 各機能の仕様書と実装メモ
- **アーキテクチャ**: `architecture/` - アーキテクチャドキュメント

## 🤝 開発規約

### バックエンド
- 新規エンドポイントは `api/Program.cs` に Minimal API スタイルで追加
- ビジネスロジックは `api/Services/` 配下のサービスクラスに実装
- リクエスト/レスポンスDTOは `api/Models/` に配置
- バリデーションロジックは `api/Validators/` に FluentValidation クラスとして実装
- エンドポイントには必ず `.WithOpenApi()` を追加してOpenAPI仕様に含める
- バリデーターは `Program.cs` で自動登録され、エンドポイントで `IValidator<T>` を注入

### フロントエンド
- API呼び出しは必ず `front/src/lib/api.ts` 経由で実行
- 新規API関数を追加したら、Svelteコンポーネントからインポート
- `/api` パスプレフィックスを維持（Viteプロキシ対象）
- フォームバリデーションには `front/src/lib/schemas.ts` の Zod スキーマを使用
- 生成された `.zod.ts` ファイルを直接使うか、`schemas.ts` で再エクスポート

### 型安全とバリデーション
- OpenAPI仕様を更新したら `pnpm generate:api` を実行
- 生成されたクライアントコード (`generated/`) は手動編集しない
- バックエンドで FluentValidation ルールを追加したら、Swagger で確認し必要に応じて Zod スキーマも更新
- バリデーションエラーは統一フォーマットで返却: `{ error, errors: [{ field, message }] }`

### コード生成フロー
1. `api/Models/` でDTOを定義
2. `api/Validators/` で FluentValidation バリデーターを作成
3. `api/Program.cs` でエンドポイントを追加（`.WithOpenApi()` 必須）
4. バックエンドを起動して OpenAPI 仕様を生成/エクスポート
5. `cd front && pnpm generate:api` でクライアントと Zod スキーマを生成
6. フロントエンドで生成されたクライアント/スキーマを使用