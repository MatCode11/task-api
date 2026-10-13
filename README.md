# TaskApi

A small ASP.NET Core (.NET 10) REST API for managing tasks, built as a backend learning project. Each user registers, logs in, and manages their own task list. See `PLAN.md` for the roadmap and progress log.

## Features
- CRUD for tasks at `/api/tasks`, scoped to the logged-in user (another user's task returns 404)
- Paging, filtering, search and sorting: `GET /api/tasks?page=1&pageSize=20&isCompleted=false&search=api&sort=-createdAt`
- Accounts via ASP.NET Core Identity API endpoints under `/api/auth` (`register`, `login`, `refresh`, ...) with bearer tokens
- SQLite with EF Core migrations (`taskapi.db` next to the app)
- Validation with DataAnnotations; every error is an RFC 9457 ProblemDetails (`application/problem+json`)
- Structured logging, HTTP request logging, and a `/health` endpoint that checks the database
- OpenAPI document at `/openapi/v1.json` in Development

## Requirements
- .NET SDK 10 (pinned in `global.json`)

## Run
```bash
dotnet tool restore
dotnet run --project src/TaskApi
```
The API listens on http://localhost:5194. In Development, pending migrations are applied at startup. Other environments apply them with `dotnet ef database update --project src/TaskApi`.

Use `src/TaskApi/TaskApi.http` for sample requests: register, log in, copy the `accessToken` into `@token`, then call the task endpoints.

## Endpoints
| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/health` | No | `Healthy` when the database is reachable |
| POST | `/api/auth/register` | No | `{ email, password }` |
| POST | `/api/auth/login` | No | Returns `{ accessToken, refreshToken, expiresIn }` |
| POST | `/api/auth/refresh` | No | `{ refreshToken }` |
| GET | `/api/tasks` | Yes | Returns `{ items, page, pageSize, totalCount }`. `pageSize` max is 100 |
| GET | `/api/tasks/{id}` | Yes | 404 if missing or not yours |
| POST | `/api/tasks` | Yes | `{ title, description? }`, 201 with `Location` |
| PUT | `/api/tasks/{id}` | Yes | `{ title, description?, isCompleted }` |
| DELETE | `/api/tasks/{id}` | Yes | 204 |

## Test
```bash
dotnet test
```
Unit tests cover `TaskService` on an in-memory SQLite connection. Integration tests run the whole app through `WebApplicationFactory`, each test with its own in-memory SQLite database.

## Layout
```
src/TaskApi/          Web API (Controllers, Services, Data + Migrations, Models, Dtos, Errors)
tests/TaskApi.Tests/  xUnit tests (Unit/, Integration/)
```
