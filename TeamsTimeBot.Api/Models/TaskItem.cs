namespace TeamsTimeBot.Api.Models;

public class TaskItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public User CreatedBy { get; set; } = null!;

    public ICollection<TaskComment> Comments { get; set; }
        = new List<TaskComment>();
    }
