/**
 * API wrapper - re-exports generated API functions and defines response types
 */

// Re-export generated API functions
export { health } from './generated/health/health';
export { chat } from './generated/chat/chat';
export { getTodos, createTodo, updateTodo, deleteTodo } from './generated/todos/todos';

// Re-export generated request types
export type { ChatMessage, ChatRequest, CreateTodoRequest, UpdateTodoRequest } from './generated/models';

// Define response types based on backend models
export interface TodoItem {
	id: string;
	title: string;
	description?: string | null;
	isCompleted: boolean;
	createdAt: string;
	updatedAt: string;
}

export interface GetTodosResponse {
	todos: TodoItem[];
	total: number;
}

export interface ChatResponse {
	reply: string;
	isStub: boolean;
}

export interface HealthResponse {
	status: string;
	time: string;
}
