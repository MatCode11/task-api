# TaskApi

A small ASP.NET Core REST API for managing tasks, built as a backend learning project. It uses controllers, a service layer that returns DTOs, and EF Core. The roadmap is in `PLAN.md`: SQLite with migrations, tests, validation and ProblemDetails errors, pagination, logging, and auth. Code should stay simple and idiomatic, because the user is learning from it. Prefer built-in ASP.NET Core features over third-party libraries.

## Stack
- .NET 10 / ASP.NET Core controllers in `src/TaskApi`
- EF Core 10 with SQLite, migrations in `Data/Migrations` (local `dotnet-ef` tool)
- Built-in OpenAPI (`/openapi/v1.json` in Development)
- xUnit + `Microsoft.AspNetCore.Mvc.Testing` for tests in `tests/TaskApi.Tests`
- ASP.NET Core Identity API endpoints (bearer tokens) under `/api/auth`

## Commands
```bash
dotnet tool restore                           # installs the local dotnet-ef from dotnet-tools.json
dotnet build
dotnet run --project src/TaskApi              # http://localhost:5194
dotnet test
dotnet ef migrations add <Name> --project src/TaskApi --output-dir Data/Migrations
dotnet ef database update --project src/TaskApi
```
Use `TaskApi.http` for manual requests and keep it updated when endpoints change.

## Conventions
- Layers: Controllers (HTTP only) -> Services (`I<Name>Service`, registered as Scoped) -> `AppDbContext`. Controllers never touch `AppDbContext` directly.
- Every `ITaskService` method takes the caller's `userId` and filters by it. A task owned by someone else is treated as not found (404).
- Never return EF entities from endpoints. Use `sealed record` DTOs in `Dtos/`.
- Async all the way: every I/O method is `async`, has an `Async` suffix, and accepts and passes through a `CancellationToken`.
- Read queries use `AsNoTracking()` and project to DTOs with `Select`.
- Constructor injection only. Files use file-scoped namespaces, `sealed` classes by default, and nullable reference types are enabled.
- Error responses use ProblemDetails (from M4). Do not return ad-hoc `{ error = ... }` objects in new code.
- Times are stored in UTC (`DateTime.UtcNow`).
- Every new endpoint or behavior change gets tests (from M3) and an entry in `TaskApi.http`.

## Hard rules
- Schema changes go through EF migrations only (from M2). Never use `EnsureCreated` together with migrations, and never edit an applied migration.
- No secrets in `appsettings.json` committed to git. Use user-secrets or environment variables.
- Keep EF Core and ASP.NET package versions on the same major version as the target framework.
- Do not add repository/unit-of-work layers, MediatR, or AutoMapper unless the user asks.
- Update the progress log in `PLAN.md` when a milestone is finished. Never delete the progress log.
