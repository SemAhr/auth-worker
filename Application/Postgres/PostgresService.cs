using Auth_Worker.Domains.Records;
using Npgsql;
using NpgsqlTypes;

namespace Auth_Worker.Application.Postgres;

public sealed class PostgresService(NpgsqlDataSource dataSource, ILogger<PostgresService> logger) : IPostgresService
{
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select 1", connection);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var isHealthy = result is 1 or 1L;

            logger.LogInformation("PostgreSQL probe result: {IsHealthy}", isHealthy);

            return isHealthy;
        }
        catch (NpgsqlException exception)
        {
            logger.LogError(exception, "PostgreSQL connection failed.");
            return false;
        }
        catch (TimeoutException exception)
        {
            logger.LogError(exception, "PostgreSQL timeout while checking connection.");
            return false;
        }
    }

    public async Task UpdateSessionsAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken = default)
    {
        if (sessions.Count == 0)
        {
            logger.LogInformation("No sessions were received to persist.");
            return;
        }

        var distinctSessions = sessions
            .GroupBy(session => session.Id)
            .Select(group => group
                .OrderByDescending(item => item.Details.LastActivityAt)
                .First())
            .ToList();

        const string updateLastActivitySql =
            """
                UPDATE sessions
                SET last_activity_at = @last_activity_at
                WHERE id = @session_id
                  AND last_activity_at is distinct from @last_activity_at;
                """;


        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var batch = new NpgsqlBatch(connection, transaction);

            foreach (var session in distinctSessions)
            {
                var batchCommand = new NpgsqlBatchCommand(updateLastActivitySql);

                batchCommand.Parameters.Add(new NpgsqlParameter("session_id", NpgsqlDbType.Uuid)
                {
                    Value = session.Id
                });

                batchCommand.Parameters.Add(new NpgsqlParameter("last_activity_at", NpgsqlDbType.TimestampTz)
                {
                    Value = session.Details.LastActivityAt
                });

                batch.BatchCommands.Add(batchCommand);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Sessions persisted successfully. Received: {ReceivedCount}. Distinct processed: {ProcessedCount}",
                sessions.Count,
                distinctSessions.Count);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);

            logger.LogError(
                exception,
                "An error occurred while persisting sessions into PostgreSQL.");

            throw;
        }
    }
}
