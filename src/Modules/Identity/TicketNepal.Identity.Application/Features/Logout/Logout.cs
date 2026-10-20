using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TicketNepal.Identity.Application.Abstractions;
using TicketNepal.SharedKernel;

namespace TicketNepal.Identity.Application.Features;

public static class Logout
{
    public sealed record Command(string RefreshToken) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() => RuleFor(x => x.RefreshToken).NotEmpty();
    }

    public sealed class Handler(
        ITokenService tokens, IRefreshTokenRepository refreshTokens, TimeProvider clock)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var stored = await refreshTokens.GetByHashAsync(
                tokens.HashRefreshToken(request.RefreshToken), cancellationToken);

            if (stored is not null)
                await refreshTokens.RevokeFamilyAsync(stored.FamilyId, clock.GetUtcNow(), cancellationToken);

            return Result.Success();
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder group) =>
        group.MapPost("/logout", async (Command command, ISender sender, CancellationToken ct) =>
        {
            await sender.Send(command, ct);
            return Results.NoContent();
        });
}