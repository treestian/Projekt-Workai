namespace TeamsTimeBot.Api.Models;

public class TaskComment
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public TaskItem Task { get; set; } = null!;

    public User Author { get; set; } = null!;
}