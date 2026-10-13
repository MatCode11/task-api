using Microsoft.EntityFrameworkCore;
using TaskApi.Models;

namespace TaskApi.Data;

public static class DevDataSeeder
{
    // Development only: apply pending migrations and add sample tasks to an empty database.
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (await db.Tasks.AnyAsync())
        {
            return;
        }

        db.Tasks.AddRange(
            new TaskItem { Title = "Learn ASP.NET Core", CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "Build a simple API", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
    }
}
