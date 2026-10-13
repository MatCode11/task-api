using System.Net;
using System.Net.Http.Json;
using TaskApi.Dtos;

namespace TaskApi.Tests.Integration;

public sealed class TasksEndpointsTests : IAsyncLifetime
{
    private readonly TaskApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await _factory.CreateClientWithDatabaseAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private async Task<TaskDto> CreateTaskAsync(string title, string? description = null)
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskDto(title, description));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskDto>())!;
    }

    [Fact]
    public async Task GetAll_StartsEmpty()
    {
        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>("/api/tasks");

        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task Post_CreatesTaskAndReturnsLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskDto("Ship it", "soon"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal("Ship it", created!.Title);
        Assert.EndsWith($"/api/tasks/{created.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Post_EmptyTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskDto("  ", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsTaskOr404()
    {
        var created = await CreateTaskAsync("Find me");

        var found = await _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}");
        var missing = await _client.GetAsync("/api/tasks/9999");

        Assert.Equal("Find me", found!.Title);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Put_UpdatesTaskOr404()
    {
        var created = await CreateTaskAsync("Before");

        var response = await _client.PutAsJsonAsync($"/api/tasks/{created.Id}", new UpdateTaskDto("After", null, true));
        var missing = await _client.PutAsJsonAsync("/api/tasks/9999", new UpdateTaskDto("After", null, true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<TaskDto>();
        Assert.Equal("After", updated!.Title);
        Assert.True(updated.IsCompleted);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTaskThen404()
    {
        var created = await CreateTaskAsync("Temporary");

        var first = await _client.DeleteAsync($"/api/tasks/{created.Id}");
        var second = await _client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }
}
