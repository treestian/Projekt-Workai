namespace TeamsTimeBot.Api.Models;

public class LLMResponse
{
    public int? TaskId { get; set; }

    public string? TaskName { get; set; }

    public string? NewTaskName { get; set; }

    public string? Description { get; set; }

    public string? Comment { get; set; }

    public int? ManualMinutes { get; set; }
    public DateTime? WorkDate { get; set; }
}
