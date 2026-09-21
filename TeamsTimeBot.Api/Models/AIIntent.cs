namespace TeamsTimeBot.Api.Models;

public class AIIntent
{
    public string Action { get; set; } = "UNKNOWN";

    public List<string> PossibleActions { get; set; } = [];

    public int? TaskId { get; set; }

    public string? TaskName { get; set; }

    public string? NewTaskName { get; set; }

    public string? Description { get; set; }

    public string? Comment { get; set; }

    public int? ManualMinutes { get; set; }
}