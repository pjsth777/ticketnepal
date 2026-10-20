using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using TicketNepal.Application.Core.Http;
using TicketNepal.Identity.Application.Abstractions;
using TicketNepal.Identity.Domain;
using TicketNepal.SharedKernel;

namespace TicketNepal.Identity.Application.Features;

public static class Login
{
    public sealed record Request(string Email, string Password);

    public sealed record Command(string Email, string Password, string? IpAddress)
        : IRequest<Result<AuthResponse>>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class Handler(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        TokenIssuer issuer,
        IRefreshTokenRepository refreshTokens) : IRequestHandler<Command, Result<AuthResponse>>
    {
        public async Task<Result<AuthResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await users.FindByEmailAsync(request.Email);
            if (user is null)
                return Fail(IdentityErrors.InvalidCredentials);

            var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (check.IsLockedOut)
                return Fail(IdentityErrors.LockedOut);
            if (!check.Succeeded)
                return Fail(IdentityErrors.InvalidCredentials);

            // Checked only after the password is proven, so the state isn't leaked to guessers.
            if (!user.EmailConfirmed)
                return Fail(IdentityErrors.EmailNotConfirmed);
            if (!user.IsActive)
                return Fail(IdentityErrors.AccountDisabled);

            var roles = await users.GetRolesAsync(user);
            var response = issuer.Issue(user, roles, familyId: Guid.CreateVersion7(), request.IpAddress);
            await refreshTokens.SaveChangesAsync(cancellationToken);

            return Result.Success(response);
        }

        private static Result<AuthResponse> Fail(Error error) => Result.Failure<AuthResponse>(error);
    }

    public static void MapEndpoint(IEndpointRouteBuilder group) =>
        group.MapPost("/login", async (Request request, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new Command(request.Email, request.Password, http.Connection.RemoteIpAddress?.ToString()), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        });
}