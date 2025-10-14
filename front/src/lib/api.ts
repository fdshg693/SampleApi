export type ChatMessage = { role: 'user' | 'assistant' | 'system'; content: string };
export type ChatRequest = { messages: ChatMessage[]; model?: string };
export type ChatResponse = { reply: string; isStub: boolean };

const BASE = '';

export async function sendChat(request: ChatRequest, signal?: AbortSignal): Promise<ChatResponse> {
	const res = await fetch(`${BASE}/api/chat`, {
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

export async function health(signal?: AbortSignal) {
	const res = await fetch(`${BASE}/api/health`, { signal });
	if (!res.ok) throw new Error(`Health check failed: ${res.status}`);
	return res.json() as Promise<{ status: string; time: string }>; 
}

// TODO API types
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
