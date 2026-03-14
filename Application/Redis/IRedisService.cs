using Auth_Worker.Domains.Records;

namespace Auth_Worker.Application.Redis;

public interface IRedisService
{
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Session>> GetSessionsAsync(CancellationToken cancellationToken = default);
}
