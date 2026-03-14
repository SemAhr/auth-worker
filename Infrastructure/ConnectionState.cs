namespace Auth_Worker.Infrastructure;

public sealed class ConnectivityState
{
    public bool RedisIsHealthy { get; set; }
    public bool PostgresIsHealthy { get; set; }
    public DateTimeOffset LastCheckAt { get; set; }
}
