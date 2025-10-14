using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SampleApi.Models;

namespace SampleApi.Services;

public class AiChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public AiChatService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public async Task<ChatResponse> GetChatCompletionAsync(ChatRequest request, CancellationToken ct)
    {
        // Try OpenAI
        var openAiKey = _config["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var openAiModel = request.Model ?? _config["OpenAI:Model"] ?? "gpt-4o-mini";

        try
        {
            if (!string.IsNullOrWhiteSpace(openAiKey))
            {
                var reply = await CallOpenAiAsync(openAiKey!, openAiModel, request.Messages, ct);
                return new ChatResponse { Reply = reply, IsStub = false };
            }
        }
        catch (Exception ex)
        {
            // Fallback to stub on error, include brief error hint (not sensitive)
            return new ChatResponse
            {
                Reply = $"(stub) Sorry, AI call failed: {ex.GetType().Name}. Here's a simple echo: " + LastUserMessage(request.Messages),
                IsStub = true
            };
        }

        // No keys configured: return stub echo
        return new ChatResponse
        {
            Reply = "(stub) No AI provider configured. Echo: " + LastUserMessage(request.Messages),
            IsStub = true
        };
    }

    private static string LastUserMessage(List<ChatMessage> messages)
        => messages.LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content
           ?? messages.LastOrDefault()?.Content
           ?? string.Empty;

    private async Task<string> CallOpenAiAsync(string apiKey, string model, List<ChatMessage> messages, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature = 0.3
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var res = await client.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
        res.EnsureSuccessStatusCode();
        using var stream = await res.Content.ReadAsStreamAsync(ct);

        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        var choice = root.GetProperty("choices")[0];
        var message = choice.GetProperty("message");
        var reply = message.GetProperty("content").GetString() ?? string.Empty;
        return reply.Trim();
    }

}
