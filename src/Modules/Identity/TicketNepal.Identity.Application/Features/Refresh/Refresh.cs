using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using TicketNepal.Application.Core.Http;
using TicketNepal.Identity.Application.Abstractions;
using TicketNepal.Identity.Domain;
using TicketNepal.SharedKernel;

namespace TicketNepal.Identity.Application.Features;

public static class Refresh
{
    public sealed record Request(string RefreshToken);

    public sealed record Command(string RefreshToken, string? IpAddress) : IRequest<Result<AuthResponse>>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.RefreshToken).NotEmpty();
    }

    public sealed class Handler(
        ITokenService tokens,
        IRefreshTokenRepository refreshTokens,
        UserManager<ApplicationUser> users,
        TokenIssuer issuer,
        TimeProvider clock,
        ILogger<Handler> logger) : IRequestHandler<Command, Result<AuthResponse>>
    {
        public async Task<Result<AuthResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = clock.GetUtcNow();
            var stored = await refreshTokens.GetByHashAsync(
                tokens.HashRefreshToken(request.RefreshToken), cancellationToken);

            if (stored is null)
                return Fail(IdentityErrors.InvalidRefreshToken);

            if (stored.IsRevoked)
            {
                // A rotated or logged-out token is being replayed: assume theft, kill the whole session.
                await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
                logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}; session family {FamilyId} revoked",
                    stored.UserId, stored.FamilyId);
                return Fail(IdentityErrors.RefreshTokenReused);
            }

            if (stored.IsExpired(now))
                return Fail(IdentityErrors.InvalidRefreshToken);

            var user = await users.FindByIdAsync(stored.UserId.ToString());
            if (user is null || !user.IsActive)
            {
                await refreshTokens.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
                return Fail(IdentityErrors.InvalidRefreshToken);
            }

            var roles = await users.GetRolesAsync(user);
            var response = issuer.Issue(user, roles, stored.FamilyId, request.IpAddress, rotatedFrom: stored);
            await refreshTokens.SaveChangesAsync(cancellationToken);

            return Result.Success(response);
        }

        private static Result<AuthResponse> Fail(Error error) => Result.Failure<AuthResponse>(error);
    }

    public static void MapEndpoint(IEndpointRouteBuilder group) =>
        group.MapPost("/refresh", async (Request request, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new Command(request.RefreshToken, http.Connection.RemoteIpAddress?.ToString()), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        });
}