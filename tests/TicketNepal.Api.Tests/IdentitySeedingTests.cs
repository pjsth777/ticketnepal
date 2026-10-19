using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TicketNepal.Identity.Domain;
using TicketNepal.SharedKernel.Security;

namespace TicketNepal.Api.Tests;

[Collection(ApiCollection.Name)]
public class IdentitySeedingTests(ApiFactory factory)
{
    [Fact]
    public async Task All_roles_are_seeded_on_startup()
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Roles.All)
            Assert.True(await roles.RoleExistsAsync(role), $"Missing role {role}");
    }

    [Fact]
    public async Task Admin_user_is_seeded_confirmed_and_in_admin_role()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await users.FindByEmailAsync(ApiFactory.AdminEmail);

        Assert.NotNull(admin);
        Assert.True(admin.EmailConfirmed);
        Assert.True(await users.IsInRoleAsync(admin, Roles.Admin));
    }
}