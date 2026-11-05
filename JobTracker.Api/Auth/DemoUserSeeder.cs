using JobTracker.Core.Models;
using JobTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Auth;

public static class DemoUserSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoUserSeeder");
        var hasher = new PasswordHasher<User>();

        var demoUsers = new[]
        {
            (Username: config["DemoUsers:Admin:Username"] ?? "admin",
             Password: config["DemoUsers:Admin:Password"] ?? "Admin123!",
             Role: UserRole.Admin),
            (Username: config["DemoUsers:ReadOnly:Username"] ?? "demo",
             Password: config["DemoUsers:ReadOnly:Password"] ?? "Demo123!",
             Role: UserRole.ReadOnly)
        };

        foreach (var (username, password, role) in demoUsers)
        {
            if (await db.Users.AnyAsync(u => u.Username == username))
                continue;

            var user = new User { Username = username, Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);

            logger.LogInformation("Seeded demo user {Username} ({Role})", username, role);
        }

        await db.SaveChangesAsync();
    }
}