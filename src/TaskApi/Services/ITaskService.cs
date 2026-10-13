using TaskApi.Dtos;

namespace TaskApi.Services;

// Every method is scoped to one user: a task owned by someone else behaves as if it does not exist.
public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetPageAsync(string userId, TaskQuery query, CancellationToken cancellationToken);
    Task<TaskDto?> GetByIdAsync(string userId, int id, CancellationToken cancellationToken);
    Task<TaskDto> CreateAsync(string userId, CreateTaskDto dto, CancellationToken cancellationToken);
    Task<TaskDto?> UpdateAsync(string userId, int id, UpdateTaskDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string userId, int id, CancellationToken cancellationToken);
}
