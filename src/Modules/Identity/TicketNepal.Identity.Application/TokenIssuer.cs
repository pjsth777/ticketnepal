using TicketNepal.Identity.Application.Abstractions;
using TicketNepal.Identity.Domain;

namespace TicketNepal.Identity.Application;

public sealed class TokenIssuer(ITokenService tokens, IRefreshTokenRepository refreshTokens, TimeProvider clock)
{
    public AuthResponse Issue(
        ApplicationUser user, IEnumerable<string> roles, Guid familyId,
        string? ipAddress, RefreshToken? rotatedFrom = null)
    {
        var now = clock.GetUtcNow();
        var (raw, hash) = tokens.GenerateRefreshToken();

        refreshTokens.Add(RefreshToken.Create(user.Id, familyId, hash, now, tokens.RefreshTokenLifetime, ipAddress));
        rotatedFrom?.Revoke(now, hash);

        var accessToken = tokens.CreateAccessToken(user.Id, user.Email!, roles);
        return new AuthResponse(accessToken, raw, (int)tokens.AccessTokenLifetime.TotalSeconds);
    }
}