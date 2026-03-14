using Auth_Worker.Domains.Records;

namespace Auth_Worker.Application.Postgres;

public interface IPostgresService
{
    Task<bool> CheckConnectionAsync(CancellationToken cancellationToken);
    Task UpdateSessionsAsync(IReadOnlyList<Session> sessions, CancellationToken cancellationToken = default);
}
