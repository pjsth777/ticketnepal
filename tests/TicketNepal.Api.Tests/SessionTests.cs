using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TicketNepal.Api.Tests;

[Collection(ApiCollection.Name)]
public class SessionTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed record Tokens(string AccessToken, string RefreshToken, int ExpiresIn);

    private async Task<string> RegisterConfirmedUserAsync()
    {
        var email = AuthTestHelpers.NewEmail();
        await _client.RegisterAsync(email);
        await _client.ConfirmEmailAsync(factory, email);
        return email;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password = AuthTestHelpers.Password) =>
        _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

    [Fact]
    public async Task Unconfirmed_user_cannot_log_in()
    {
        var email = AuthTestHelpers.NewEmail();
        await _client.RegisterAsync(email);

        var response = await LoginAsync(email);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_returns_unauthorized()
    {
        var email = await RegisterConfirmedUserAsync();

        var response = await LoginAsync(email, "Wrong-Passw0rd");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_tokens_that_open_a_protected_endpoint()
    {
        var email = await RegisterConfirmedUserAsync();

        var login = await LoginAsync(email);
        var tokens = (await login.Content.ReadFromJsonAsync<Tokens>())!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var me = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Contains("Attendee", body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Protected_endpoint_rejects_anonymous_requests()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_revokes_the_whole_session()
    {
        var email = await RegisterConfirmedUserAsync();
        var first = (await (await LoginAsync(email)).Content.ReadFromJsonAsync<Tokens>())!;

        // 1. Normal rotation works and hands back a different refresh token.
        var rotated = await RefreshAsync(first.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var second = (await rotated.Content.ReadFromJsonAsync<Tokens>())!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        // 2. Replaying the already-rotated token is treated as theft...
        var replay = await RefreshAsync(first.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // 3. ...and the legitimate newest token in that family is now dead too.
        var afterTheft = await RefreshAsync(second.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_the_session()
    {
        var email = await RegisterConfirmedUserAsync();
        var tokens = (await (await LoginAsync(email)).Content.ReadFromJsonAsync<Tokens>())!;

        var logout = await _client.PostAsJsonAsync("/api/v1/auth/logout", new { tokens.RefreshToken });
        var refresh = await RefreshAsync(tokens.RefreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account()
    {
        var email = await RegisterConfirmedUserAsync();

        for (var i = 0; i < 5; i++)
            await LoginAsync(email, "Wrong-Passw0rd");

        var response = await LoginAsync(email); // even the correct password is refused now
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_admin_can_log_in_and_has_the_admin_role()
    {
        var login = await LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = (await login.Content.ReadFromJsonAsync<Tokens>())!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var body = await (await _client.SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Contains("Admin", body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Login_with_missing_fields_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}