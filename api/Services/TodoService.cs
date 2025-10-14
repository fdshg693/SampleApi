using System.Collections.Concurrent;
using SampleApi.Models;

namespace SampleApi.Services;

/// <summary>
/// TODOのCRUD操作を管理するサービス
/// インメモリストア（ConcurrentDictionary）を使用
/// </summary>
public class TodoService
{
    private readonly ConcurrentDictionary<string, TodoItem> _todos = new();

    /// <summary>
    /// 全TODOアイテムを取得
    /// </summary>
    public Task<GetTodosResponse> GetAllAsync(CancellationToken ct = default)
    {
        var todos = _todos.Values.OrderByDescending(t => t.CreatedAt).ToList();
        var response = new GetTodosResponse
        {
            Todos = todos,
            Total = todos.Count
        };
        return Task.FromResult(response);
    }

    /// <summary>
    /// IDで特定のTODOアイテムを取得
    /// </summary>
    public Task<TodoItem?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        _todos.TryGetValue(id, out var todo);
        return Task.FromResult(todo);
    }

    /// <summary>
    /// 新規TODOアイテムを作成
    /// </summary>
    public Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var todo = new TodoItem(
            Id: Guid.NewGuid().ToString(),
            Title: request.Title,
            Description: request.Description,
            IsCompleted: false,
            CreatedAt: now,
            UpdatedAt: now
        );

        _todos[todo.Id] = todo;
        return Task.FromResult(todo);
    }

    /// <summary>
    /// 既存のTODOアイテムを更新
    /// </summary>
    public Task<TodoItem?> UpdateAsync(string id, UpdateTodoRequest request, CancellationToken ct = default)
    {
        if (!_todos.TryGetValue(id, out var existing))
        {
            return Task.FromResult<TodoItem?>(null);
        }

        var updated = existing with
        {
            Title = request.Title ?? existing.Title,
            Description = request.Description ?? existing.Description,
            IsCompleted = request.IsCompleted ?? existing.IsCompleted,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _todos[id] = updated;
        return Task.FromResult<TodoItem?>(updated);
    }

    /// <summary>
    /// TODOアイテムを削除
    /// </summary>
    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var removed = _todos.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}
