using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskApi.Data;
using TaskApi.Dtos;
using TaskApi.Models;
using TaskApi.Services;

namespace TaskApi.Tests.Unit;

public sealed class TaskServiceTests : IDisposable
{
    private const string UserA = "user-a";
    private const string UserB = "user-b";

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

        // Tasks need an owner, so add two users to own them.
        _db.Users.AddRange(new AppUser { Id = UserA, UserName = "a" }, new AppUser { Id = UserB, UserName = "b" });
        _db.SaveChanges();

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
        var created = await _service.CreateAsync(UserA, new CreateTaskDto("  Write tests  ", "   "), CancellationToken.None);

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
        Assert.Null(await _service.GetByIdAsync(UserA, 42, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ChangesFieldsAndSetsUpdatedAt()
    {
        var created = await _service.CreateAsync(UserA, new CreateTaskDto("Old", null), CancellationToken.None);

        var updated = await _service.UpdateAsync(UserA, created.Id, new UpdateTaskDto("New", "Details", true), CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("New", updated.Title);
        Assert.Equal("Details", updated.Description);
        Assert.True(updated.IsCompleted);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNullWhenMissing()
    {
        Assert.Null(await _service.UpdateAsync(UserA, 42, new UpdateTaskDto("x", null, false), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTaskAndReportsMissing()
    {
        var created = await _service.CreateAsync(UserA, new CreateTaskDto("Delete me", null), CancellationToken.None);

        Assert.True(await _service.DeleteAsync(UserA, created.Id, CancellationToken.None));
        Assert.False(await _service.DeleteAsync(UserA, created.Id, CancellationToken.None));
        Assert.Equal(0, await _db.Tasks.CountAsync());
    }

    [Fact]
    public async Task GetPageAsync_FiltersByCompletion()
    {
        var done = await _service.CreateAsync(UserA, new CreateTaskDto("Done", null), CancellationToken.None);
        await _service.UpdateAsync(UserA, done.Id, new UpdateTaskDto("Done", null, true), CancellationToken.None);
        await _service.CreateAsync(UserA, new CreateTaskDto("Open", null), CancellationToken.None);

        var page = await _service.GetPageAsync(UserA, new TaskQuery { IsCompleted = true }, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Done", Assert.Single(page.Items).Title);
    }

    [Fact]
    public async Task GetPageAsync_SearchMatchesTitleOrDescriptionCaseInsensitively()
    {
        await _service.CreateAsync(UserA, new CreateTaskDto("Build API", null), CancellationToken.None);
        await _service.CreateAsync(UserA, new CreateTaskDto("Other", "uses the api"), CancellationToken.None);
        await _service.CreateAsync(UserA, new CreateTaskDto("Unrelated", null), CancellationToken.None);

        var page = await _service.GetPageAsync(UserA, new TaskQuery { Search = "Api" }, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetPageAsync_SearchTreatsWildcardsLiterally()
    {
        await _service.CreateAsync(UserA, new CreateTaskDto("100% done", null), CancellationToken.None);
        await _service.CreateAsync(UserA, new CreateTaskDto("Anything", null), CancellationToken.None);

        var percent = await _service.GetPageAsync(UserA, new TaskQuery { Search = "%" }, CancellationToken.None);
        var underscore = await _service.GetPageAsync(UserA, new TaskQuery { Search = "_" }, CancellationToken.None);

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
            await _service.CreateAsync(UserA, new CreateTaskDto(title, null), CancellationToken.None);
        }

        var page = await _service.GetPageAsync(UserA, new TaskQuery { Sort = sort }, CancellationToken.None);

        Assert.Equal(expected, page.Items.Select(t => t.Title));
    }

    [Fact]
    public async Task GetPageAsync_PageBeyondEndIsEmptyButKeepsTotal()
    {
        await _service.CreateAsync(UserA, new CreateTaskDto("Only", null), CancellationToken.None);

        var page = await _service.GetPageAsync(UserA, new TaskQuery { Page = 3, PageSize = 10 }, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task OtherUsersTasks_AreInvisibleAndUntouchable()
    {
        var bobsTask = await _service.CreateAsync(UserB, new CreateTaskDto("Bob only", null), CancellationToken.None);

        Assert.Null(await _service.GetByIdAsync(UserA, bobsTask.Id, CancellationToken.None));
        Assert.Null(await _service.UpdateAsync(UserA, bobsTask.Id, new UpdateTaskDto("Mine now", null, true), CancellationToken.None));
        Assert.False(await _service.DeleteAsync(UserA, bobsTask.Id, CancellationToken.None));
        Assert.Equal(0, (await _service.GetPageAsync(UserA, new TaskQuery(), CancellationToken.None)).TotalCount);

        var unchanged = await _service.GetByIdAsync(UserB, bobsTask.Id, CancellationToken.None);
        Assert.Equal("Bob only", unchanged!.Title);
    }

    [Fact]
    public async Task Dates_AreReturnedAsUtc()
    {
        var created = await _service.CreateAsync(UserA, new CreateTaskDto("Utc", null), CancellationToken.None);
        _db.ChangeTracker.Clear();

        var loaded = await _service.GetByIdAsync(UserA, created.Id, CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, loaded!.CreatedAt.Kind);
    }
}
