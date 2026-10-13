namespace TaskApi.Dtos;

public sealed record TaskDto(
    int Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
