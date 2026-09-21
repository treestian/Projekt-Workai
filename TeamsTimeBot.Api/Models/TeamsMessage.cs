namespace TeamsTimeBot.Api.Models;

public class TeamsMessage
{
    public int Id { get; set; }

    public string GraphMessageId { get; set; } = string.Empty;

    public string TeamId { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string? SenderAzureId { get; set; }

    public string? SenderName { get; set; }

    public string? MessageText { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ReceivedAt { get; set; }
}