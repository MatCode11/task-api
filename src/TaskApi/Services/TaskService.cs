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

    public async Task<PagedResult<TaskDto>> GetPageAsync(TaskQuery query, CancellationToken cancellationToken)
    {
        var tasks = _db.Tasks.AsNoTracking();

        if (query.IsCompleted is bool isCompleted)
        {
            tasks = tasks.Where(t => t.IsCompleted == isCompleted);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Escape LIKE wildcards so the search text is matched literally.
            var pattern = "%" + query.Search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_") + "%";
            tasks = tasks.Where(t =>
                EF.Functions.Like(t.Title, pattern, "\\") ||
                (t.Description != null && EF.Functions.Like(t.Description, pattern, "\\")));
        }

        var totalCount = await tasks.CountAsync(cancellationToken);

        var items = await ApplySort(tasks, query.Sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static IQueryable<TaskItem> ApplySort(IQueryable<TaskItem> tasks, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var field = sort?.TrimStart('-').ToLowerInvariant();

        // Id is the tiebreaker so paging is stable when the sort field has duplicates.
        return (field, descending) switch
        {
            ("title", false) => tasks.OrderBy(t => t.Title).ThenBy(t => t.Id),
            ("title", true) => tasks.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id),
            ("createdat", false) => tasks.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
            ("createdat", true) => tasks.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id),
            ("updatedat", false) => tasks.OrderBy(t => t.UpdatedAt).ThenBy(t => t.Id),
            ("updatedat", true) => tasks.OrderByDescending(t => t.UpdatedAt).ThenByDescending(t => t.Id),
            (_, true) => tasks.OrderByDescending(t => t.Id),
            _ => tasks.OrderBy(t => t.Id)
        };
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
