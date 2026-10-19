using TicketNepal.Identity.Domain;

namespace TicketNepal.Identity.Tests;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private static RefreshToken NewToken() => 
        RefreshToken.Create(Guid.NewGuid(), Guid.NewGuid(), "hash", Now, Lifetime, "127.0.0.1");
    
    [Fact]
    public void New_token_is_active_until_it_expires()
    {
        var token = NewToken();

        Assert.True(token.IsActive(Now.AddDays(6)));
        Assert.False(token.IsActive(Now.AddDays(7)));
    }

    [Fact]
    public void Revoked_token_is_not_active_and_records_its_replacement()
    {
        var token = NewToken();

        token.Revoke(Now.AddMinutes(5), "next-hash");

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(Now.AddMinutes(6)));
        Assert.Equal("next-hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void Revoking_twice_keeps_the_first_revocation()
    {
        var token = NewToken();
        token.Revoke(Now.AddMinutes(5), "first");

        token.Revoke(Now.AddMinutes(10), "second");

        Assert.Equal(Now.AddMinutes(5), token.RevokedAt);
        Assert.Equal("first", token.ReplacedByTokenHash);
    }
}