// orval.config.ts
import { defineConfig } from 'orval';

export default defineConfig({
  // ① APIクライアント（Svelteで使うので fetch ベース）
  api: {
    input: {
      // 生成安定のためローカルファイル推奨
      target: '../openapi-spec.json',
      validation: false,
    },
    output: {
      mode: 'tags-split',
      target: 'src/lib/generated',        // ディレクトリ
      schemas: 'src/lib/generated/models',
      client: 'fetch', 
    },
  },

  // ② Zod スキーマ（同じ OpenAPI を参照）
  apiZod: {
    input: {
      target: '../openapi-spec.json',
      validation: false,
    },
    output: {
      mode: 'tags-split',
      client: 'zod',                      // ← ここがポイント
      target: 'src/lib/generated',        // クライアントと並べたい場合
      fileExtension: '.zod.ts',           // pets.ts と pets.zod.ts を並置
      override: {
        zod: {
          generate: {
            response: true,
            body: true,
            param: true,
            query: true,
            header: true,
          },
          strict: {
            response: true,
            body: true,
            param: true,
            query: true,
            header: true,
          },
          // クエリパラメータを z.coerce.* にしたい場合
          coerce: {
            query: ['string', 'number', 'boolean', 'date'],
          },
          // ISO文字列の扱いを調整したい場合
          dateTimeOptions: { local: true, offset: true, precision: 3 },
        },
      },
    },
  },
});
