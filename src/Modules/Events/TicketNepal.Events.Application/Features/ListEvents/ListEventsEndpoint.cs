using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TicketNepal.Events.Application.Features.ListEvents;

public sealed record ListEventsResponse(IReadOnlyList<object> Items, int Page, int PageSize, int Total);

public static class ListEventsEndpoint
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/events", () => Results.Ok(new ListEventsResponse([], 1, 20, 0)))
                .WithName("ListEvents");
}