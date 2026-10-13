using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Errors;
using TaskApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskDb"));
});
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new { name = "Task API", status = "ok" }));

app.MapControllers();

app.Run();
