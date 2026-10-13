using TaskApi.Dtos;

namespace TaskApi.Services;

public interface ITaskService
{
    Task<PagedResult<TaskDto>> GetPageAsync(TaskQuery query, CancellationToken cancellationToken);
    Task<TaskDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TaskDto> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken);
    Task<TaskDto?> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
