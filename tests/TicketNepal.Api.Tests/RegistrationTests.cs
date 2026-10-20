using System.Net;
using System.Net.Http.Json;

namespace TicketNepal.Api.Tests;

[Collection(ApiCollection.Name)]
public class RegistrationTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_creates_the_account_and_sends_a_confirmation_email()
    {
        var email = AuthTestHelpers.NewEmail();

        var response = await _client.RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains(factory.Emails.Sent, m => m.To == email);
    }

    [Fact]
    public async Task Registering_the_same_email_twice_returns_conflict()
    {
        var email = AuthTestHelpers.NewEmail();
        await _client.RegisterAsync(email);

        var second = await _client.RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Invalid_email_returns_a_validation_problem()
    {
        var response = await _client.RegisterAsync("not-an-email");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Password_that_breaks_the_identity_policy_is_rejected()
    {
        var response = await _client.RegisterAsync(AuthTestHelpers.NewEmail(), password: "alllowercase1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirming_email_succeeds_and_is_idempotent()
    {
        var email = AuthTestHelpers.NewEmail();
        await _client.RegisterAsync(email);

        var first = await _client.ConfirmEmailAsync(factory, email);
        var second = await _client.ConfirmEmailAsync(factory, email);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task Confirming_with_a_garbage_token_returns_bad_request()
    {
        var email = AuthTestHelpers.NewEmail();
        await _client.RegisterAsync(email);
        var (userId, _) = factory.Emails.GetConfirmation(email);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId, token = "garbage" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}