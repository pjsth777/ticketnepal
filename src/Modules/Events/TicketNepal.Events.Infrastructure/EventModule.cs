using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketNepal.Events.Application.Features.ListEvents;


namespace TicketNepal.Events.Infrastructure;

public static class EventsModule
{
    public static IServiceCollection AddEventsModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapEventsModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1");
        ListEventsEndpoint.Map(group);
        return app;
    }
}