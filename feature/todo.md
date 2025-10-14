# TODOページ実装ガイド

## 概要
既存のチャット機能に加えて、TODOリスト機能を追加します。  
このドキュメントでは、フロントエンド・バックエンドそれぞれの実装方針と、共通して守るべき設計ルールを定義します。

---

## 共通設計ルール

### 1. エンドポイント設計原則
- **パスプレフィックス**: すべてのAPIエンドポイントは `/api/*` を使用
  - 理由: Viteのプロキシ設定 (`front/vite.config.ts`) との整合性
- **RESTful命名**: リソースベースのURL設計
  - `/api/todos` - TODOリスト全体を扱う
  - `/api/todos/{id}` - 個別のTODOアイテムを扱う
- **HTTPメソッド規約**:
  - `GET /api/todos` - 全TODO取得
  - `POST /api/todos` - 新規TODO作成
  - `PUT /api/todos/{id}` - TODO更新
  - `DELETE /api/todos/{id}` - TODO削除

### 2. ペイロード設計原則
- **JSON形式**: すべてのリクエスト・レスポンスはJSON
- **命名規約**: camelCase (例: `isCompleted`, `createdAt`)
- **System.Text.Json属性**: バックエンドでは `[JsonPropertyName("fieldName")]` を使用
- **必須フィールド**: `record` または `required` で明示

### 3. エラーハンドリング
- **400 Bad Request**: 入力検証エラー時
  - 形式: `{ "error": "エラーメッセージ" }`
- **404 Not Found**: リソースが見つからない場合
- **500 Internal Server Error**: サーバー側エラー

### 4. データモデル

#### TODOアイテム構造
```json
{
  "id": "string (GUID)",
  "title": "string (required, 1-200文字)",
  "description": "string (optional, 0-1000文字)",
  "isCompleted": "boolean",
  "createdAt": "string (ISO 8601)",
  "updatedAt": "string (ISO 8601)"
}
```

---

## バックエンド実装タスク

### 1. データモデル作成 (`api/Models/TodoModels.cs`)
```csharp
using System.Text.Json.Serialization;

namespace SampleApi.Models;

// TODOアイテムの基本構造
public record TodoItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isCompleted")] bool IsCompleted,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt
);

// POST /api/todos リクエスト
public class CreateTodoRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

// PUT /api/todos/{id} リクエスト
public class UpdateTodoRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool? IsCompleted { get; set; }
}

// GET /api/todos レスポンス
public class GetTodosResponse
{
    [JsonPropertyName("todos")]
    public List<TodoItem> Todos { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
```

### 2. サービス層作成 (`api/Services/TodoService.cs`)
- **責務**: TODOのCRUD操作ロジック
- **データストア**: 初期実装はインメモリ (`ConcurrentDictionary<string, TodoItem>`)
- **メソッド**:
  - `Task<GetTodosResponse> GetAllAsync(CancellationToken ct)`
  - `Task<TodoItem?> GetByIdAsync(string id, CancellationToken ct)`
  - `Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken ct)`
  - `Task<TodoItem?> UpdateAsync(string id, UpdateTodoRequest request, CancellationToken ct)`
  - `Task<bool> DeleteAsync(string id, CancellationToken ct)`

### 3. エンドポイント登録 (`api/Program.cs`)
- **場所**: `app.MapPost("/api/chat", ...)` の後に追加
- **依存性注入**: `builder.Services.AddSingleton<TodoService>();`
- **実装例**:
```csharp
// GET /api/todos
app.MapGet("/api/todos", async (TodoService todoService, CancellationToken ct) =>
{
    var response = await todoService.GetAllAsync(ct);
    return Results.Ok(response);
})
.WithName("GetTodos")
.WithOpenApi();

// POST /api/todos
app.MapPost("/api/todos", async (CreateTodoRequest request, TodoService todoService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request?.Title) || request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "title is required and must be 1-200 characters" });
    }

    var todo = await todoService.CreateAsync(request, ct);
    return Results.Ok(todo);
})
.WithName("CreateTodo")
.WithOpenApi();

// PUT /api/todos/{id}
app.MapPut("/api/todos/{id}", async (string id, UpdateTodoRequest request, TodoService todoService, CancellationToken ct) =>
{
    var todo = await todoService.UpdateAsync(id, request, ct);
    if (todo is null)
    {
        return Results.NotFound(new { error = $"Todo with id '{id}' not found" });
    }
    return Results.Ok(todo);
})
.WithName("UpdateTodo")
.WithOpenApi();

// DELETE /api/todos/{id}
app.MapDelete("/api/todos/{id}", async (string id, TodoService todoService, CancellationToken ct) =>
{
    var deleted = await todoService.DeleteAsync(id, ct);
    if (!deleted)
    {
        return Results.NotFound(new { error = $"Todo with id '{id}' not found" });
    }
    return Results.NoContent();
})
.WithName("DeleteTodo")
.WithOpenApi();
```

### 4. CORS設定
- **既存の設定を維持**: `http://localhost:5173` と `http://localhost:3000` は既に許可済み
- **追加不要**: 新しいエンドポイントも自動的に同じCORSポリシーが適用される

---

## フロントエンド実装タスク

### 1. API型定義・関数追加 (`front/src/lib/api.ts`)
```typescript
// 既存のChatMessage, ChatRequest等の後に追加

export type TodoItem = {
	id: string;
	title: string;
	description?: string;
	isCompleted: boolean;
	createdAt: string;
	updatedAt: string;
};

export type CreateTodoRequest = {
	title: string;
	description?: string;
};

export type UpdateTodoRequest = {
	title?: string;
	description?: string;
	isCompleted?: boolean;
};

export type GetTodosResponse = {
	todos: TodoItem[];
	total: number;
};

// GET /api/todos
export async function getTodos(signal?: AbortSignal): Promise<GetTodosResponse> {
	const res = await fetch(`${BASE}/api/todos`, { signal });
	if (!res.ok) {
		const text = await res.text();
		throw new Error(`API error ${res.status}: ${text}`);
	}
	return res.json();
}

// POST /api/todos
export async function createTodo(request: CreateTodoRequest, signal?: AbortSignal): Promise<TodoItem> {
	const res = await fetch(`${BASE}/api/todos`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(request),
		signal
	});
	if (!res.ok) {
		const text = await res.text();
		throw new Error(`API error ${res.status}: ${text}`);
	}
	return res.json();
}

// PUT /api/todos/{id}
export async function updateTodo(id: string, request: UpdateTodoRequest, signal?: AbortSignal): Promise<TodoItem> {
	const res = await fetch(`${BASE}/api/todos/${id}`, {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(request),
		signal
	});
	if (!res.ok) {
		const text = await res.text();
		throw new Error(`API error ${res.status}: ${text}`);
	}
	return res.json();
}

// DELETE /api/todos/{id}
export async function deleteTodo(id: string, signal?: AbortSignal): Promise<void> {
	const res = await fetch(`${BASE}/api/todos/${id}`, {
		method: 'DELETE',
		signal
	});
	if (!res.ok) {
		const text = await res.text();
		throw new Error(`API error ${res.status}: ${text}`);
	}
}
```

### 2. TODOページ作成 (`front/src/routes/todos/+page.svelte`)
- **ディレクトリ**: `front/src/routes/todos/` を新規作成
- **ファイル**: `+page.svelte`
- **主要機能**:
  - TODOリスト表示（`getTodos()` を `onMount` で呼び出し）
  - 新規TODO作成フォーム（タイトル・説明入力）
  - チェックボックスで完了/未完了切り替え（`updateTodo()` 呼び出し）
  - 削除ボタン（`deleteTodo()` 呼び出し）
  - エラーハンドリング表示

### 3. ナビゲーション追加
- **オプション**: トップページ (`+page.svelte`) にTODOページへのリンクを追加
- または、共通ヘッダーコンポーネント作成して両ページで使用

### 4. スタイリング
- **デザイン統一**: チャットページと同じダークテーマを継承
- **レイアウト**: カード型UI、チェックボックス、削除アイコン等
- **レスポンシブ**: モバイル対応（既存のチャットUIと同様）

---

## 実装順序の推奨フロー

1. **バックエンド**: `api/Models/TodoModels.cs` 作成
2. **バックエンド**: `api/Services/TodoService.cs` 作成・実装
3. **バックエンド**: `api/Program.cs` にエンドポイント登録・サービス注入
4. **バックエンド**: `dotnet run` で起動、APIテスト（Postman, curl, `SampleApi.http` 等）
5. **フロントエンド**: `front/src/lib/api.ts` に型・関数追加
6. **フロントエンド**: `front/src/routes/todos/+page.svelte` 作成
7. **フロントエンド**: `pnpm dev` で起動、動作確認
8. **統合テスト**: ブラウザで全機能（作成・更新・削除・表示）を確認

---

## テスト方針

### バックエンドテスト
- **手動テスト**: `SampleApi.http` にリクエスト例を追加
```http
### Get all todos
GET https://localhost:7082/api/todos

### Create a todo
POST https://localhost:7082/api/todos
Content-Type: application/json

{
  "title": "Buy milk",
  "description": "2 liters of whole milk"
}

### Update a todo (replace {id} with actual GUID)
PUT https://localhost:7082/api/todos/{id}
Content-Type: application/json

{
  "isCompleted": true
}

### Delete a todo
DELETE https://localhost:7082/api/todos/{id}
```

### フロントエンドテスト
- **手動テスト**: ブラウザで `http://localhost:5173/todos` にアクセス
- **動作確認項目**:
  - [ ] 初回ロード時に空リストまたは既存TODOが表示される
  - [ ] 新規TODO作成が成功する
  - [ ] チェックボックスクリックで完了状態が切り替わる
  - [ ] 削除ボタンでTODOが削除される
  - [ ] エラー時に適切なメッセージが表示される

---

## 将来の拡張案

### データ永続化
- **現在**: インメモリストア（アプリ再起動でデータ消失）
- **次ステップ**: 
  - SQLite / SQL Server / PostgreSQL 接続
  - Entity Framework Core 導入
  - `api/Data/TodoDbContext.cs` 作成

### 認証・認可
- **現在**: 認証なし（全ユーザー共通のTODOリスト）
- **次ステップ**:
  - Azure AD B2C / Auth0 等による認証
  - ユーザーごとのTODO分離

### フィルタ・ソート
- **クエリパラメータ**: 
  - `GET /api/todos?completed=true`
  - `GET /api/todos?sort=createdAt&order=desc`
- **検索機能**: `GET /api/todos?search=keyword`

### リアルタイム同期
- **SignalR**: 複数クライアント間でのTODO変更をリアルタイム通知

---

## 参考ファイル

- **既存のチャット実装**: 
  - `api/Program.cs` (エンドポイント定義の参考)
  - `api/Models/ChatModels.cs` (モデル定義の参考)
  - `api/Services/AiChatService.cs` (サービス層の参考)
  - `front/src/lib/api.ts` (API関数の参考)
  - `front/src/routes/+page.svelte` (UIコンポーネントの参考)

- **設定ファイル**:
  - `api/Properties/launchSettings.json` (ポート設定)
  - `front/vite.config.ts` (プロキシ設定)

---

## 注意事項

- **エンドポイントプレフィックス**: 必ず `/api` で始めること（Viteプロキシの対象範囲）
- **ポート番号**: バックエンドは `https://localhost:7082` を使用（変更しない）
- **型の一貫性**: TypeScriptとC#で同じフィールド名・型を使用（camelCase統一）
- **エラーハンドリング**: フロントエンドで必ず `try-catch` でラップし、ユーザーにフィードバック
- **キャンセル処理**: 長時間処理の場合は `AbortSignal` / `CancellationToken` を活用

---

**このドキュメントに沿って実装することで、既存のチャット機能と統一感のあるTODO機能を追加できます。**
