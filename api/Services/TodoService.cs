using System.Text.Json;
using StackExchange.Redis;
using SampleApi.Models;

namespace SampleApi.Services;

/// <summary>
/// TODOのCRUD操作を管理するサービス（Redis版）
/// </summary>
public class TodoService
{
    private readonly IDatabase _redis;
    private readonly string _keyPrefix;
    private readonly string _allTodosKey;
    private readonly ILogger<TodoService> _logger;

    public TodoService(
        RedisConnectionService redisConnection,
        IConfiguration configuration,
        ILogger<TodoService> logger)
    {
        _redis = redisConnection.GetDatabase();
        _keyPrefix = configuration["Redis:InstanceName"] ?? "SampleApi:";
        _allTodosKey = $"{_keyPrefix}todos:all";
        _logger = logger;
    }

    /// <summary>
    /// 全TODOアイテムを取得
    /// </summary>
    public async Task<GetTodosResponse> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var todoIds = await _redis.SetMembersAsync(_allTodosKey);
            var todos = new List<TodoItem>();

            foreach (var id in todoIds)
            {
                var todo = await GetByIdAsync(id.ToString(), ct);
                if (todo != null)
                {
                    todos.Add(todo);
                }
            }

            var sorted = todos.OrderByDescending(t => t.CreatedAt).ToList();
            return new GetTodosResponse
            {
                Todos = sorted,
                Total = sorted.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all todos from Redis");
            throw;
        }
    }

    /// <summary>
    /// IDで特定のTODOアイテムを取得
    /// </summary>
    public async Task<TodoItem?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var key = $"{_keyPrefix}todo:{id}";
            var json = await _redis.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<TodoItem>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get todo {Id} from Redis", id);
            return null;
        }
    }

    /// <summary>
    /// 新規TODOアイテムを作成
    /// </summary>
    public async Task<TodoItem> CreateAsync(CreateTodoRequest request, CancellationToken ct = default)
    {
        try
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

            var key = $"{_keyPrefix}todo:{todo.Id}";
            var json = JsonSerializer.Serialize(todo);

            // トランザクション的な操作
            var tran = _redis.CreateTransaction();
            _ = tran.StringSetAsync(key, json);
            _ = tran.SetAddAsync(_allTodosKey, todo.Id);
            await tran.ExecuteAsync();

            _logger.LogInformation("Created todo {Id}: {Title}", todo.Id, todo.Title);
            return todo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create todo in Redis");
            throw;
        }
    }

    /// <summary>
    /// 既存のTODOアイテムを更新
    /// </summary>
    public async Task<TodoItem?> UpdateAsync(string id, UpdateTodoRequest request, CancellationToken ct = default)
    {
        try
        {
            var existing = await GetByIdAsync(id, ct);
            if (existing == null)
            {
                return null;
            }

            var updated = existing with
            {
                Title = request.Title ?? existing.Title,
                Description = request.Description ?? existing.Description,
                IsCompleted = request.IsCompleted ?? existing.IsCompleted,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var key = $"{_keyPrefix}todo:{id}";
            var json = JsonSerializer.Serialize(updated);
            await _redis.StringSetAsync(key, json);

            _logger.LogInformation("Updated todo {Id}", id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update todo {Id} in Redis", id);
            throw;
        }
    }

    /// <summary>
    /// TODOアイテムを削除
    /// </summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        try
        {
            var key = $"{_keyPrefix}todo:{id}";
            
            var tran = _redis.CreateTransaction();
            _ = tran.KeyDeleteAsync(key);
            _ = tran.SetRemoveAsync(_allTodosKey, id);
            var result = await tran.ExecuteAsync();

            if (result)
            {
                _logger.LogInformation("Deleted todo {Id}", id);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete todo {Id} from Redis", id);
            return false;
        }
    }
}
