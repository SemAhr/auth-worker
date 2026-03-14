using Auth_Worker.Application.Postgres;
using Auth_Worker.Application.Redis;
using Auth_Worker.Infrastructure;

namespace Auth_Worker;

public sealed class Worker(ILogger<Worker> logger, RedisService redis, PostgresService postgres, ConnectivityState connectivityState) : BackgroundService
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started at: {Timestamp}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var redisIsHealthy = await redis.CheckConnectionAsync(stoppingToken);
                var postgresIsHealthy = await postgres.CheckConnectionAsync(stoppingToken);

                connectivityState.RedisIsHealthy = redisIsHealthy;
                connectivityState.PostgresIsHealthy = postgresIsHealthy;
                connectivityState.LastCheckAt = DateTimeOffset.Now;

                logger.LogInformation(
                    "Probe result | Redis: {RedisIsHealthy} | PostgreSQL: {PostgresIsHealthy} | Timestamp: {Timestamp}",
                    redisIsHealthy,
                    postgresIsHealthy,
                    connectivityState.LastCheckAt);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error while executing connectivity probes.");
            }

            await Task.Delay(ProbeInterval, stoppingToken);
        }

        logger.LogInformation("Worker stopped at: {Timestamp}", DateTimeOffset.Now);
    }
}
