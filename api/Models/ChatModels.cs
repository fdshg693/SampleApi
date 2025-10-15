using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SampleApi.Models;

public record ChatMessage(
    /// <summary>
    /// メッセージのロール (user, assistant, system)
    /// </summary>
    [property: JsonPropertyName("role")]
    [property: Required(ErrorMessage = "ロールは必須です")]
    [property: RegularExpression("^(user|assistant|system)$", ErrorMessage = "ロールは 'user', 'assistant', 'system' のいずれかである必要があります")]
    string Role,

    /// <summary>
    /// メッセージ内容（1-5000文字）
    /// </summary>
    [property: JsonPropertyName("content")]
    [property: Required(ErrorMessage = "メッセージ内容は必須です")]
    [property: StringLength(5000, MinimumLength = 1, ErrorMessage = "メッセージは1〜5000文字以内で入力してください")]
    string Content
);

public class ChatRequest
{
    /// <summary>
    /// チャットメッセージのリスト（最低1件必須）
    /// </summary>
    [JsonPropertyName("messages")]
    [Required(ErrorMessage = "メッセージは必須です")]
    [MinLength(1, ErrorMessage = "少なくとも1つのメッセージが必要です")]
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// モデル名またはAzureデプロイメント名（0-100文字、任意）
    /// </summary>
    [JsonPropertyName("model")]
    [StringLength(100, ErrorMessage = "モデル名は100文字以内で入力してください")]
    public string? Model { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("reply")]
    public string Reply { get; set; } = string.Empty;

    [JsonPropertyName("isStub")]
    public bool IsStub { get; set; }
}
