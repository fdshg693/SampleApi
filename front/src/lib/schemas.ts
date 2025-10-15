/**
 * Validation schemas - re-exports Zod schemas from generated code
 */

import { z } from 'zod';
import { chatBody } from './generated/chat/chat.zod';
import { createTodoBody, updateTodoBody } from './generated/todos/todos.zod';

// Chat validation schemas
export const chatMessageSchema = z.object({
	message: z.string().min(1, 'メッセージを入力してください').max(5000, 'メッセージは5000文字以内で入力してください')
});

export const chatRequestSchema = chatBody;

// Todo validation schemas
export const createTodoSchema = createTodoBody;
export const updateTodoSchema = updateTodoBody;

// Export individual field schemas for easier use
export const todoTitleSchema = z.string().min(1, 'タイトルを入力してください').max(200, 'タイトルは200文字以内で入力してください');
export const todoDescriptionSchema = z.string().max(1000, '説明は1000文字以内で入力してください').optional();
