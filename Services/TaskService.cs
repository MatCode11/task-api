using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Dtos;
using TaskApi.Models;

namespace TaskApi.Services;

public sealed class TaskService : ITaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Tasks
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt, t.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken)
    {
        var entity = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        _db.Tasks.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new TaskDto(entity.Id, entity.Title, entity.Description, entity.IsCompleted, entity.CreatedAt, entity.UpdatedAt);
    }

    public async Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken)
    {
        var entity = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = dto.Title.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.IsCompleted = dto.IsCompleted;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new TaskDto(entity.Id, entity.Title, entity.Description, entity.IsCompleted, entity.CreatedAt, entity.UpdatedAt);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _db.Tasks.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
