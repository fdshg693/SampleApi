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
