namespace TaskApi.Dtos;

public sealed record UpdateTaskDto(
    string Title,
    string? Description,
    bool IsCompleted
);
