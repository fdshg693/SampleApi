using System.Text.Json;
using SampleApi.Models;
using StackExchange.Redis;

namespace SampleApi.Services;

/// <summary>
/// Redis-backed chat history cache. Stores serialized message arrays per sessionId with TTL.
/// </summary>
public class ChatCacheService
{
    private readonly IDatabase _redis;
    private readonly string _keyPrefix;
    private readonly TimeSpan _expiration;

    public ChatCacheService(RedisConnectionService redisConnection, IConfiguration configuration)
    {
        _redis = redisConnection.GetDatabase();
        _keyPrefix = configuration["Redis:InstanceName"] ?? "SampleApi:";
        // default 24h if not configured
        var hours = 24;
        if (int.TryParse(configuration["ChatCache:Hours"], out var h) && h > 0)
        {
            hours = h;
        }
        _expiration = TimeSpan.FromHours(hours);
    }

    private string Key(string sessionId) => $"{_keyPrefix}chat:{sessionId}";

    /// <summary>
    /// Save chat history for a session. Optionally cap at maxCount (drops oldest if exceeded).
    /// </summary>
    public async Task SaveChatHistoryAsync(string sessionId, IReadOnlyList<ChatMessage> messages, int maxCount = 100)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return;

        List<ChatMessage> trimmed;
        if (messages.Count > maxCount)
        {
            // keep last maxCount messages
            trimmed = messages.Skip(messages.Count - maxCount).ToList();
        }
        else
        {
            trimmed = messages.ToList();
        }

        var json = JsonSerializer.Serialize(trimmed);
        await _redis.StringSetAsync(Key(sessionId), json, _expiration);
    }

    /// <summary>
    /// Get chat history for a session; null if not found.
    /// </summary>
    public async Task<List<ChatMessage>?> GetChatHistoryAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;
        var json = await _redis.StringGetAsync(Key(sessionId));
        if (json.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<List<ChatMessage>>(json!) ?? new List<ChatMessage>();
    }
}
