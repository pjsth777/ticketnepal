using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicketNepal.Api.Tests;

[Collection(ApiCollection.Name)]
public class WalkingSkeletonTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Liveness_endpoint_returns_200()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Events_endpoint_returns_an_empty_first_page()
    {
        var response = await _client.GetAsync("/api/v1/events");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<EventsPage>();

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(1, page.Page);
        Assert.Equal(0, page.Total);
    }

    private sealed record EventsPage(List<object> Items, int Page, int PageSize, int Total);
}