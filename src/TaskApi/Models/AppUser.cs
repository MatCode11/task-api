using Microsoft.AspNetCore.Identity;

namespace TaskApi.Models;

public sealed class AppUser : IdentityUser
{
    public List<TaskItem> Tasks { get; } = [];
}
