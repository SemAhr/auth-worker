using System.Text.Json;
using Auth_Worker.Domains.Records;
using StackExchange.Redis;

namespace Auth_Worker.Application.Redis;

public sealed class RedisService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisService> logger) : IRedisService
{
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!connectionMultiplexer.IsConnected)
            {
                logger.LogWarning("Redis multiplexer is not connected.");
                return false;
            }

            var database = connectionMultiplexer.GetDatabase();
            var pingTime = await database.PingAsync();

            logger.LogInformation("Redis ping completed in {ElapsedMilliseconds} ms.", pingTime.TotalMilliseconds);

            return true;
        }
        catch (RedisConnectionException exception)
        {
            logger.LogError(exception, "Redis connection failed.");
            return false;
        }
        catch (RedisTimeoutException exception)
        {
            logger.LogError(exception, "Redis timeout while checking connection.");
            return false;
        }
    }

    public async Task<IReadOnlyList<Session>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        var database = connectionMultiplexer.GetDatabase();

        var endpoints = connectionMultiplexer.GetEndPoints();
        if (endpoints.Length == 0)
        {
            logger.LogWarning("No Redis endpoints were found.");
            return [];
        }

        var server = connectionMultiplexer.GetServer(endpoints[0]);

        if (!server.IsConnected)
        {
            logger.LogWarning("Redis server endpoint {Endpoint} is not connected.", endpoints[0]);
            return [];
        }

        var sessions = new List<Session>();

        foreach (var key in server.Keys(pattern: "*:idle", pageSize: 100))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var sessionData = await database.StringGetAsync(key);

            if (sessionData.IsNullOrEmpty)
            {
                continue;
            }

            try
            {
                var sessionJson = sessionData.ToString();
                var session = JsonSerializer.Deserialize<Session>(sessionJson);

                if (session is not null)
                {
                    sessions.Add(session);
                }
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Could not deserialize Redis key {RedisKey} into Session.", key);
            }
        }

        logger.LogInformation("Redis scan completed. Sessions found: {SessionCount}", sessions.Count);

        return sessions;
    }
}
