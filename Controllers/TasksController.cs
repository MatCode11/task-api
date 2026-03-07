using Microsoft.AspNetCore.Mvc;
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

    //Get all task 
    [HttpGet]
    public ActionResult<List<TaskApi.Models.Task>> GetTasks()
    {
        return Ok(_taskService.GetAllTasks());
    }

    //Get task by id
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

    //Create task
    [HttpPost]
    public ActionResult<TaskApi.Models.Task> CreateTask([FromBody] TaskApi.Models.Task newTask)
    {
        var createdTask = _taskService.CreateTask(newTask);
        return CreatedAtAction(nameof(GetTask), new { id = createdTask.Id }, createdTask);
    }

    //Update task
    [HttpPut("{id}")]
    public ActionResult UpdateTask(int id, [FromBody] TaskApi.Models.Task updatedTask)
    {
        if (id != updatedTask.Id)
        {
            return BadRequest();
        }

        var existingTask = _taskService.GetTaskById(id);
        if (existingTask == null)
        {
            return NotFound();
        }
        
        _taskService.UpdateTask(updatedTask);
        return NoContent();
    }

    //Delete task
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
