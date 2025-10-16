using StackExchange.Redis;

namespace SampleApi.Services;

/// <summary>
/// Simple sliding-window rate limiter backed by Redis SortedSet.
/// </summary>
public class RateLimitService
{
    private readonly IDatabase _redis;
    private readonly string _keyPrefix;

    public RateLimitService(RedisConnectionService redisConnection, IConfiguration configuration)
    {
        _redis = redisConnection.GetDatabase();
        _keyPrefix = configuration["Redis:InstanceName"] ?? "SampleApi:";
    }

    private string Key(string id) => $"{_keyPrefix}ratelimit:{id}";

    /// <summary>
    /// Returns true when allowed, false when rate limit exceeded.
    /// </summary>
    public async Task<bool> IsAllowedAsync(string clientId, int maxRequests, TimeSpan window)
    {
        if (maxRequests <= 0) return true; // disabled

        var key = Key(clientId);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (long)window.TotalSeconds;

        // Use a batch to pipeline operations
        var batch = _redis.CreateBatch();
        var removeTask = batch.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
        var lengthTask = batch.SortedSetLengthAsync(key);
        batch.Execute();
        await Task.WhenAll(removeTask, lengthTask);

        var count = await lengthTask;
        if (count >= maxRequests)
        {
            return false;
        }

        // record request and set expiry
        var tx = _redis.CreateTransaction();
        _ = tx.SortedSetAddAsync(key, Guid.NewGuid().ToString("N"), now);
        _ = tx.KeyExpireAsync(key, window);
        await tx.ExecuteAsync();
        return true;
    }
}
