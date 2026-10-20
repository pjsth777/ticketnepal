using System.Net.Http.Json;

namespace TicketNepal.Api.Tests;

internal static class AuthTestHelpers
{
    public const string Password = "Passw0rd!x";

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/v1/auth/register", new { email, password, fullName = "Test User" });

    public static Task<HttpResponseMessage> ConfirmEmailAsync(this HttpClient client, ApiFactory factory, string email)
    {
        var (userId, token) = factory.Emails.GetConfirmation(email);
        return client.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId, token });
    }
}