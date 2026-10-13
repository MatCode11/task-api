using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskApi.Data;

namespace TaskApi.Tests.Integration;

// Runs the real app against a private in-memory SQLite database.
// The open connection keeps the shared in-memory database alive for the factory's lifetime.
public sealed class TaskApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public TaskApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TaskDb", _connectionString);
    }

    public async Task<HttpClient> CreateClientWithDatabaseAsync()
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}

public static class AuthenticatedClientExtensions
{
    // Registers a new user through the Identity endpoints and returns a client that sends their bearer token.
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this WebApplicationFactory<Program> factory, string email)
    {
        const string password = "Test-Passw0rd";
        var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AccessTokenResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }
}
