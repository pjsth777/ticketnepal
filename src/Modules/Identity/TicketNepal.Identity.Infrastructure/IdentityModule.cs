using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using TicketNepal.Application.Core;
using TicketNepal.Identity.Application;
using TicketNepal.Identity.Application.Abstractions;
using TicketNepal.Identity.Application.Features;
using TicketNepal.Identity.Infrastructure.Email;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketNepal.Identity.Domain;
using TicketNepal.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TicketNepal.Identity.Infrastructure.Security;
using TicketNepal.SharedKernel.Security;
using System.Security.Claims;

namespace TicketNepal.Identity.Infrastructure;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppIdentityDbContext>((sp, options) => options
            .UseNpgsql(
                sp.GetRequiredService<IConfiguration>().GetConnectionString("Default"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", AppIdentityDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddDataProtection()
            .SetApplicationName("TicketNepal")
            .PersistKeysToDbContext<AppIdentityDbContext>();

        services.AddIdentityCore<ApplicationUser>(options => 
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppIdentityDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddModuleApplication(typeof(IdentityErrors).Assembly);

        services.Configure<FrontendOptions>(configuration.GetSection("Frontend"));
        services.Configure<ResendOptions>(configuration.GetSection("Resend"));

        if (string.Equals(configuration["Email:Provider"], "Resend", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
            {
                var resend = sp.GetRequiredService<IOptions<ResendOptions>>().Value;
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resend.ApiKey);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ticketnepal/1.0"); // Resend rejects requests with no User-Agent
            });
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<TokenIssuer>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.RequireAdmin, policy => policy.RequireRole(Roles.Admin));

        return services;
    }

    public static async Task InitializeIdentityModuleAsync(this IServiceProvider rootServices)
    {
        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
        await db.Database.MigrateAsync();
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");
        Register.MapEndpoint(group);
        ConfirmEmail.MapEndpoint(group);
        Login.MapEndpoint(group);
        Refresh.MapEndpoint(group);
        Logout.MapEndpoint(group);

        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            id = user.FindFirst("sub")?.Value,
            email = user.FindFirst("email")?.Value,
            roles = user.FindAll("role").Select(c => c.Value)
        })).RequireAuthorization();

        return app;
    }
}