using System.Text.Json.Serialization;

namespace SampleApi.Models;

// TODOアイテムの基本構造
public record TodoItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("isCompleted")] bool IsCompleted,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updatedAt")] DateTimeOffset UpdatedAt
);

// POST /api/todos リクエスト
public class CreateTodoRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

// PUT /api/todos/{id} リクエスト
public class UpdateTodoRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool? IsCompleted { get; set; }
}

// GET /api/todos レスポンス
public class GetTodosResponse
{
    [JsonPropertyName("todos")]
    public List<TodoItem> Todos { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
