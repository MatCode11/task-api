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
        var page = await _client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks");

        Assert.NotNull(page);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Fact]
    public async Task GetAll_PagesFiltersAndSorts()
    {
        for (var i = 1; i <= 5; i++)
        {
            await CreateTaskAsync($"Task {i}", i % 2 == 0 ? "about the API" : null);
        }

        var secondPage = await _client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks?page=2&pageSize=2");
        var searched = await _client.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks?search=api&sort=-id");

        Assert.Equal(5, secondPage!.TotalCount);
        Assert.Equal(["Task 3", "Task 4"], secondPage.Items.Select(t => t.Title));
        Assert.Equal(2, searched!.TotalCount);
        Assert.Equal(["Task 4", "Task 2"], searched.Items.Select(t => t.Title));
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("sort=priority")]
    [InlineData("isCompleted=maybe")]
    public async Task GetAll_InvalidQuery_Returns400(string queryString)
    {
        var response = await _client.GetAsync($"/api/tasks?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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
