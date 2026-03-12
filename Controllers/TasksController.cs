using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TaskApi.Models;
using TaskApi.Services;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("di")]
    public ActionResult GetDiInfo()
    {
        var resolved1 = HttpContext.RequestServices.GetRequiredService<ITaskService>();
        var resolved2 = HttpContext.RequestServices.GetRequiredService<ITaskService>();

        return Ok(new
        {
            injectedInstanceId = _taskService.InstanceId,
            resolved1InstanceId = resolved1.InstanceId,
            resolved2InstanceId = resolved2.InstanceId,
            resolvedSameInstance = ReferenceEquals(resolved1, resolved2),
            requestTraceId = HttpContext.TraceIdentifier
        });
    }

    [HttpGet]
    public ActionResult<IEnumerable<TaskApi.Models.Task>> GetTasks()
    {
        return Ok(_taskService.GetAllTasks());
    }

    [HttpGet("{id}")]
    public ActionResult<TaskApi.Models.Task> GetTask(int id)
    {
        var task = _taskService.GetTaskById(id);
        if (task == null)
        {
            return NotFound();
        }
        return Ok(task);
    }

    [HttpPost]
    public ActionResult<TaskApi.Models.Task> CreateTask([FromBody] TaskApi.Models.Task newTask)
    {
        var createdTask = _taskService.CreateTask(newTask);
        return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id}")]
    public ActionResult UpdateTask(int id, [FromBody] TaskApi.Models.Task updatedTask)
    {
        var existingTask = _taskService.GetTaskById(id);
        if (existingTask == null)
        {
            return NotFound();
        }
        
        updatedTask.Id = id;
        _taskService.UpdateTask(updatedTask);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteTask(int id)
    {
        var existingTask = _taskService.GetTaskById(id);
        if (existingTask == null)
        {
            return NotFound();
        }

        _taskService.DeleteTask(id);
        return NoContent();
    }
}
