# Orval ドキュメント

## 概要

**Orval** は、OpenAPI/Swagger仕様書から TypeScript のクライアントコードを自動生成するツールです。このプロジェクトでは、ASP.NET Core Minimal API が公開する OpenAPI 仕様から、フロントエンド（SvelteKit）で使用する型安全な API クライアントを自動生成しています。

### 主な利点

- **型安全性**: OpenAPI 仕様から TypeScript の型定義が自動生成され、コンパイル時に型チェックが行われる
- **開発効率**: バックエンドの変更に追従したクライアントコードを自動生成できるため、手動でのAPI呼び出しコードの記述・更新が不要
- **一貫性**: API エンドポイント、リクエスト/レスポンス型、HTTPメソッドなどがOpenAPI仕様と完全に一致
- **ドキュメントの同期**: コードとドキュメントが常に同期した状態を保つ

---

## 基本コマンド

### 1. Orval のインストール

```bash
pnpm add -D orval
# または
npm install -D orval
```

このプロジェクトでは `package.json` の devDependencies に既に含まれています。

### 2. API クライアントの生成

```bash
pnpm generate:api
# または
npm run generate:api
```

このコマンドは内部的に `orval` コマンドを実行し、`orval.config.ts` の設定に基づいてクライアントコードを生成します。

### 3. 生成コマンドの実行タイミング

以下のタイミングで実行する必要があります：

- バックエンドの API エンドポイントを追加・変更・削除した後
- リクエスト/レスポンスの型定義を変更した後
- 新しい機能の開発を開始する前（最新の API 定義を取得するため）

---

## このプロジェクトでの利用方法

### プロジェクト構成

```
front/
├── orval.config.ts              # Orval の設定ファイル
├── package.json                  # "generate:api" スクリプトを定義
└── src/
    └── lib/
        ├── api.ts                # 手動で作成した API 呼び出しラッパー
        └── generated/            # Orval が自動生成するファイル（このフォルダ全体）
            ├── custom-instance.ts  # カスタム fetch インスタンス
            ├── index.ts           # エクスポートの集約
            ├── chat/
            │   └── chat.ts       # Chat API のクライアント
            ├── health/
            │   └── health.ts     # Health API のクライアント
            ├── todos/
            │   └── todos.ts      # Todos API のクライアント
            └── models/
                ├── chatMessage.ts
                ├── chatRequest.ts
                ├── createTodoRequest.ts
                ├── updateTodoRequest.ts
                └── index.ts
```

### 設定ファイル (`orval.config.ts`)

```typescript
import { defineConfig } from 'orval';

export default defineConfig({
  sampleapi: {
    input: {
      target: 'https://localhost:7082/openapi/v1.json',  // OpenAPI仕様のURL
      validation: false,                                  // スキーマ検証を無効化（開発用証明書対策）
    },
    output: {
      mode: 'tags-split',                               // タグごとにファイルを分割
      target: 'src/lib/generated/api.ts',               // 生成ファイルのベースパス
      schemas: 'src/lib/generated/models',              // モデル（型定義）の出力先
      client: 'fetch',                                  // 使用する HTTP クライアント
      baseUrl: 'http://localhost:5073',                 // API のベース URL
      override: {
        mutator: {
          path: 'src/lib/generated/custom-instance.ts', // カスタムインスタンスのパス
          name: 'customInstance',                       // カスタムインスタンスの関数名
        },
      },
    },
  },
});
```

#### 設定の詳細

- **`input.target`**: バックエンドが公開する OpenAPI 仕様書の URL
  - 開発環境では `https://localhost:7082/openapi/v1.json`
  - **注意**: バックエンド（`dotnet run`）が起動している必要がある
  
- **`input.validation: false`**: 
  - 自己署名証明書（開発用）のSSL検証エラーを回避するために無効化
  
- **`output.mode: 'tags-split'`**: 
  - OpenAPI の `tags` ごとにファイルを分割（例: `chat/`, `todos/`, `health/`）
  
- **`output.client: 'fetch'`**: 
  - ブラウザ標準の `fetch` API を使用（追加の HTTP クライアントライブラリ不要）
  
- **`output.baseUrl`**: 
  - 開発時は Vite プロキシ経由でアクセスするため、HTTP ポート `http://localhost:5073` を指定
  - Vite の `vite.config.ts` で `/api/*` を `https://localhost:7082` にプロキシ設定済み
  
- **`override.mutator`**: 
  - デフォルトの fetch 実装をカスタマイズ
  - エラーハンドリング、204 No Content のハンドリングなどを実装

### カスタムインスタンス (`custom-instance.ts`)

Orval が生成する API 呼び出しはすべてこの関数を経由します：

```typescript
export const customInstance = async <T>(
  url: string,
  config: RequestInit = {}
): Promise<T> => {
  try {
    const res = await fetch(url, config);
    
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`API error ${res.status}: ${text}`);
    }
    
    // DELETE requests may not have a response body
    if (res.status === 204 || res.headers.get('content-length') === '0') {
      return undefined as T;
    }
    
    const data = await res.json();
    return { data, status: res.status, headers: res.headers } as T;
  } catch (error) {
    console.error('API call failed:', { url, error });
    throw error;
  }
};
```

**カスタマイズのポイント:**
- エラー時のレスポンスボディを取得して詳細なエラーメッセージを表示
- 204 No Content のレスポンスを適切にハンドリング
- Orval の期待する `{ data, status, headers }` 形式でレスポンスを返す

---

## 開発ワークフロー

### 1. 新しい API エンドポイントを追加する場合

#### バックエンド側（`api/Program.cs`）

```csharp
// 新しいエンドポイントを追加
app.MapGet("/api/users", () => {
    // 実装
})
.WithName("GetUsers")
.WithTags("Users")
.WithOpenApi();
```

#### フロントエンド側

1. **バックエンドを起動**:
   ```bash
   cd api
   dotnet run
   ```

2. **Orval でクライアントコードを生成**:
   ```bash
   cd front
   pnpm generate:api
   ```

3. **生成されたコードを使用**:
   ```typescript
   import { getUsers } from '$lib/generated/users/users';
   
   const response = await getUsers();
   ```

### 2. 既存の API を変更する場合

バックエンドで変更後、同じ手順で再生成：

```bash
# api フォルダでバックエンド起動中の状態で
cd front
pnpm generate:api
```

生成されたコードは既存のファイルを上書きするため、型定義が自動的に最新の状態に更新されます。

### 3. 開発環境のセットアップ

新しい開発者がプロジェクトをクローンした場合：

```bash
# 1. バックエンドの依存関係をインストール
cd api
dotnet restore

# 2. バックエンドを起動（別ターミナル）
dotnet run

# 3. フロントエンドの依存関係をインストール
cd front
pnpm install

# 4. API クライアントコードを生成
pnpm generate:api

# 5. フロントエンドの開発サーバーを起動
pnpm dev
```

---

## トラブルシューティング

### エラー: `UNABLE_TO_VERIFY_LEAF_SIGNATURE` または SSL エラー

**原因**: 開発環境の自己署名証明書が信頼されていない

**解決方法**:
```bash
# .NET 開発証明書を信頼
dotnet dev-certs https --trust
```

または `orval.config.ts` で `validation: false` を設定（既に設定済み）

### エラー: `Cannot read properties of undefined`

**原因**: バックエンドが起動していない、または OpenAPI エンドポイントにアクセスできない

**解決方法**:
1. バックエンドが起動しているか確認: `https://localhost:7082/openapi/v1.json` にアクセス
2. `api/Properties/launchSettings.json` でポート設定を確認
3. CORS設定を確認（既に `Program.cs` で設定済み）

### 生成されたコードが古い

**原因**: バックエンドの変更後に `pnpm generate:api` を実行していない

**解決方法**:
```bash
cd front
pnpm generate:api
```

---

## ベストプラクティス

### 1. 生成ファイルは手動編集しない

`src/lib/generated/` 配下のファイルは自動生成されるため、手動で編集しない。次回の生成時に上書きされます。

### 2. カスタムロジックは別ファイルに記述

生成されたコードをラップする場合は、`src/lib/api.ts` のような別ファイルを作成：

```typescript
// src/lib/api.ts
import { chat } from '$lib/generated/chat/chat';
import type { ChatRequest } from '$lib/generated/models';

export async function sendChat(request: ChatRequest) {
  // カスタムロジック（ローディング状態管理など）
  const response = await chat(request);
  return response.data;
}
```

### 3. TypeScript の型を活用

生成された型定義を積極的に使用：

```typescript
import type { ChatMessage, TodoItem } from '$lib/generated/models';

let messages: ChatMessage[] = [];
let todos: TodoItem[] = [];
```

### 4. バージョン管理

- `src/lib/generated/` は `.gitignore` に追加せず、コミットする
- チーム全体で同じ API クライアントコードを共有
- バックエンドの変更と同時にフロントエンドの生成コードもコミット

---

## 参考リンク

- [Orval 公式ドキュメント](https://orval.dev/)
- [OpenAPI Specification](https://swagger.io/specification/)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
