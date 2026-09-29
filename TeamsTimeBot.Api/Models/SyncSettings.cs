namespace TeamsTimeBot.Api.Models;

public class SyncSettings
{
    public int Id { get; set; }

    public int UserSyncIntervalHours { get; set; } = 24;

    public DateTime UpdatedAt { get; set; }

    public string? UsersDeltaLink { get; set; }
}