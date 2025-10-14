# SampleApi（全体 README）

このリポジトリは、SvelteKit（フロントエンド）と ASP.NET Core Minimal API（バックエンド）で構成されたシンプルなチャットアプリのサンプルです。開発時は Vite の開発サーバーから .NET API へプロキシして連携します。

## 構成

```
SampleApi/
  api/    # ASP.NET Core Minimal API (.NET 9)
  front/  # SvelteKit + Vite フロントエンド
  memo/   # 開発メモなど（任意）
```

- バックエンドは `/api/health` と `/api/chat` を提供
- フロントは `/api/*` へのアクセスを Vite の dev server で .NET API へプロキシ
- OpenAI キー未設定時はスタブ（簡易エコー）で応答

## 前提条件

- Node.js（18+ 推奨）/ pnpm または npm
- .NET SDK 9（`dotnet --version` で確認）
- 開発用 HTTPS 証明書の信頼（必要に応じて）

## 起動手順（開発）

ターミナルは Windows PowerShell を想定しています。

### 1) API を起動

```powershell
cd c:\CodeStudy\SampleApi\api
# （任意）OpenAI キーを設定すると実 API 呼び出し、未設定ならスタブ応答
# 一時的に設定する場合:
#$env:OPENAI_API_KEY = "sk-..."
# 永続的に設定する場合:
#setx OPENAI_API_KEY "sk-..."

# 依存関係復元と起動
dotnet restore
# HTTPS 用の開発証明書を信頼（未実行の場合）
dotnet dev-certs https --trust
# API 起動
dotnet run
```

- 既定の起動 URL は `launchSettings.json` により `https://localhost:7082`（および `http://localhost:5073`）
- 開発時のみ OpenAPI を `/openapi/v1.json` に公開

### 2) フロントを起動

別ターミナルで:

```powershell
cd c:\CodeStudy\SampleApi\front
pnpm install  # または npm install
pnpm dev      # または npm run dev
```

- 既定で `http://localhost:5173` で起動
- `front/vite.config.ts` のプロキシ設定により、`/api` へのリクエストは `https://localhost:7082` に転送
- バックエンドの CORS は `http://localhost:5173` と `http://localhost:3000` を許可済み

## 連携の仕組み（Front ⇄ API）

- フロントの呼び出しは `front/src/lib/api.ts` に集約
  - `BASE` は空文字（同一オリジン想定）。開発時は Vite の dev proxy が `/api` を .NET に転送
  - `sendChat(request)` → POST `/api/chat`
  - `health()` → GET `/api/health`
- 画面は `front/src/routes/+page.svelte`
  - マウント時に `health()` を実行して疎通確認
  - フォーム送信で `sendChat()` を呼び、応答メッセージを表示

## API の仕様（抜粋）

- GET `/api/health`
  - 例: `{ "status": "ok", "time": "2025-01-01T00:00:00Z" }`
- POST `/api/chat`
  - リクエスト例:
    ```json
    {
      "messages": [ { "role": "user", "content": "こんにちは" } ],
      "model": "gpt-4o-mini" // 省略可
    }
    ```
  - レスポンス例:
    ```json
    { "reply": "...", "isStub": false }
    ```
  - `messages` 未指定/空は 400 を返却
  - キー未設定や外部 API エラー時は `isStub: true` のスタブ応答

詳細は `api/README.md` と `api/Program.cs`/`Services/AiChatService.cs` を参照してください。

## 設定と環境変数

- OpenAI の設定方法
  - 環境変数: `OPENAI_API_KEY`
  - appsettings: `OpenAI:ApiKey`, `OpenAI:Model`（既定: `gpt-4o-mini`）
- 優先順位: appsettings > 環境変数
- 証明書エラーが出る場合は `dotnet dev-certs https --trust` を実行

## よくあるトラブル

- フロントから 404/ネットワークエラー
  - API が起動していない、または証明書未信頼
  - `vite.config.ts` の `target` が実際の API https ポート（`launchSettings.json` の `https`）と一致しているか
- CORS エラー
  - API 側で許可されているオリジンにアクセス元が含まれているか（既定で `5173` と `3000`）
- 400 Bad Request（/api/chat）
  - `messages` に 1 件以上の要素が必要

## ライセンス

このサンプルのライセンス条件が必要な場合はプロジェクトに合わせて追記してください。
