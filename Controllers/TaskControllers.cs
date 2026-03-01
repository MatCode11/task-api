using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TaskControllers : ControllerBase
{
    [HttpGet]
    public IActionResult GetTasks()
    {
        var payload = new[] { new { Id = 1, Title = "Sample Task" } };
        return Ok(payload);
    }
}





