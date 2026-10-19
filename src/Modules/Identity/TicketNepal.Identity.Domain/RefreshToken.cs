namespace TicketNepal.Identity.Domain;

/// <summary>
/// One link in a rotation chain. All tokens descended from a single login share a FamilyId,
/// so reuse of a rotated token lets us revoke exactly that session and nothing else.
/// </summary>

public sealed class RefreshToken
{
    private RefreshToken() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsRevoked => RevokedAt is not null;
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public static RefreshToken Create(
        Guid userId, Guid familyId, string tokenHash,
        DateTimeOffset now, TimeSpan lifetime, string? createdByIp) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        FamilyId = familyId,
        TokenHash = tokenHash,
        CreatedAt = now,
        ExpiresAt = now + lifetime,
        CreatedByIp = createdByIp
    };

    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        if (IsRevoked) return;
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}