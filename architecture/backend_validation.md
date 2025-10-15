# バックエンドバリデーション設計

このドキュメントでは、SampleApiにおけるバリデーション戦略とOpenAPI仕様への反映について説明します。

## 概要

現在のプロジェクトでは、**Data AnnotationsとFluentValidationのハイブリッド構成**を採用しています。
それぞれの役割を明確に分けることで、OpenAPI仕様の品質とバリデーションロジックの柔軟性を両立しています。

## バリデーション戦略

### 1. Data Annotations（`api/Models/*.cs`）

**目的**: OpenAPI仕様生成とフロントエンド型安全性の確保

**使用場所**: DTOクラスのプロパティ属性として定義

**例**: `api/Models/TodoModels.cs`
```csharp
public class CreateTodoRequest
{
    [JsonPropertyName("title")]
    [Required(ErrorMessage = "タイトルは必須です")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "タイトルは1〜200文字以内で入力してください")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [StringLength(1000, ErrorMessage = "説明は1000文字以内で入力してください")]
    public string? Description { get; set; }
}
```

**反映される制約**:
- `[Required]` → `required: ["propertyName"]`
- `[StringLength(max, MinimumLength=min)]` → `maxLength`, `minLength`
- `[RegularExpression("pattern")]` → `pattern: "regex"`
- `[Range(min, max)]` → `minimum`, `maximum`

### 2. FluentValidation（`api/Validators/*.cs`）

**目的**: 複雑なバリデーションロジックとビジネスルールの実装

**使用場所**: 専用のValidatorクラスとして定義

**例**: `api/Validators/TodoValidators.cs`
```csharp
public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("説明は1000文字以内で入力してください")
            .When(x => !string.IsNullOrEmpty(x.Description)); // 条件付きバリデーション
    }
}
```

**利用できる機能**:
- `.When()` - 条件付きバリデーション
- `.Must()` - カスタムロジック
- `.SetValidator()` - ネストしたオブジェクトのバリデーション
- `.RuleForEach()` - コレクション要素のバリデーション
- 非同期バリデーション（`.MustAsync()`）

## OpenAPI仕様への反映状況

### ✅ 完全に反映される（Data Annotations）

`openapi-spec.json`の例:

```json
{
  "CreateTodoRequest": {
    "required": ["title"],
    "type": "object",
    "properties": {
      "title": {
        "maxLength": 200,
        "minLength": 1,
        "type": "string"
      },
      "description": {
        "maxLength": 1000,
        "minLength": 0,
        "type": "string",
        "nullable": true
      }
    }
  }
}
```

この仕様から**Orvalが自動的にTypeScript型を生成**:

```typescript
export interface CreateTodoRequest {
  /**
   * @minLength 1
   * @maxLength 200
   */
  title: string;
  /**
   * @minLength 0
   * @maxLength 1000
   * @nullable
   */
  description?: string | null;
}
```

さらに**Zodスキーマも自動生成**（バリデーションに利用可能）:

```typescript
export const createTodoRequestSchema = z.object({
  title: z.string().min(1).max(200),
  description: z.string().max(1000).nullable().optional()
});
```

### ⚠️ 部分的に反映される（FluentValidation）

`MicroElements.Swashbuckle.FluentValidation.AspNetCore`パッケージを使用していますが、**Swagger UIでの表示強化**が主な目的です。

**Program.cs**での設定:
```csharp
// Add FluentValidation rules to Swagger
builder.Services.AddFluentValidationRulesToSwagger();
```

**反映される情報**:
- Swagger UIでのバリデーションヒント表示
- 一部の基本的な制約（NotEmpty、MaximumLength等）

**反映されない情報**:
- `.When()`による条件付きバリデーション
- `.Must()`によるカスタムロジック
- 複雑なビジネスルール

## 役割分担と使い分け

| 項目 | Data Annotations | FluentValidation |
|------|------------------|------------------|
| **OpenAPI反映** | ✅ 完全反映 | ⚠️ 一部のみ |
| **Swagger UI** | ✅ 表示される | ✅ 強化される |
| **フロントエンド型生成** | ✅ Orval/Zodに活用 | ❌ 反映されない |
| **複雑なロジック** | ❌ 制限あり | ✅ 柔軟に記述可能 |
| **条件付きバリデーション** | ❌ 不可 | ✅ `.When()`で可能 |
| **非同期バリデーション** | ❌ 不可 | ✅ `.MustAsync()`で可能 |
| **カスタムエラーメッセージ** | ✅ 可能 | ✅ より柔軟 |
| **保守性** | ✅ DTOと一体化 | ✅ 分離されて管理しやすい |

## 推奨パターン

### ✅ このように使い分ける

```csharp
// api/Models/TodoModels.cs
public class CreateTodoRequest
{
    // 基本的な制約 → Data Annotations
    [Required(ErrorMessage = "タイトルは必須です")]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

// api/Validators/TodoValidators.cs
public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        // 複雑なロジック → FluentValidation
        RuleFor(x => x.Title)
            .Must(title => !title.Contains("禁止ワード"))
            .WithMessage("タイトルに禁止ワードが含まれています");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description)); // 条件付き
    }
}
```

### ❌ これは避ける

```csharp
// アンチパターン: 同じ制約を両方に書く（重複）
public class CreateTodoRequest
{
    [StringLength(200)]  // ❌ 重複
    public string Title { get; set; }
}

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200);  // ❌ 重複
    }
}
```

## エンドポイントでの使用方法

### FluentValidationによるバリデーション実行

`api/Program.cs`での実装例:

```csharp
app.MapPost("/api/todos", async (
    CreateTodoRequest request, 
    IValidator<CreateTodoRequest> validator,  // DIでValidatorを注入
    TodoService todoService, 
    CancellationToken ct) =>
{
    // バリデーション実行
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { 
            error = "Validation failed", 
            errors = validationResult.Errors.Select(e => new { 
                field = e.PropertyName, 
                message = e.ErrorMessage 
            })
        });
    }

    var todo = await todoService.CreateAsync(request, ct);
    return Results.Ok(todo);
})
.WithOpenApi();
```

### バリデーションエラーレスポンス例

```json
{
  "error": "Validation failed",
  "errors": [
    {
      "field": "Title",
      "message": "タイトルは必須です"
    },
    {
      "field": "Description",
      "message": "説明は1000文字以内で入力してください"
    }
  ]
}
```

## フロントエンドとの連携

### 型生成フロー

1. **バックエンド**: DTOにData Annotationsを定義
2. **OpenAPI**: `/openapi/v1.json`に制約が反映される
3. **Orval**: OpenAPI仕様を読み取り、TypeScript型とZodスキーマを生成
4. **フロントエンド**: 生成された型とスキーマでバリデーション

```powershell
# フロントエンドの型を再生成
cd front
$env:NODE_TLS_REJECT_UNAUTHORIZED='0'
pnpm generate:api
Remove-Item env:NODE_TLS_REJECT_UNAUTHORIZED
```

### 生成されるファイル

```
front/src/lib/generated/
├── models/
│   ├── createTodoRequest.ts      # TypeScript型定義
│   ├── updateTodoRequest.ts
│   └── ...
└── todos/
    ├── todos.ts                   # svelte-query hooks
    └── todos.zod.ts               # Zodスキーマ
```

## 設定ファイル

### Program.cs

```csharp
// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();

// Add FluentValidation rules to Swagger
builder.Services.AddFluentValidationRulesToSwagger();
```

### 使用パッケージ

- `FluentValidation.AspNetCore` - FluentValidation本体
- `MicroElements.Swashbuckle.FluentValidation.AspNetCore` - Swagger統合

## ベストプラクティス

### ✅ DO

1. **基本的な制約はData Annotationsで定義**
   - Required, StringLength, Range, RegularExpression等
   - OpenAPI仕様に反映させたい制約

2. **複雑なロジックはFluentValidationで定義**
   - 条件付きバリデーション（`.When()`）
   - カスタムロジック（`.Must()`）
   - ビジネスルール検証

3. **エラーメッセージは日本語で統一**
   - ユーザーフレンドリーなメッセージ
   - フロントエンドでそのまま表示可能

4. **エンドポイントには必ず`.WithOpenApi()`を付ける**
   - OpenAPI仕様生成に必須

### ❌ DON'T

1. **同じ制約を重複して定義しない**
   - メンテナンス性が低下
   - 不整合の原因になる

2. **匿名型でエンドポイントを定義しない**
   - OpenAPI仕様が不正確になる
   - フロントエンド型生成ができない

3. **FluentValidationのみに頼らない**
   - OpenAPI仕様に反映されない
   - フロントエンドで型安全性が失われる

## DTOを`api/Models`に配置するメリット

### 🎯 主要なメリット

1. **型安全性の確保**
   - バックエンド↔フロントエンド間で型定義が完全に同期
   - OpenAPI → Orval → TypeScriptの自動生成パイプライン

2. **バリデーションの一元管理**
   - Data Annotations（OpenAPI反映用）
   - FluentValidation（複雑なロジック用）
   - 役割が明確で保守しやすい

3. **ドキュメント品質の向上**
   - XMLコメント → OpenAPIの`description`に反映
   - Swagger UIで確認可能
   - フロントエンド開発者にも情報が伝わる

4. **保守性とリファクタリング**
   - 変更時の影響範囲が明確
   - IDEのリファクタリング機能が効く
   - 名前空間で統一管理

5. **テストとモック作成の容易性**
   - 型定義が明確
   - MSW等のモックツールで同じ型を使用可能

### ❌ DTO化しない場合のデメリット

```csharp
// アンチパターン: 匿名型やdynamic型
app.MapPost("/api/todos", (dynamic request) => { ... })
```

**失われるもの**:
- OpenAPI仕様が不正確（型情報が欠落）
- フロントエンド型生成ができない（`any`型だらけ）
- バリデーションが手動になる
- リファクタリングが困難
- IDEの補完が効かない

## まとめ

現在のハイブリッド構成は、以下の観点からベストプラクティスに沿っています：

| 評価項目 | スコア | 説明 |
|---------|--------|------|
| **型安全性** | ⭐⭐⭐⭐⭐ | バックエンド↔フロントエンド完全同期 |
| **保守性** | ⭐⭐⭐⭐⭐ | 変更時の影響範囲が明確 |
| **ドキュメント** | ⭐⭐⭐⭐⭐ | OpenAPI/Swagger自動生成 |
| **開発効率** | ⭐⭐⭐⭐⭐ | Orvalで自動型生成、手作業不要 |
| **バリデーション** | ⭐⭐⭐⭐⭐ | FluentValidation + Data Annotations |

**推奨アクション**: この設計を継続し、新機能追加時も同じパターンを踏襲してください。
