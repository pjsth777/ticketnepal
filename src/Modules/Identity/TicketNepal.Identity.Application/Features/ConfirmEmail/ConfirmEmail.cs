using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using TicketNepal.Application.Core.Http;
using TicketNepal.Identity.Domain;
using TicketNepal.SharedKernel;

namespace TicketNepal.Identity.Application.Features;

public static class ConfirmEmail
{
    public sealed record Command(Guid UserId, string Token) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Token).NotEmpty();
        }
    }

    public sealed class Handler(UserManager<ApplicationUser> users) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = await users.FindByIdAsync(request.UserId.ToString());
            if (user is null)
                return Result.Failure(IdentityErrors.InvalidConfirmationToken);

            if (user.EmailConfirmed)
                return Result.Success(); // idempotent: clicking the link twice is fine

            string token;
            try
            {
                token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
            }
            catch (FormatException)
            {
                return Result.Failure(IdentityErrors.InvalidConfirmationToken);
            }

            var result = await users.ConfirmEmailAsync(user, token);
            return result.Succeeded
                ? Result.Success()
                : Result.Failure(IdentityErrors.InvalidConfirmationToken);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder group) =>
        group.MapPost("/confirm-email", async (Command command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });
}