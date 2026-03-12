using TaskApi.Models;

namespace TaskApi.Services;

public class TaskService : ITaskService
{
    public string InstanceId { get; } = Guid.NewGuid().ToString("N");

    private static readonly List<TaskApi.Models.Task> _tasks = new()
    {
        new TaskApi.Models.Task { Id = 1, Title = "Learn ASP.NET Core", IsCompleted = false },
        new TaskApi.Models.Task { Id = 2, Title = "Build a Web API", IsCompleted = false }
    };

    public IEnumerable<TaskApi.Models.Task> GetAllTasks()
    {
        return _tasks;
    }

    public TaskApi.Models.Task? GetTaskById(int id)
    {
        return _tasks.FirstOrDefault(t => t.Id == id);
    }

    public TaskApi.Models.Task CreateTask(TaskApi.Models.Task task)
    {
        task.Id = _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1;
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = null;
        _tasks.Add(task);
        return task;
    }

    public void UpdateTask(TaskApi.Models.Task task)
    {
        var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existingTask != null)
        {
            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.IsCompleted = task.IsCompleted;
            existingTask.DueDate = task.DueDate;
            existingTask.UpdatedAt = DateTime.UtcNow;
        }
    }

    public void DeleteTask(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            _tasks.Remove(task);
        }
    }
}
