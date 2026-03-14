using Auth_Worker.Application.Postgres;
using Auth_Worker.Application.Redis;

namespace auth_worker;

public sealed class Worker(ILogger<Worker> logger, IRedisService redisService, IPostgresService postgresService) : BackgroundService
{
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(20);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker started at: {Timestamp}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting new cycle at: {Timestamp}", DateTimeOffset.Now);

                var sessions = await redisService.GetSessionsAsync(stoppingToken);
                await postgresService.UpdateSessionsAsync(sessions, stoppingToken);

                logger.LogInformation("Cycle completed successfully. Sessions processed: {SessionCount}", sessions.Count);

                await Task.Delay(ProbeInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unexpected error in worker execution.");

                await Task.Delay(ProbeInterval, stoppingToken);
            }
        }

        logger.LogInformation("Worker stopped at: {Timestamp}", DateTimeOffset.Now);
    }
}
