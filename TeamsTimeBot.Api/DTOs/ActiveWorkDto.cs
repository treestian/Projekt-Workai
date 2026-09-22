namespace TeamsTimeBot.Api.DTOs;

public class ActiveWorkDto
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public ActiveWorkTaskDto? Task { get; set; }
    public ActiveWorkUserDto? User { get; set; }
}

public class ActiveWorkTaskDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ActiveWorkUserDto
{
    public int Id { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}

