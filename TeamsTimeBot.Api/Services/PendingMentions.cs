namespace TeamsTimeBot.Api.Services;

public class PendingMentions
{
    private readonly List<PendingMention> _mentions = [];

    public IReadOnlyList<PendingMention> Items => _mentions;

    public void Add(
        string? azureId,
        string? displayName,
        string message)
    {
        if (string.IsNullOrWhiteSpace(azureId))
        {
            return;
        }

        if (_mentions.Any(x =>
                x.AzureId == azureId &&
                x.Message == message))
        {
            return;
        }

        _mentions.Add(
            new PendingMention(
                azureId,
                displayName,
                message));
    }
}

public sealed record PendingMention(
    string AzureId,
    string? DisplayName,
    string Message);
