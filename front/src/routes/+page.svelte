<script lang="ts">
	import { onMount } from 'svelte';
	import { health } from '$lib/api';
	import { createChat } from '$lib/generated/chat/chat';
	import type { ChatMessage } from '$lib/generated/models';
	import { chatMessageSchema } from '$lib/schemas';
	import { ZodError } from 'zod';

	let messages = $state<ChatMessage[]>([]);
	let input = $state('');
	let error: string | null = $state(null);
	let validationError: string | null = $state(null);
	let isStub = $state(false);

	// TanStack Query mutation for sending chat messages
	const chatMutation = createChat({
		mutation: {
			onSuccess: (response: any) => {
				// Extract the response data
				const data = response?.data;
				if (data?.reply) {
					messages = [...messages, { role: 'assistant', content: data.reply }];
					isStub = data.isStub ?? false;
				}
				error = null;
				validationError = null;
			},
			onError: (err: any) => {
				error = err?.message ?? 'Failed to send message';
			}
		}
	});

	onMount(async () => {
		try {
			const h = await health();
			console.log('Health', h);
		} catch (e) {
			error = 'Backend is not reachable. Start the API server.';
		}
	});

	function send() {
		if (chatMutation.isPending) return;
		
		// Zodでバリデーション
		try {
			const validated = chatMessageSchema.parse({ message: input });
			
			// バリデーション成功: エラーをクリアして送信
			validationError = null;
			
			const userMsg: ChatMessage = { role: 'user', content: validated.message };
			messages = [...messages, userMsg];
			input = '';
			
			// Send all messages including the new user message
			chatMutation.mutate({
				data: {
					messages: messages
				}
			});
		} catch (err) {
			// バリデーションエラー: エラーメッセージを表示
			if (err instanceof ZodError) {
				validationError = err.errors[0]?.message || 'Invalid input';
			}
		}
	}
</script>

<svelte:head>
	<title>Chat</title>
</svelte:head>

<div class="app">
	<header>
		<div class="header-content">
			<div class="title-section">
				<h1>AI Chat</h1>
				{#if isStub}
					<span class="tag">stub</span>
				{/if}
			</div>
			<a href="/todos" class="nav-link">📝 TODOs</a>
		</div>
	</header>

	<section class="chat">
		{#if messages.length === 0}
			<p class="empty">Ask me anything to get started.</p>
		{/if}
		{#each messages as m, i}
			<div class={`msg ${m.role}`}>
				<div class="bubble">{m.content}</div>
			</div>
		{/each}
	</section>

	{#if error}
		<div class="error">{error}</div>
	{/if}

	{#if validationError}
		<div class="validation-error">{validationError}</div>
	{/if}

	<form class="input" onsubmit={send}>
		<input
			placeholder="Type a message..."
			bind:value={input}
			autocomplete="off"
			class:error={validationError}
		/>
		<button disabled={chatMutation.isPending}>
			{chatMutation.isPending ? 'Sending…' : 'Send'}
		</button>
	</form>
</div>

<style>
	:global(body) {
		margin: 0;
		font-family: system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial, "Apple Color Emoji", "Segoe UI Emoji";
		background: #0b1120;
		color: #e5e7eb;
	}
	.app { max-width: 900px; margin: 0 auto; padding: 1rem; }
	header { margin-bottom: 1rem; }
	.header-content { display: flex; align-items: center; justify-content: space-between; }
	.title-section { display: flex; align-items: center; gap: .5rem; }
	header h1 { font-size: 1.25rem; margin: 0; }
	.tag { font-size: .75rem; padding: .125rem .4rem; border: 1px solid #4b5563; border-radius: 4px; color: #9ca3af; }
	.nav-link { color: #60a5fa; text-decoration: none; padding: .5rem .75rem; border-radius: 8px; border: 1px solid #374151; background: #1f2937; transition: all 0.2s; }
	.nav-link:hover { background: #374151; }
	.chat { display: grid; gap: .5rem; margin: 1rem 0; }
	.msg { display: flex; }
	.msg.user { justify-content: flex-end; }
	.msg.assistant { justify-content: flex-start; }
	.bubble { max-width: 80%; padding: .6rem .75rem; border-radius: 10px; line-height: 1.35; white-space: pre-wrap; }
	.msg.user .bubble { background: #1f2937; }
	.msg.assistant .bubble { background: #111827; border: 1px solid #374151; }
	.empty { opacity: .7; }
	.input { display: flex; gap: .5rem; }
	.input input { flex: 1; padding: .6rem .75rem; border-radius: 8px; border: 1px solid #374151; background: #0f172a; color: #e5e7eb; }
	.input input.error { border-color: #ef4444; }
	.input button { padding: .6rem .9rem; border-radius: 8px; border: 1px solid #374151; background: #1f2937; color: #e5e7eb; cursor: pointer; }
	.input button[disabled] { opacity: .6; cursor: not-allowed; }
	.error { color: #fecaca; background: #7f1d1d; border: 1px solid #991b1b; padding: .5rem .75rem; border-radius: 8px; }
	.validation-error { color: #fca5a5; font-size: 0.875rem; margin: 0.5rem 0; }
</style>
