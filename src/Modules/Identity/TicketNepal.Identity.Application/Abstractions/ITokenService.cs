namespace TicketNepal.Identity.Application.Abstractions;

public sealed record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn);

public interface ITokenService
{
    TimeSpan AccessTokenLifetime { get; }
    TimeSpan RefreshTokenLifetime { get; }

    string CreateAccessToken(Guid userId, string email, IEnumerable<string> roles);

    (string Raw, string Hash) GenerateRefreshToken();

    string HashRefreshToken(string raw);
}