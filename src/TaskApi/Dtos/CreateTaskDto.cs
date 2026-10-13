using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dtos;

public sealed record CreateTaskDto(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description
);
