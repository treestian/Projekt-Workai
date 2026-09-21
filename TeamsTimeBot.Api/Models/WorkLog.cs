namespace TeamsTimeBot.Api.Models;

public class WorkLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int TaskId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }
    public TimeSpan? Duration =>
        EndedAt.HasValue
            ? EndedAt.Value - StartedAt
            : null;
    public string Status { get; set; } = "W trakcie";
}