using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketNepal.Identity.Domain;
using TicketNepal.SharedKernel.Security;

namespace TicketNepal.Identity.Infrastructure;

internal static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var config = services.GetRequiredService<IConfiguration>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var clock = services.GetRequiredService<TimeProvider>();

        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<Guid>(role));
        }

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("No admin seed configured; skipping.");
            return;
        }

        if (await users.FindByEmailAsync(email) is not null)
            return;

        var admin = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Platform Admin",
            CreatedAt = clock.GetUtcNow()
        };

        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded)
            throw new InvalidOperationException(
                $"Admin seed failed: {string.Join("; ", created.Errors.Select(e => e.Description))}");

        await users.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("Seeded admin user {Email}", email);
    }
}