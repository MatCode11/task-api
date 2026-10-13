using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Errors;
using TaskApi.Models;
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

// Identity API endpoints: register/login issue bearer tokens (or cookies with ?useCookies=true).
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
    options.CombineLogs = true;
});

var app = builder.Build();

app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await DevDataSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { name = "Task API", status = "ok" }));
app.MapHealthChecks("/health");
app.MapGroup("/api/auth").WithTags("Auth").MapIdentityApi<AppUser>();

app.MapControllers();

app.Run();
