using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        // Seed roles
        var roles = new[] { "User", "Admin", "SuperAdmin" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }

        // Seed default SuperAdmin
        const string adminEmail = "admin@retromask.com";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Super",
                LastName = "Admin",
                DisplayName = "Super Admin",
                Role = UserRole.SuperAdmin,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
                logger.LogInformation("Seeded SuperAdmin user: {Email}", adminEmail);
            }
            else
            {
                logger.LogWarning("Failed to seed SuperAdmin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
