using System.Net;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketNepal.Application.Core.Http;
using TicketNepal.Identity.Application.Abstractions;
using TicketNepal.Identity.Domain;
using TicketNepal.SharedKernel;
using TicketNepal.SharedKernel.Security;

namespace TicketNepal.Identity.Application.Features;

public static class Register
{
    public sealed record Command(string Email, string Password, string FullName) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        UserManager<ApplicationUser> users,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontend,
        TimeProvider clock,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = new ApplicationUser
            {
                Id = Guid.CreateVersion7(),
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName.Trim(),
                CreatedAt = clock.GetUtcNow()
            };

            var created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded)
            {
                var duplicate = created.Errors.Any(e =>
                    e.Code is nameof(IdentityErrorDescriber.DuplicateEmail)
                           or nameof(IdentityErrorDescriber.DuplicateUserName));

                return duplicate
                    ? Result.Failure(IdentityErrors.EmailTaken)
                    : Result.Failure(Error.Validation(
                        "Auth.RegistrationFailed",
                        string.Join(" ", created.Errors.Select(e => e.Description))));
            }

            await users.AddToRoleAsync(user, Roles.Attendee);
            await SendConfirmationEmailAsync(user, cancellationToken);

            return Result.Success();
        }

        private async Task SendConfirmationEmailAsync(ApplicationUser user, CancellationToken ct)
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = $"{frontend.Value.BaseUrl.TrimEnd('/')}/confirm-email?userId={user.Id}&token={encoded}";

            var html = $"""
                <p>Hi {WebUtility.HtmlEncode(user.FullName)},</p>
                <p>Welcome to TicketNepal. Please <a href="{link}">confirm your email</a> to activate your account.</p>
                <p>This link expires in 24 hours.</p>
                """;

            try
            {
                await emailSender.SendAsync(
                    new EmailMessage(user.Email!, "Confirm your TicketNepal account", html), ct);
            }
            catch (Exception ex)
            {
                // The account exists; a failed email must not turn into a failed registration.
                logger.LogError(ex, "Failed to send confirmation email to user {UserId}", user.Id);
            }
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder group) =>
        group.MapPost("/register", async (Command command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.StatusCode(StatusCodes.Status201Created) : result.ToProblem();
        });
}