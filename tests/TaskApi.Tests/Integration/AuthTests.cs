using System.Net;
using System.Net.Http.Json;
using TaskApi.Dtos;

namespace TaskApi.Tests.Integration;

public sealed class AuthTests : IAsyncLifetime
{
    private readonly TaskApiFactory _factory = new();
    private HttpClient _anonymous = null!;

    public async Task InitializeAsync() => _anonymous = await _factory.CreateClientWithDatabaseAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/1")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/1")]
    [InlineData("DELETE", "/api/tasks/1")]
    public async Task TaskEndpoints_WithoutToken_Return401ProblemDetails(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new UpdateTaskDto("x", null, false));
        }

        var response = await _anonymous.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_DoesNotRequireAuth()
    {
        var response = await _anonymous.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await _factory.CreateAuthenticatedClientAsync("carol@example.com");

        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email = "carol@example.com", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new { email = "dave@example.com", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Users_OnlySeeAndChangeTheirOwnTasks()
    {
        var alice = await _factory.CreateAuthenticatedClientAsync("alice@example.com");
        var bob = await _factory.CreateAuthenticatedClientAsync("bob@example.com");

        var created = await alice.PostAsJsonAsync("/api/tasks", new CreateTaskDto("Alice's task", null));
        var aliceTask = (await created.Content.ReadFromJsonAsync<TaskDto>())!;
        await bob.PostAsJsonAsync("/api/tasks", new CreateTaskDto("Bob's task", null));

        var aliceList = await alice.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks");
        var bobList = await bob.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks");

        Assert.Equal("Alice's task", Assert.Single(aliceList!.Items).Title);
        Assert.Equal("Bob's task", Assert.Single(bobList!.Items).Title);

        // Another user's task looks like it does not exist.
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/tasks/{aliceTask.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PutAsJsonAsync($"/api/tasks/{aliceTask.Id}", new UpdateTaskDto("Hacked", null, true))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/tasks/{aliceTask.Id}")).StatusCode);

        var stillThere = await alice.GetFromJsonAsync<TaskDto>($"/api/tasks/{aliceTask.Id}");
        Assert.Equal("Alice's task", stillThere!.Title);
    }
}
