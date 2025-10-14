using System.Text.Json.Serialization;

namespace SampleApi.Models;

public record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public class ChatRequest
{
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    // Optional model name or Azure deployment name
    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("reply")]
    public string Reply { get; set; } = string.Empty;

    [JsonPropertyName("isStub")]
    public bool IsStub { get; set; }
}
