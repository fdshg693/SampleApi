<script lang="ts">
	import { 
		createGetTodos, 
		createCreateTodo, 
		createUpdateTodo, 
		createDeleteTodo,
		getGetTodosQueryKey
	} from '$lib/generated/todos/todos';
	import type { TodoItem } from '$lib/api';
	import { useQueryClient } from '@tanstack/svelte-query';
	import { createTodoSchema } from '$lib/schemas';
	import { ZodError } from 'zod';

	// 新規TODO作成フォーム
	let newTitle = $state('');
	let newDescription = $state('');
	
	// バリデーションエラー
	let validationErrors = $state<Record<string, string>>({});
	
	const queryClient = useQueryClient();

	// Query: TODOリストを取得（自動キャッシング・再フェッチ）
	const todosQuery = createGetTodos({
		query: {
			refetchOnMount: true,
			staleTime: 30 * 1000, // 30秒間はキャッシュを使用
		}
	});

	// Mutation: TODO作成
	const createMutation = createCreateTodo({
		mutation: {
			onSuccess: () => {
				// 成功したら TODO リストを再フェッチ
				queryClient.invalidateQueries({ queryKey: getGetTodosQueryKey() });
				newTitle = '';
				newDescription = '';
				validationErrors = {}; // エラーをクリア
			}
		}
	});

	// Mutation: TODO更新
	const updateMutation = createUpdateTodo({
		mutation: {
			onMutate: async (variables) => {
				// 楽観的更新: UIを即座に更新
				await queryClient.cancelQueries({ queryKey: getGetTodosQueryKey() });
				
				const previousTodos = queryClient.getQueryData(getGetTodosQueryKey());
				
				// 楽観的にキャッシュを更新
				queryClient.setQueryData(getGetTodosQueryKey(), (old: any) => {
					if (!old?.data?.todos) return old;
					return {
						...old,
						data: {
							...old.data,
							todos: old.data.todos.map((t: TodoItem) =>
								t.id === variables.id
									? { ...t, ...variables.data, updatedAt: new Date().toISOString() }
									: t
							)
						}
					};
				});
				
				return { previousTodos };
			},
			onError: (_err, _variables, context) => {
				// エラー時はロールバック
				if (context?.previousTodos) {
					queryClient.setQueryData(getGetTodosQueryKey(), context.previousTodos);
				}
			},
			onSettled: () => {
				// 完了後は再フェッチして同期
				queryClient.invalidateQueries({ queryKey: getGetTodosQueryKey() });
			}
		}
	});

	// Mutation: TODO削除
	const deleteMutation = createDeleteTodo({
		mutation: {
			onMutate: async (variables) => {
				// 楽観的更新
				await queryClient.cancelQueries({ queryKey: getGetTodosQueryKey() });
				
				const previousTodos = queryClient.getQueryData(getGetTodosQueryKey());
				
				queryClient.setQueryData(getGetTodosQueryKey(), (old: any) => {
					if (!old?.data?.todos) return old;
					return {
						...old,
						data: {
							...old.data,
							todos: old.data.todos.filter((t: TodoItem) => t.id !== variables.id)
						}
					};
				});
				
				return { previousTodos };
			},
			onError: (_err, _variables, context) => {
				if (context?.previousTodos) {
					queryClient.setQueryData(getGetTodosQueryKey(), context.previousTodos);
				}
			},
			onSettled: () => {
				queryClient.invalidateQueries({ queryKey: getGetTodosQueryKey() });
			}
		}
	});

	function handleCreate(e: Event) {
		e.preventDefault();
		if (createMutation.isPending) return;
		
		// Zodでバリデーション
		try {
			const validated = createTodoSchema.parse({
				title: newTitle,
				description: newDescription
			});
			
			// バリデーション成功: エラーをクリアして送信
			validationErrors = {};
			
			createMutation.mutate({
				data: {
					title: validated.title,
					description: validated.description || undefined
				}
			});
		} catch (err) {
			// バリデーションエラー: エラーメッセージを表示
			if (err instanceof ZodError) {
				const errors: Record<string, string> = {};
				err.errors.forEach((error) => {
					const path = error.path.join('.');
					errors[path] = error.message;
				});
				validationErrors = errors;
			}
		}
	}

	function toggleComplete(todo: TodoItem) {
		updateMutation.mutate({
			id: todo.id,
			data: {
				isCompleted: !todo.isCompleted
			}
		});
	}

	function handleDelete(id: string) {
		if (!confirm('Are you sure you want to delete this todo?')) return;
		deleteMutation.mutate({ id });
	}

	function formatDate(dateString: string): string {
		const date = new Date(dateString);
		return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
	}

	// Reactive derivations using $derived
	const todos = $derived((todosQuery.data as any)?.data?.todos || []);
	const loading = $derived(todosQuery.isLoading);
	const error = $derived(todosQuery.error || createMutation.error || updateMutation.error || deleteMutation.error);
</script>

<svelte:head>
	<title>TODO List</title>
</svelte:head>

<div class="app">
	<header>
		<div class="header-content">
			<h1>📝 TODO List</h1>
			<a href="/" class="nav-link">💬 Chat</a>
		</div>
	</header>

	{#if error}
		<div class="error">{(error as any)?.message ?? 'An error occurred'}</div>
	{/if}

	<section class="create-section">
		<h2>Create New TODO</h2>
		<form onsubmit={handleCreate}>
			<div class="form-group">
				<input
					type="text"
					placeholder="Title (required, max 200 characters)"
					bind:value={newTitle}
					maxlength="200"
					class:error={validationErrors['title']}
				/>
				{#if validationErrors['title']}
					<span class="error-message">{validationErrors['title']}</span>
				{/if}
			</div>
			<div class="form-group">
				<textarea
					placeholder="Description (optional, max 1000 characters)"
					bind:value={newDescription}
					maxlength="1000"
					rows="3"
					class:error={validationErrors['description']}
				></textarea>
				{#if validationErrors['description']}
					<span class="error-message">{validationErrors['description']}</span>
				{/if}
			</div>
			<button type="submit" disabled={createMutation.isPending}>
				{createMutation.isPending ? 'Creating...' : '➕ Add TODO'}
			</button>
		</form>
	</section>

	<section class="todos-section">
		<div class="section-header">
			<h2>Your TODOs</h2>
			<span class="count">{todos.length} {todos.length === 1 ? 'item' : 'items'}</span>
		</div>

		{#if loading}
			<p class="empty">Loading...</p>
		{:else if todos.length === 0}
			<p class="empty">No todos yet. Create one above!</p>
		{:else}
			<div class="todos-list">
				{#each todos as todo (todo.id)}
					<div class="todo-card" class:completed={todo.isCompleted}>
						<div class="todo-header">
							<label class="checkbox-wrapper">
								<input
									type="checkbox"
									checked={todo.isCompleted}
									onchange={() => toggleComplete(todo)}
								/>
								<span class="checkmark"></span>
							</label>
							<h3 class:completed-text={todo.isCompleted}>{todo.title}</h3>
							<button class="delete-btn" onclick={() => handleDelete(todo.id)} title="Delete">
								🗑️
							</button>
						</div>
						{#if todo.description}
							<p class="description">{todo.description}</p>
						{/if}
						<div class="todo-footer">
							<span class="timestamp">Created: {formatDate(todo.createdAt)}</span>
							{#if todo.updatedAt !== todo.createdAt}
								<span class="timestamp">Updated: {formatDate(todo.updatedAt)}</span>
							{/if}
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</section>
</div>

<style>
	:global(body) {
		margin: 0;
		font-family: system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial, "Apple Color Emoji", "Segoe UI Emoji";
		background: #0b1120;
		color: #e5e7eb;
	}

	.app {
		max-width: 900px;
		margin: 0 auto;
		padding: 1rem;
	}

	header {
		margin-bottom: 2rem;
	}

	.header-content {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	header h1 {
		font-size: 1.5rem;
		margin: 0;
	}

	.nav-link {
		color: #60a5fa;
		text-decoration: none;
		padding: .5rem .75rem;
		border-radius: 8px;
		border: 1px solid #374151;
		background: #1f2937;
		transition: all 0.2s;
	}

	.nav-link:hover {
		background: #374151;
	}

	.error {
		color: #fecaca;
		background: #7f1d1d;
		border: 1px solid #991b1b;
		padding: .75rem;
		border-radius: 8px;
		margin-bottom: 1rem;
	}

	.create-section {
		background: #111827;
		border: 1px solid #374151;
		border-radius: 12px;
		padding: 1.5rem;
		margin-bottom: 2rem;
	}

	.create-section h2 {
		font-size: 1.125rem;
		margin: 0 0 1rem 0;
	}

	.form-group {
		margin-bottom: 1rem;
	}

	.form-group input,
	.form-group textarea {
		width: 100%;
		padding: .75rem;
		border-radius: 8px;
		border: 1px solid #374151;
		background: #0f172a;
		color: #e5e7eb;
		font-family: inherit;
		font-size: 1rem;
		box-sizing: border-box;
	}

	.form-group textarea {
		resize: vertical;
	}

	.form-group input.error,
	.form-group textarea.error {
		border-color: #ef4444;
	}

	.error-message {
		display: block;
		color: #fca5a5;
		font-size: 0.875rem;
		margin-top: 0.25rem;
	}

	button[type="submit"] {
		width: 100%;
		padding: .75rem;
		border-radius: 8px;
		border: 1px solid #374151;
		background: #1f2937;
		color: #e5e7eb;
		cursor: pointer;
		font-size: 1rem;
		transition: all 0.2s;
	}

	button[type="submit"]:hover:not([disabled]) {
		background: #374151;
	}

	button[type="submit"][disabled] {
		opacity: .6;
		cursor: not-allowed;
	}

	.todos-section {
		margin-top: 2rem;
	}

	.section-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 1rem;
	}

	.section-header h2 {
		font-size: 1.25rem;
		margin: 0;
	}

	.count {
		font-size: .875rem;
		color: #9ca3af;
		background: #1f2937;
		padding: .25rem .75rem;
		border-radius: 12px;
		border: 1px solid #374151;
	}

	.empty {
		opacity: .7;
		text-align: center;
		padding: 2rem;
	}

	.todos-list {
		display: grid;
		gap: 1rem;
	}

	.todo-card {
		background: #111827;
		border: 1px solid #374151;
		border-radius: 12px;
		padding: 1.25rem;
		transition: all 0.2s;
	}

	.todo-card:hover {
		border-color: #4b5563;
	}

	.todo-card.completed {
		opacity: 0.7;
	}

	.todo-header {
		display: flex;
		align-items: flex-start;
		gap: .75rem;
		margin-bottom: .5rem;
	}

	.checkbox-wrapper {
		position: relative;
		display: flex;
		align-items: center;
		cursor: pointer;
		padding-top: .25rem;
	}

	.checkbox-wrapper input[type="checkbox"] {
		position: absolute;
		opacity: 0;
		cursor: pointer;
	}

	.checkmark {
		width: 20px;
		height: 20px;
		border: 2px solid #374151;
		border-radius: 4px;
		background: #0f172a;
		transition: all 0.2s;
	}

	.checkbox-wrapper:hover .checkmark {
		border-color: #60a5fa;
	}

	.checkbox-wrapper input:checked ~ .checkmark {
		background: #60a5fa;
		border-color: #60a5fa;
	}

	.checkbox-wrapper input:checked ~ .checkmark::after {
		content: '✓';
		position: absolute;
		color: white;
		font-size: 14px;
		font-weight: bold;
		left: 50%;
		top: 50%;
		transform: translate(-50%, -50%);
	}

	.todo-header h3 {
		flex: 1;
		margin: 0;
		font-size: 1.125rem;
		word-break: break-word;
	}

	.completed-text {
		text-decoration: line-through;
		opacity: 0.6;
	}

	.delete-btn {
		background: transparent;
		border: none;
		cursor: pointer;
		font-size: 1.25rem;
		padding: .25rem .5rem;
		border-radius: 6px;
		transition: all 0.2s;
		opacity: 0.6;
	}

	.delete-btn:hover {
		background: #7f1d1d;
		opacity: 1;
	}

	.description {
		margin: .75rem 0 .75rem 2rem;
		color: #9ca3af;
		line-height: 1.5;
		white-space: pre-wrap;
		word-break: break-word;
	}

	.todo-footer {
		display: flex;
		gap: 1rem;
		margin-top: .75rem;
		padding-top: .75rem;
		border-top: 1px solid #1f2937;
		flex-wrap: wrap;
	}

	.timestamp {
		font-size: .75rem;
		color: #6b7280;
	}

	@media (max-width: 640px) {
		.app {
			padding: .5rem;
		}

		.create-section,
		.todo-card {
			padding: 1rem;
		}

		.header-content {
			flex-direction: column;
			align-items: flex-start;
			gap: 1rem;
		}
	}
</style>
