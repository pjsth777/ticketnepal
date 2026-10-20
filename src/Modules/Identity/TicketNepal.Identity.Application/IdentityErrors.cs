using TicketNepal.SharedKernel;

namespace TicketNepal.Identity.Application;

public static class IdentityErrors
{
    public static readonly Error EmailTaken = 
        Error.Conflict("Auth.EmailTaken", "An account with this email already exists");

    public static readonly Error InvalidConfirmationToken = 
        Error.Validation("Auth.InvalidConfirmationToken", "The confirmation link is invalidor has expired.");

     public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Email or password is incorrect.");

    public static readonly Error EmailNotConfirmed =
        Error.Forbidden("Auth.EmailNotConfirmed", "Please confirm your email before signing in.");

    public static readonly Error LockedOut =
        Error.Forbidden("Auth.LockedOut", "Too many failed attempts. Try again later.");

    public static readonly Error AccountDisabled =
        Error.Forbidden("Auth.AccountDisabled", "This account has been disabled.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "The session is invalid or has expired.");

    public static readonly Error RefreshTokenReused =
        Error.Unauthorized("Auth.RefreshTokenReused", "This session is no longer valid. Please sign in again.");
}