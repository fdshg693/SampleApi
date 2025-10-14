# API フォルダの説明

このディレクトリは ASP.NET Core (.NET 9) の最小構成（Minimal API）で実装したバックエンドです。フロントエンド（`front/`）からのチャット呼び出しを受け付け、必要に応じて OpenAI の Chat Completions API を呼び出します。キー未設定または失敗時はスタブ応答（エコー）を返します。

## 概要

- ランタイム: .NET 9
- 構成: Minimal API + OpenAPI（開発時のみ）
- CORS: 開発時に `http://localhost:5173`（Vite）と `http://localhost:3000`（CRA/Next）からの呼び出しを許可
- 主なエンドポイント:
	- GET `/api/health` ヘルスチェック
	- POST `/api/chat` チャット（OpenAI 経由/スタブ）
- OpenAPI（開発時のみ）: `/openapi/v1.json`

## ディレクトリ構成（抜粋）

```
api/
	Program.cs                  // エントリポイント（エンドポイント定義、CORS, OpenAPI）
	Services/
		AiChatService.cs          // OpenAI 呼び出しとスタブ処理
	Models/
		ChatModels.cs             // Chat API のリクエスト/レスポンス型
	appsettings.json            // 既定設定
	appsettings.Development.json// 開発用設定
	SampleApi.http              // VS Code での動作確認用 HTTP リクエスト集
	SampleApi.csproj            // プロジェクト定義
```

## 設定

OpenAI を使う場合は以下のいずれかで API キーとモデル名を設定します。

- `appsettings.json` もしくは `appsettings.Development.json`
	- `OpenAI:ApiKey`: OpenAI の API キー
	- `OpenAI:Model`: モデル名（既定: `gpt-4o-mini`）
- 環境変数
	- `OPENAI_API_KEY`: OpenAI の API キー

キーの優先順位は「appsettings → 環境変数」の順です。

PowerShell（現在のセッションのみ）例:

```powershell
$env:OPENAI_API_KEY = "sk-...あなたのキー..."
```

永続化する場合（再ログイン後有効）:

```powershell
setx OPENAI_API_KEY "sk-...あなたのキー..."
```

## 起動方法（Windows PowerShell）

```powershell
cd c:\CodeStudy\SampleApi\api
dotnet restore
dotnet run
```

初回は開発用 HTTPS 証明書の信頼が必要な場合があります:

```powershell
dotnet dev-certs https --trust
```

起動後、既定では `https://localhost:xxxx`（ポートは環境により異なる）で待ち受けます。フロントエンド（`front/`）は `http://localhost:5173` を使用する想定です。

## エンドポイント

### ヘルスチェック

- メソッド/パス: GET `/api/health`
- レスポンス（例）:

```json
{ "status": "ok", "time": "2025-01-01T00:00:00Z" }
```

### チャット

- メソッド/パス: POST `/api/chat`
- リクエスト（例）:

```json
{
	"messages": [
		{ "role": "user", "content": "こんにちは！" }
	],
	"model": "gpt-4o-mini" // 省略可（appsettings の既定を使用）
}
```

- レスポンス（例）:

```json
{
	"reply": "…モデルの応答またはスタブのエコー…",
	"isStub": false
}
```

- バリデーション: `messages` が空または未指定の場合は `400 Bad Request` を返します。
- フォールバック: キー未設定や OpenAI 呼び出し失敗時は `isStub: true` でエコー応答を返します。

### OpenAPI（開発時のみ）

- JSON: GET `/openapi/v1.json`

## VS Code での動作確認

- `SampleApi.http` を開き、VS Code の REST Client 拡張（または HTTP クライアント機能）でリクエストを送信できます。
- CORS は `http://localhost:5173` と `http://localhost:3000` を許可しています（開発時）。別ポート/ホストから呼び出す場合は `Program.cs` の CORS 設定を更新してください。

## 実装メモ

- `Services/AiChatService.cs` が OpenAI Chat Completions API（`/v1/chat/completions`）を呼び、`Models/ChatModels.cs` の `ChatRequest`/`ChatResponse` モデルをやり取りします。
- 失敗時や未設定時はスタブ応答にフォールバックし、最後のユーザーメッセージを簡易エコーします。

## トラブルシューティング

- 400 エラー: `messages` を 1 件以上含めてください。
- CORS エラー: アクセス元のオリジンが許可されているか確認。必要に応じて CORS 設定を追加。
- HTTPS エラー: 開発用証明書を信頼（`dotnet dev-certs https --trust`）。

