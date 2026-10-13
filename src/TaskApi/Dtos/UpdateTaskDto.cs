using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dtos;

public sealed record UpdateTaskDto(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    bool IsCompleted
);
