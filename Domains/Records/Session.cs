namespace Auth_Worker.Domains.Records;

public record SessionDetails
(
    int IdleTimeout,
    int GraceWindow,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime LastActivityAt
);

public record Session
(
    Guid Id,
    string Username,
    SessionDetails Details
);
