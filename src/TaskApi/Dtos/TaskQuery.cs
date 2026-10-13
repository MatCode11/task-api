using System.ComponentModel.DataAnnotations;

namespace TaskApi.Dtos;

// Query string for GET /api/tasks, e.g. ?page=1&pageSize=20&isCompleted=false&search=api&sort=-createdAt
public sealed class TaskQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;

    public bool? IsCompleted { get; init; }

    [StringLength(200)]
    public string? Search { get; init; }

    // A field name, optionally prefixed with "-" for descending order.
    [RegularExpression("^-?(?i:id|title|createdAt|updatedAt)$",
        ErrorMessage = "Sort must be one of id, title, createdAt, updatedAt, optionally prefixed with '-'.")]
    public string? Sort { get; init; }
}
