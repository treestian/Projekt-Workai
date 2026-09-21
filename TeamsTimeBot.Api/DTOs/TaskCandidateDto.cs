namespace TeamsTimeBot.Api.DTOs;

public class TaskCandidateDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }
}