using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskApi.Data;
using TaskApi.Dtos;
using TaskApi.Services;

namespace TaskApi.Tests.Unit;

public sealed class TaskServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.Migrate();

        _service = new TaskService(_db, NullLogger<TaskService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateAsync_TrimsInputAndStoresTask()
    {
        var created = await _service.CreateAsync(new CreateTaskDto("  Write tests  ", "   "), CancellationToken.None);

        Assert.True(created.Id > 0);
        Assert.Equal("Write tests", created.Title);
        Assert.Null(created.Description);
        Assert.False(created.IsCompleted);
        Assert.Null(created.UpdatedAt);
        Assert.Equal(1, await _db.Tasks.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenMissing()
    {
        Assert.Null(await _service.GetByIdAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndSetsUpdatedAt()
    {
        var created = await _service.CreateAsync(new CreateTaskDto("Old", null), CancellationToken.None);

        var updated = await _service.UpdateAsync(created.Id, new UpdateTaskDto("New", "Details", true), CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("New", updated.Title);
        Assert.Equal("Details", updated.Description);
        Assert.True(updated.IsCompleted);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullWhenMissing()
    {
        Assert.Null(await _service.UpdateAsync(42, new UpdateTaskDto("x", null, false), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskAndReportsMissing()
    {
        var created = await _service.CreateAsync(new CreateTaskDto("Delete me", null), CancellationToken.None);

        Assert.True(await _service.DeleteAsync(created.Id, CancellationToken.None));
        Assert.False(await _service.DeleteAsync(created.Id, CancellationToken.None));
        Assert.Equal(0, await _db.Tasks.CountAsync());
    }

    [Fact]
    public async Task GetPageAsync_FiltersByCompletion()
    {
        var done = await _service.CreateAsync(new CreateTaskDto("Done", null), CancellationToken.None);
        await _service.UpdateAsync(done.Id, new UpdateTaskDto("Done", null, true), CancellationToken.None);
        await _service.CreateAsync(new CreateTaskDto("Open", null), CancellationToken.None);

        var page = await _service.GetPageAsync(new TaskQuery { IsCompleted = true }, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Done", Assert.Single(page.Items).Title);
    }

    [Fact]
    public async Task GetPageAsync_SearchMatchesTitleOrDescriptionCaseInsensitively()
    {
        await _service.CreateAsync(new CreateTaskDto("Build API", null), CancellationToken.None);
        await _service.CreateAsync(new CreateTaskDto("Other", "uses the api"), CancellationToken.None);
        await _service.CreateAsync(new CreateTaskDto("Unrelated", null), CancellationToken.None);

        var page = await _service.GetPageAsync(new TaskQuery { Search = "Api" }, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_SearchTreatsWildcardsLiterally()
    {
        await _service.CreateAsync(new CreateTaskDto("100% done", null), CancellationToken.None);
        await _service.CreateAsync(new CreateTaskDto("Anything", null), CancellationToken.None);

        var percent = await _service.GetPageAsync(new TaskQuery { Search = "%" }, CancellationToken.None);
        var underscore = await _service.GetPageAsync(new TaskQuery { Search = "_" }, CancellationToken.None);

        Assert.Equal("100% done", Assert.Single(percent.Items).Title);
        Assert.Empty(underscore.Items);
    }

    [Theory]
    [InlineData("title", new[] { "a", "b", "c" })]
    [InlineData("-title", new[] { "c", "b", "a" })]
    [InlineData("-id", new[] { "a", "c", "b" })]
    [InlineData(null, new[] { "b", "c", "a" })]
    public async Task GetPageAsync_Sorts(string? sort, string[] expected)
    {
        foreach (var title in new[] { "b", "c", "a" })
        {
            await _service.CreateAsync(new CreateTaskDto(title, null), CancellationToken.None);
        }

        var page = await _service.GetPageAsync(new TaskQuery { Sort = sort }, CancellationToken.None);

        Assert.Equal(expected, page.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task GetPageAsync_PageBeyondEndIsEmptyButKeepsTotal()
    {
        await _service.CreateAsync(new CreateTaskDto("Only", null), CancellationToken.None);

        var page = await _service.GetPageAsync(new TaskQuery { Page = 3, PageSize = 10 }, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Dates_AreReturnedAsUtc()
    {
        var created = await _service.CreateAsync(new CreateTaskDto("Utc", null), CancellationToken.None);
        _db.ChangeTracker.Clear();

        var loaded = await _service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, loaded!.CreatedAt.Kind);
    }
}
