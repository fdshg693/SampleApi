/**
 * API Client - Centralized API calls for the frontend
 * 
 * This file now uses the auto-generated Orval client from OpenAPI spec.
 * To regenerate the client after backend API changes, run: pnpm generate:api
 */

// Re-export all generated types and API functions
export * from './generated';

// Import generated functions for wrapper compatibility
import { 
	chat as generatedChat,
	health as generatedHealth,
	getTodos as generatedGetTodos,
	createTodo as generatedCreateTodo,
	updateTodo as generatedUpdateTodo,
	deleteTodo as generatedDeleteTodo
} from './generated';

// Re-export types for backward compatibility
import type { 
	ChatMessage,
	ChatRequest,
	CreateTodoRequest,
	UpdateTodoRequest
} from './generated';

export type { ChatMessage, ChatRequest, CreateTodoRequest, UpdateTodoRequest };

// Legacy response types (for backward compatibility)
export type ChatResponse = { reply: string; isStub: boolean };

export type TodoItem = {
	id: string;
	title: string;
	description?: string;
	isCompleted: boolean;
	createdAt: string;
	updatedAt: string;
};

export type GetTodosResponse = {
	todos: TodoItem[];
	total: number;
};

// Wrapper functions that maintain backward compatibility with existing code
// These extract the data from the generated client's response format

export async function sendChat(request: ChatRequest, signal?: AbortSignal): Promise<ChatResponse> {
	const response = await generatedChat(request, { signal });
	// The generated client returns the full response; extract data as needed
	return response.data as any as ChatResponse;
}

export async function health(signal?: AbortSignal) {
	const response = await generatedHealth({ signal });
	return response.data as any as { status: string; time: string };
}

export async function getTodos(signal?: AbortSignal): Promise<GetTodosResponse> {
	const response = await generatedGetTodos({ signal });
	return response.data as any as GetTodosResponse;
}

export async function createTodo(request: CreateTodoRequest, signal?: AbortSignal): Promise<TodoItem> {
	const response = await generatedCreateTodo(request, { signal });
	return response.data as any as TodoItem;
}

export async function updateTodo(id: string, request: UpdateTodoRequest, signal?: AbortSignal): Promise<TodoItem> {
	const response = await generatedUpdateTodo(id, request, { signal });
	return response.data as any as TodoItem;
}

export async function deleteTodo(id: string, signal?: AbortSignal): Promise<void> {
	await generatedDeleteTodo(id, { signal });
}
