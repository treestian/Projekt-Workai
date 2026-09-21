namespace TeamsTimeBot.Api.Models;

public class ParsedMessage
{
    public string Action { get; set; } = string.Empty;

    public int? TaskId { get; set; }

    public string OriginalText { get; set; } = string.Empty;
    public string? Comment { get; set; }
}