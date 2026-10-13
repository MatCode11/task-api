using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskApi.Dtos;
using TaskApi.Services;

namespace TaskApi.Tests.Integration;

public sealed class ErrorResponseTests : IAsyncLifetime
{
    private readonly TaskApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await _factory.CreateClientWithDatabaseAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact]
    public async Task Post_WhitespaceTitle_ReturnsValidationProblemWithFieldError()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskDto("  ", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Title", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Post_TooLongFields_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/tasks", new CreateTaskDto(new string('a', 201), new string('b', 2001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Title", problem!.Errors.Keys);
        Assert.Contains("Description", problem.Errors.Keys);
    }

    [Fact]
    public async Task Put_MissingTitle_ReturnsValidationProblem()
    {
        var response = await _client.PutAsync("/api/tasks/1", new StringContent("""{ "isCompleted": true }""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Title", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Post_MalformedJson_ReturnsValidationProblem()
    {
        var response = await _client.PostAsync("/api/tasks", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsProblemDetails404()
    {
        var response = await _client.GetAsync("/api/tasks/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(404, problem!.Status);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsProblemDetails404()
    {
        var response = await _client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetailsWithoutStackTrace()
    {
        using var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<ITaskService, ThrowingTaskService>()))
            .CreateClient();

        var response = await client.GetAsync("/api/tasks/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Boom", body);
        Assert.DoesNotContain("at TaskApi", body);
    }

    private sealed class ThrowingTaskService : ITaskService
    {
        private static Exception Boom() => new InvalidOperationException("Boom");

        public Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken cancellationToken) => throw Boom();
        public Task<TaskDto?> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Boom();
        public Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken) => throw Boom();
        public Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken) => throw Boom();
        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) => throw Boom();
    }
}
