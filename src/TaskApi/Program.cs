using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Models;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("TaskDb");
});
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Tasks.AnyAsync())
    {
        db.Tasks.AddRange(
            new TaskItem { Title = "Learn ASP.NET Core", IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new TaskItem { Title = "Build a simple API", IsCompleted = false, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
    }
}

app.MapGet("/", () => Results.Ok(new { name = "Task API", status = "ok" }));

app.MapControllers();

app.Run();


