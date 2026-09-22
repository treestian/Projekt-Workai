using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class NotificationService
{
    private readonly GraphService _graphService;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        GraphService graphService,
        AppDbContext db,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _graphService = graphService;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessNotificationAsync(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        try
        {
            using var json = JsonDocument.Parse(body);

            if (!json.RootElement.TryGetProperty(
                    "value",
                    out var notifications))
            {
                return;
            }

            foreach (var notification in notifications.EnumerateArray())
            {
                await ProcessSingleNotificationAsync(notification);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid JSON received from Microsoft Graph.");
        }
    }

    private async Task ProcessSingleNotificationAsync(
        JsonElement notification)
    {
        var clientState = GetStringProperty(
            notification,
            "clientState");

        var expectedClientState =
            _configuration["Graph:ClientState"];

        if (clientState != expectedClientState)
        {
            _logger.LogWarning(
                "Received notification with invalid clientState.");

            return;
        }

        var changeType = GetStringProperty(
            notification,
            "changeType");

        var resource = GetStringProperty(
            notification,
            "resource");

        _logger.LogInformation(
            "Teams message notification received. " +
            "ChangeType: {ChangeType}, Resource: {Resource}",
            changeType,
            resource);

        if (!string.Equals(
                changeType,
                "created",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resource))
        {
            return;
        }

        await SaveTeamsMessageAsync(resource);
    }

    private async Task SaveTeamsMessageAsync(string resource)
    {
        var teamId = ExtractValue(resource, "teams");
        var channelId = ExtractValue(resource, "channels");
        var messageId = ExtractValue(resource, "messages");

        if (string.IsNullOrEmpty(teamId) ||
            string.IsNullOrEmpty(channelId) ||
            string.IsNullOrEmpty(messageId))
        {
            _logger.LogWarning(
                "Could not extract teamId, channelId or messageId " +
                "from resource.");

            return;
        }

        _logger.LogInformation(
            "Getting Teams message. " +
            "TeamId: {TeamId}, ChannelId: {ChannelId}, MessageId: {MessageId}",
            teamId,
            channelId,
            messageId);

        var message = await _graphService.GetChannelMessageAsync(
            teamId,
            channelId,
            messageId);

        if (message == null)
        {
            _logger.LogWarning(
                "Microsoft Graph returned no message.");

            return;
        }

        if (string.IsNullOrEmpty(message.Id))
        {
            _logger.LogWarning(
                "Microsoft Graph message has no ID.");

            return;
        }

        var existingMessage = await _db.TeamsMessages
            .FirstOrDefaultAsync(
                x => x.GraphMessageId == message.Id);

        if (existingMessage != null)
        {
            _logger.LogInformation(
                "Message {MessageId} already exists in database.",
                message.Id);

            return;
        }

        var teamsMessage = CreateTeamsMessage(
            message,
            teamId,
            channelId);

        _db.TeamsMessages.Add(teamsMessage);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Teams message {MessageId} saved to database.",
            message.Id);
    }

    private static TeamsMessage CreateTeamsMessage(
        ChatMessage message,
        string teamId,
        string channelId)
    {
        var rawMessageText = message.Body?.Content ?? string.Empty;

        var cleanMessageText = Regex.Replace(
            rawMessageText,
            "<.*?>",
            string.Empty);

        cleanMessageText = WebUtility
            .HtmlDecode(cleanMessageText)
            .Trim();

        return new TeamsMessage
        {
            GraphMessageId = message.Id!,
            TeamId = teamId,
            ChannelId = channelId,
            SenderAzureId = message.From?.User?.Id,
            SenderName = message.From?.User?.DisplayName,
            MessageText = cleanMessageText,
            CreatedAt = message.CreatedDateTime?.UtcDateTime
                ?? DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow
        };
    }

    private static string? GetStringProperty(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return null;
        }

        return property.GetString();
    }

    private static string? ExtractValue(
        string resource,
        string resourceName)
    {
        var pattern = $"{resourceName}('";

        var startIndex = resource.IndexOf(
            pattern,
            StringComparison.OrdinalIgnoreCase);

        if (startIndex == -1)
        {
            return null;
        }

        startIndex += pattern.Length;

        var endIndex = resource.IndexOf(
            "')",
            startIndex,
            StringComparison.OrdinalIgnoreCase);

        if (endIndex == -1)
        {
            return null;
        }

        return resource[startIndex..endIndex];
    }
}
