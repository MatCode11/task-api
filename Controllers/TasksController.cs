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

    [HttpGet]
    public IActionResult GetTasks()
    {
        return Ok(_tasks);
    }
}
