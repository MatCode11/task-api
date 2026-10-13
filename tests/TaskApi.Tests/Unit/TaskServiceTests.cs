using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

        _service = new TaskService(_db);
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
    public async Task Dates_AreReturnedAsUtc()
    {
        var created = await _service.CreateAsync(new CreateTaskDto("Utc", null), CancellationToken.None);
        _db.ChangeTracker.Clear();

        var loaded = await _service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, loaded!.CreatedAt.Kind);
    }
}
