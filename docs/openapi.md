# OpenAPI ドキュメント

## 概要

**OpenAPI** (旧称: Swagger) は、RESTful API を記述するための業界標準の仕様です。OpenAPI 仕様書は JSON または YAML 形式で記述され、API のエンドポイント、リクエスト/レスポンスの型、認証方式、エラーレスポンスなどを機械可読な形式で定義します。

### 主な利点

- **標準化されたAPI仕様**: 業界標準のフォーマットで API を定義することで、チーム間やツール間での相互運用性が向上
- **自動ドキュメント生成**: コードから仕様書を自動生成し、常に最新のドキュメントを維持
- **型安全なクライアント生成**: OpenAPI 仕様から各種言語のクライアントコードを自動生成（TypeScript、Java、Python など）
- **テストとモック**: 仕様書を元にした自動テストやモックサーバーの構築が可能
- **対話的なAPI探索**: Swagger UI などのツールで、ブラウザから直接 API を試すことができる

### OpenAPI 仕様のバージョン

- **OpenAPI 3.x**: 現在の最新標準（このプロジェクトで使用）
- **Swagger 2.0**: 旧バージョン（レガシーシステムで使用）

---

## バックエンド（ASP.NET Core）での使い方

### 1. 必要なパッケージ

ASP.NET Core で OpenAPI を利用するには、以下の NuGet パッケージが必要です：

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.8" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="9.0.6" />
```

- **Microsoft.AspNetCore.OpenApi**: ASP.NET Core 9.0+ の公式 OpenAPI サポート
- **Swashbuckle.AspNetCore**: Swagger UI と OpenAPI 仕様生成を提供

### 2. 基本的なセットアップ

#### サービスの登録（`Program.cs`）

```csharp
var builder = WebApplication.CreateBuilder(args);

// OpenAPI サービスを追加
builder.Services.AddOpenApi();

// Swagger UI のためのサービスを追加
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SampleApi",
        Version = "v1",
        Description = "A minimal full-stack chat + TODO API built with ASP.NET Core Minimal API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "SampleApi Team",
        }
    });
});

var app = builder.Build();
```

#### ミドルウェアの設定

```csharp
// 開発環境のみで OpenAPI と Swagger UI を有効化
if (app.Environment.IsDevelopment())
{
    // OpenAPI 仕様書のエンドポイントを追加
    app.MapOpenApi();  // デフォルト: /openapi/v1.json
    
    // Swagger UI を有効化
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "SampleApi v1");
        c.RoutePrefix = "swagger";  // https://localhost:7082/swagger でアクセス
    });
}
```

### 3. エンドポイントへのメタデータ追加

Minimal API では、各エンドポイントに `.WithOpenApi()` や追加のメタデータメソッドを使って情報を付与します：

```csharp
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
   .WithName("Health")                   // エンドポイントの一意な名前
   .WithTags("Health")                   // タグによるグループ化
   .WithSummary("Health check endpoint") // 簡潔な説明
   .WithDescription("Returns the API health status and current server time for quick smoke tests.") // 詳細な説明
   .WithOpenApi();                       // OpenAPI 仕様への登録

app.MapPost("/api/chat", async (ChatRequest request, AiChatService ai, CancellationToken ct) =>
{
    if (request?.Messages is null || request.Messages.Count == 0)
    {
        return Results.BadRequest(new { error = "messages is required" });
    }
    var response = await ai.GetChatCompletionAsync(request, ct);
    return Results.Ok(response);
})
.WithName("Chat")
.WithTags("Chat")
.WithSummary("Send chat messages to AI")
.WithDescription("Accepts an array of chat messages and returns an AI-generated response.")
.WithOpenApi();
```

### 4. リクエスト/レスポンス型の定義

OpenAPI 仕様に型情報を含めるため、DTO（Data Transfer Object）クラスを定義します：

```csharp
// Models/ChatModels.cs
public record ChatRequest
{
    public List<ChatMessage> Messages { get; init; } = new();
    public string? Model { get; init; }
}

public record ChatMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}

public record ChatResponse
{
    public required string Reply { get; init; }
    public bool IsStub { get; init; }
}
```

### 5. アクセス方法

開発環境で API を起動すると、以下のエンドポイントにアクセスできます：

- **OpenAPI 仕様書（JSON）**: `https://localhost:7082/openapi/v1.json`
- **Swagger UI（対話的ドキュメント）**: `https://localhost:7082/swagger`

---

## このプロジェクトでの使い方

### プロジェクト構成

```
api/
├── Program.cs                    # OpenAPI の設定とエンドポイント定義
├── SampleApi.csproj              # NuGet パッケージ参照
├── Models/
│   ├── ChatModels.cs            # Chat API のリクエスト/レスポンス型
│   └── TodoModels.cs            # TODO API のリクエスト/レスポンス型
└── Services/
    ├── AiChatService.cs         # Chat ビジネスロジック
    └── TodoService.cs           # TODO ビジネスロジック

front/
├── orval.config.ts              # Orval の設定（OpenAPI 仕様から TypeScript クライアントを生成）
└── src/lib/generated/           # 生成された TypeScript クライアントコード
```

### 開発ワークフロー

#### 1. バックエンドで新しい API エンドポイントを追加

**例: 新しい TODO 検索エンドポイントを追加**

```csharp
// Program.cs
app.MapGet("/api/todos/search", (string? keyword, TodoService todoService) =>
{
    var results = todoService.Search(keyword);
    return Results.Ok(results);
})
.WithName("SearchTodos")
.WithTags("Todos")
.WithSummary("Search TODO items by keyword")
.WithDescription("Searches TODO items by title or description matching the provided keyword.")
.WithOpenApi();
```

#### 2. バックエンドを起動して OpenAPI 仕様を確認

```powershell
cd api
dotnet run
```

ブラウザで以下を開く：
- Swagger UI: `https://localhost:7082/swagger`
- OpenAPI JSON: `https://localhost:7082/openapi/v1.json`

#### 3. フロントエンドでクライアントコードを生成

**前提条件**: バックエンドが起動していること（Orval が OpenAPI 仕様を取得するため）

```powershell
cd front
pnpm generate:api
```

このコマンドは以下を実行します：
1. `https://localhost:7082/openapi/v1.json` から OpenAPI 仕様を取得
2. `orval.config.ts` の設定に基づいて TypeScript クライアントコードを生成
3. 生成されたコードを `front/src/lib/generated/` に配置

#### 4. フロントエンドで生成されたクライアントを使用

```typescript
// front/src/routes/todos/+page.svelte
<script lang="ts">
  import { getTodos, createTodo, updateTodo, deleteTodo } from '$lib/generated/todos/todos';
  import type { TodoItem } from '$lib/generated/models';
  
  let todos: TodoItem[] = [];
  
  async function loadTodos() {
    const response = await getTodos();
    todos = response.todos;
  }
  
  async function addTodo(title: string, description?: string) {
    const newTodo = await createTodo({ title, description });
    todos = [...todos, newTodo];
  }
</script>
```

### OpenAPI 仕様のカスタマイズ

#### タグによるグループ化

エンドポイントを `.WithTags()` でグループ化すると、Swagger UI で見やすくなり、Orval では タグごとにファイルが分割されます：

```csharp
// Health タグ
app.MapGet("/api/health", ...).WithTags("Health");

// Chat タグ
app.MapPost("/api/chat", ...).WithTags("Chat");

// Todos タグ
app.MapGet("/api/todos", ...).WithTags("Todos");
app.MapPost("/api/todos", ...).WithTags("Todos");
app.MapPut("/api/todos/{id}", ...).WithTags("Todos");
app.MapDelete("/api/todos/{id}", ...).WithTags("Todos");
```

生成されるフロントエンドの構造：
```
front/src/lib/generated/
├── health/
│   └── health.ts      # Health タグのエンドポイント
├── chat/
│   └── chat.ts        # Chat タグのエンドポイント
└── todos/
    └── todos.ts       # Todos タグのエンドポイント
```

#### バリデーションエラーの記述

エンドポイントの説明に、バリデーションルールやエラーレスポンスを明記：

```csharp
app.MapPost("/api/todos", async (CreateTodoRequest request, TodoService todoService) =>
{
    // バリデーション
    if (string.IsNullOrWhiteSpace(request?.Title) || request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "title is required and must be 1-200 characters" });
    }
    // ...
})
.WithDescription("Creates a new TODO item. Title must be 1-200 characters, description must be 0-1000 characters. Returns 400 if validation fails.")
.WithOpenApi();
```

---

## ベストプラクティス

### 1. 常に最新の仕様を維持

- API を変更したら必ず Swagger UI で確認
- フロントエンド開発前に `pnpm generate:api` を実行

### 2. 明確な命名とドキュメント

- `.WithName()`: 一意で分かりやすい名前を付ける
- `.WithSummary()`: 簡潔な一行説明
- `.WithDescription()`: 詳細な説明、バリデーションルール、エラーケースなど

### 3. 型安全性を活用

- DTO クラスには `required` や `init` を使用
- nullable 型（`string?`）を明示的に定義
- 数値の範囲やバリデーションをコメントで記載

### 4. バージョニング

現在は `/openapi/v1.json` として単一バージョンですが、将来的に API をバージョニングする場合：

```csharp
app.MapGroup("/api/v1")
   .MapGet("/todos", ...).WithOpenApi();

app.MapGroup("/api/v2")
   .MapGet("/todos", ...).WithOpenApi();
```

---

## トラブルシューティング

### 問題: Orval が OpenAPI 仕様を取得できない

**原因**: バックエンドが起動していない、または証明書エラー

**解決策**:
1. バックエンドが起動していることを確認: `cd api; dotnet run`
2. `orval.config.ts` で `validation: false` を設定（開発用自己署名証明書対策）

```typescript
input: {
  target: 'https://localhost:7082/openapi/v1.json',
  validation: false,  // 証明書検証を無効化
}
```

### 問題: Swagger UI でエンドポイントが表示されない

**原因**: `.WithOpenApi()` を付け忘れている

**解決策**: すべてのエンドポイントに `.WithOpenApi()` を追加

```csharp
app.MapGet("/api/example", () => Results.Ok())
   .WithOpenApi();  // これが必要
```

### 問題: 生成された TypeScript の型が正しくない

**原因**: DTO クラスに `required` や nullable 情報が不足している

**解決策**: C# の DTO クラスを修正し、再生成

```csharp
// 修正前
public string Title { get; init; }  // nullable かどうか不明

// 修正後
public required string Title { get; init; }  // 必須
public string? Description { get; init; }    // オプショナル
```

---

## 関連リソース

- [OpenAPI 公式仕様](https://spec.openapis.org/oas/latest.html)
- [ASP.NET Core OpenAPI ドキュメント](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Orval ドキュメント](orval.md)

---

## まとめ

このプロジェクトでは、OpenAPI を活用して以下を実現しています：

1. **バックエンド**: ASP.NET Core Minimal API で自動的に OpenAPI 仕様を生成
2. **ドキュメント**: Swagger UI で対話的な API ドキュメントを提供
3. **フロントエンド**: Orval を使って OpenAPI 仕様から型安全な TypeScript クライアントを自動生成
4. **開発フロー**: バックエンドの変更が自動的にフロントエンドの型定義に反映される

このアプローチにより、API の変更に強い、型安全でメンテナンスしやすいフルスタックアプリケーションを実現しています。
