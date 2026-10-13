using System.Net;

namespace TaskApi.Tests.Integration;

public sealed class HealthTests : IAsyncLifetime
{
    private readonly TaskApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await _factory.CreateClientWithDatabaseAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Health_ReportsHealthyWhenDatabaseIsReachable()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
