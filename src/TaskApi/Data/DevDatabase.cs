using Microsoft.EntityFrameworkCore;

namespace TaskApi.Data;

public static class DevDatabase
{
    // Development only: apply pending migrations at startup so `dotnet run` just works.
    // Tasks belong to users, so there is no sample data; register a user to start (see TaskApi.http).
    public static async Task MigrateAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
    }
}
