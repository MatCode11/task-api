using Microsoft.AspNetCore.Mvc;
using TaskApi.Models;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private static readonly List<Models.Task> _tasks = new()
    {
        new Models.Task { Id = 1, Title = "Learn ASP.NET Core", IsCompleted = false },
        new Models.Task { Id = 2, Title = "Build a Web API", IsCompleted = false }
    };

    //Get all task 
    [HttpGet]
    public ActionResult<List<Models.Task>> GetTasks()
    {
        return _tasks;
    }

    //Get task by id
    [HttpGet("{id}")]
    public ActionResult<Models.Task> GetTask(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
        {
            return NotFound();
        }
        return Ok(task);
    }

    //Create task
    [HttpPost]
    public ActionResult<Models.Task> CreateTask([FromBody] Models.Task newTask)
    {
        newTask.Id = _tasks.Any() ? _tasks.Max(t => t.Id) + 1 : 1;
        _tasks.Add(newTask);
        return CreatedAtAction(nameof(GetTask), new { id = newTask.Id }, newTask);
    }

}
