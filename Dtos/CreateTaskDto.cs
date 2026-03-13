namespace TaskApi.Dtos;

public sealed record CreateTaskDto(
    string Title,
    string? Description
);
