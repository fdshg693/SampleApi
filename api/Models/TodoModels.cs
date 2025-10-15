using System.ComponentModel.DataAnnotations;
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
    /// <summary>
    /// タイトル（1-200文字、必須）
    /// </summary>
    [JsonPropertyName("title")]
    [Required(ErrorMessage = "タイトルは必須です")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "タイトルは1〜200文字以内で入力してください")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 説明（0-1000文字、任意）
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(1000, ErrorMessage = "説明は1000文字以内で入力してください")]
    public string? Description { get; set; }
}

// PUT /api/todos/{id} リクエスト
public class UpdateTodoRequest
{
    /// <summary>
    /// タイトル（1-200文字、任意）
    /// </summary>
    [JsonPropertyName("title")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "タイトルは1〜200文字以内で入力してください")]
    public string? Title { get; set; }

    /// <summary>
    /// 説明（0-1000文字、任意）
    /// </summary>
    [JsonPropertyName("description")]
    [StringLength(1000, ErrorMessage = "説明は1000文字以内で入力してください")]
    public string? Description { get; set; }

    /// <summary>
    /// 完了状態
    /// </summary>
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
