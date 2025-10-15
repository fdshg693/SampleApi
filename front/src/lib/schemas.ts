import { z } from 'zod';

/**
 * TODO作成用のバリデーションスキーマ
 */
export const createTodoSchema = z.object({
	title: z
		.string()
		.trim()
		.min(1, 'タイトルは必須です')
		.max(200, 'タイトルは200文字以内で入力してください'),
	description: z
		.string()
		.trim()
		.max(1000, '説明は1000文字以内で入力してください')
		.optional()
		.or(z.literal(''))
});

/**
 * TODO更新用のバリデーションスキーマ
 */
export const updateTodoSchema = z.object({
	title: z
		.string()
		.trim()
		.min(1, 'タイトルは必須です')
		.max(200, 'タイトルは200文字以内で入力してください')
		.optional(),
	description: z
		.string()
		.trim()
		.max(1000, '説明は1000文字以内で入力してください')
		.optional()
		.or(z.literal('')),
	isCompleted: z.boolean().optional()
});

/**
 * チャット送信用のバリデーションスキーマ
 */
export const chatMessageSchema = z.object({
	message: z
		.string()
		.trim()
		.min(1, 'メッセージを入力してください')
		.max(5000, 'メッセージは5000文字以内で入力してください')
});

// 型推論用のエクスポート
export type CreateTodoInput = z.infer<typeof createTodoSchema>;
export type UpdateTodoInput = z.infer<typeof updateTodoSchema>;
export type ChatMessageInput = z.infer<typeof chatMessageSchema>;
