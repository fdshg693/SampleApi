using StackExchange.Redis;

namespace SampleApi.Services;

/// <summary>
/// Redisへの接続を管理するシングルトンサービス
/// StackExchange.Redisの推奨パターン: 1アプリケーション = 1接続
/// </summary>
public class RedisConnectionService : IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly ILogger<RedisConnectionService> _logger;

    public RedisConnectionService(IConfiguration configuration, ILogger<RedisConnectionService> logger)
    {
        _logger = logger;
        var connectionString = configuration["Redis:ConnectionString"] 
            ?? throw new InvalidOperationException("Redis:ConnectionString is not configured");

        _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            try
            {
                _logger.LogInformation("Connecting to Redis: {ConnectionString}", 
                    connectionString.Split(',')[0]); // Hide credentials
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        });
    }

    public IConnectionMultiplexer Connection => _connection.Value;
    
    public IDatabase GetDatabase(int db = -1) => Connection.GetDatabase(db);

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
