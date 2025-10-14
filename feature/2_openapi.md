# OpenAPI 導入ロードマップ

このドキュメントでは、SampleApi プロジェクトに OpenAPI（Swagger）を導入し、フロントエンド側で型安全な API クライアントを自動生成するまでの手順を記載します。

## 目標

- バックエンド（.NET Minimal API）から OpenAPI 仕様を自動生成
- フロントエンド（Svelte）で TypeScript 型と API クライアントを自動生成
- 型安全な API 呼び出しと、データフェッチ・送信の最適化

## フェーズ1: .NET Minimal API で OpenAPI（Swagger）を出す

### 概要
ASP.NET Core 9 の組み込み OpenAPI サポートを使用して、API エンドポイントから OpenAPI ドキュメントを生成します。

### 実装タスク
- [ ] `Program.cs` に OpenAPI サービスを追加
  - `builder.Services.AddOpenApi()` で OpenAPI を有効化
  - `app.MapOpenApi()` でエンドポイントを公開
- [ ] Swagger UI を追加（開発環境用）
  - NuGet パッケージ `Swashbuckle.AspNetCore` をインストール
  - `app.UseSwaggerUI()` で UI を有効化
- [ ] 各エンドポイントに OpenAPI メタデータを追加
  - `.WithName()`, `.WithTags()`, `.WithDescription()` などで情報を補完
  - リクエスト/レスポンス例を `.WithOpenApi()` で追加
- [ ] OpenAPI ドキュメントの出力先を確認
  - デフォルト: `/openapi/v1.json`
  - Swagger UI: `/swagger`

### 成果物
- `/openapi/v1.json` で OpenAPI 仕様が取得可能
- `/swagger` で Swagger UI が利用可能
- 全エンドポイント（`/api/health`, `/api/chat`, `/api/todos` など）がドキュメント化

### 参考
- [ASP.NET Core OpenAPI ドキュメント](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
- [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)

---

## フェーズ2: Svelte 側で TypeScript 型と API クライアントを生成

### 概要
[Orval](https://orval.dev/) を使用して、OpenAPI 仕様から TypeScript の型定義と API クライアント関数を自動生成します。

### 実装タスク
- [ ] Orval をインストール
  ```bash
  cd front
  pnpm add -D orval
  ```
- [ ] Orval 設定ファイル `orval.config.ts` を作成
  - 入力: バックエンドの `/openapi/v1.json`
  - 出力: `src/lib/generated/` 配下に型とクライアントを生成
  - クライアント形式: `fetch` ベースの関数
- [ ] npm スクリプトに生成コマンドを追加
  ```json
  "scripts": {
    "generate:api": "orval",
    "dev": "vite dev",
    "build": "vite build"
  }
  ```
- [ ] 生成されたクライアントを既存の `api.ts` に統合
  - 既存の手書き API 呼び出しを生成されたものに置き換え
  - または、生成されたクライアントを `api.ts` から再エクスポート

### 成果物
- `src/lib/generated/api.ts` に型安全な API クライアント関数
- 各エンドポイントに対応する TypeScript 型
- OpenAPI 更新時に `pnpm generate:api` で再生成可能

### 参考
- [Orval 公式ドキュメント](https://orval.dev/)
- [Orval Config Options](https://orval.dev/reference/configuration/full-example)

---

## フェーズ3: TanStack Query と Orval の統合

### 概要
Orval で TanStack Query（旧 React Query）の hooks を生成し、データフェッチとキャッシング管理を効率化します。

### 実装タスク
- [ ] TanStack Query for Svelte をインストール
  ```bash
  cd front
  pnpm add @tanstack/svelte-query
  ```
- [ ] Orval の設定を更新して TanStack Query hooks を生成
  - `output.client` を `svelte-query` に設定
  - `useQuery`, `useMutation` などの hooks が自動生成される
- [ ] `+layout.svelte` に `QueryClientProvider` を設定
  ```svelte
  <script>
    import { QueryClient, QueryClientProvider } from '@tanstack/svelte-query';
    const queryClient = new QueryClient();
  </script>
  <QueryClientProvider client={queryClient}>
    <slot />
  </QueryClientProvider>
  ```
- [ ] 各ページで生成された hooks を使用
  - **取得系（GET）**: `useGetTodos()`, `useGetHealth()` など
  - **送信系（POST/PUT/DELETE）**: `useCreateTodo()`, `useUpdateTodo()`, `useDeleteTodo()` など
- [ ] 既存の `+page.svelte` と `todos/+page.svelte` を書き換え
  - 手動の `fetch` 呼び出しを hooks に置き換え
  - ローディング状態・エラー処理を hooks から取得

### 成果物
- 自動キャッシング・再フェッチ・バックグラウンド更新
- 宣言的なデータフェッチ（`$data`, `$isLoading`, `$error` など）
- 楽観的更新（optimistic update）のサポート

### 参考
- [TanStack Query for Svelte](https://tanstack.com/query/latest/docs/svelte/overview)
- [Orval + TanStack Query](https://orval.dev/guides/react-query)

---

## フェーズ4: Zod で入力検証（送信系）

### 概要
Zod を使ってフォーム入力やリクエストボディのバリデーションを型安全に実施します。

### 実装タスク
- [ ] Zod をインストール
  ```bash
  cd front
  pnpm add zod
  ```
- [ ] Orval の設定で Zod スキーマを生成するように設定（オプション）
  - プラグイン `orval-zod` などを利用
  - または手動で各リクエスト型に対応する Zod スキーマを作成
- [ ] フォームバリデーションに Zod を適用
  - 例: TODO 作成フォームで `title` が空でないことを検証
  ```typescript
  import { z } from 'zod';
  const todoSchema = z.object({
    title: z.string().min(1, 'Title is required'),
    description: z.string().optional(),
  });
  ```
- [ ] `useCreateTodo` などの mutation 前にバリデーション実行
  - `todoSchema.parse(formData)` でエラーをキャッチ
  - バリデーションエラーを UI に表示
- [ ] （オプション）Zod + SvelteKit Form Actions で統合

### 成果物
- クライアント側の入力検証がランタイムで実行される
- TypeScript 型と Zod スキーマで二重の型安全性
- バリデーションエラーの明確なユーザーフィードバック

### 参考
- [Zod 公式ドキュメント](https://zod.dev/)
- [Orval Zod Plugin](https://github.com/anymaniax/orval/blob/master/docs/src/pages/guides/zod.md)

---

## まとめ

この4つのフェーズを完了すると、以下が実現されます：

1. **バックエンド**: OpenAPI 仕様を自動生成・公開
2. **フロントエンド**: TypeScript 型と API クライアントを自動生成（Orval）
3. **データフェッチ**: TanStack Query で効率的なキャッシング・再フェッチ
4. **入力検証**: Zod でクライアント側の型安全なバリデーション

これにより、フルスタックで型安全な開発が可能になり、API 仕様変更時の手動修正が不要になります。

---

## 次のステップ

1. フェーズ1から順番に実装を開始
2. 各フェーズ完了後に動作確認とテスト
3. 既存の手書き API 呼び出しを段階的に移行
4. CI/CD パイプラインに API クライアント生成を組み込み（将来的な改善）
