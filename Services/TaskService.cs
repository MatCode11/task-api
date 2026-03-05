using TaskApi.Models;

namespace TaskApi.Services;

public class TaskService : ITaskService
{
    // In-memory storage (static to persist across requests for Singleton/Transient testing later)
    // Note: In a real app with Scoped services and a DB, this wouldn't be static.
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
