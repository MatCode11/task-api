using TaskApi.Models;

namespace TaskApi.Services;

public interface ITaskService
{
    IEnumerable<TaskApi.Models.Task> GetAllTasks();
    TaskApi.Models.Task? GetTaskById(int id);
    TaskApi.Models.Task CreateTask(TaskApi.Models.Task task);
    void UpdateTask(TaskApi.Models.Task task);
    void DeleteTask(int id);
}
