using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly GraphService _graphService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationsController> _logger;
    private readonly AppDbContext _dbContext;

    public NotificationsController(
        GraphService graphService,
        IConfiguration configuration,
        ILogger<NotificationsController> logger,
        AppDbContext dbContext)
    {
        _graphService = graphService;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpPost("teams")]
    public async Task<IActionResult> ReceiveNotification(
        [FromQuery] string? validationToken)
    {
        // Microsoft Graph validation request
        if (!string.IsNullOrEmpty(validationToken))
        {
            return Content(
                validationToken,
                "text/plain");
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        _logger.LogInformation(
            "Received Teams notification: {Body}",
            body);

        if (string.IsNullOrWhiteSpace(body))
        {
            return Ok();
        }

        try
        {
            using var json = JsonDocument.Parse(body);

            if (!json.RootElement.TryGetProperty(
                    "value",
                    out var notifications))
            {
                return Ok();
            }

            foreach (var notification in notifications.EnumerateArray())
            {
                var clientState = notification
                    .GetProperty("clientState")
                    .GetString();

                var expectedClientState =
                    _configuration["Graph:ClientState"];

                if (clientState != expectedClientState)
                {
                    _logger.LogWarning(
                        "Received notification with invalid clientState.");

                    continue;
                }

                var changeType = notification
                    .GetProperty("changeType")
                    .GetString();

                var resource = notification
                    .GetProperty("resource")
                    .GetString();

                _logger.LogInformation(
                    "Teams message notification received. " +
                    "ChangeType: {ChangeType}, Resource: {Resource}",
                    changeType,
                    resource);

                if (changeType == "created" &&
                    !string.IsNullOrEmpty(resource))
                {
                    await GetMessageFromGraphAsync(resource);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Invalid JSON received from Microsoft Graph.");
        }

        return Ok();
    }

    private async Task GetMessageFromGraphAsync(string resource)
    {
        try
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

            var message = await _graphService.Client
                .Teams[teamId]
                .Channels[channelId]
                .Messages[messageId]
                .GetAsync();

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

            var senderName = message.From?.User?.DisplayName;
            var senderId = message.From?.User?.Id;
            var messageText = message.Body?.Content;
            var createdAt = message.CreatedDateTime;

            var existingMessage = await _dbContext.TeamsMessages
                .FirstOrDefaultAsync(x =>
                    x.GraphMessageId == message.Id);

            if (existingMessage != null)
            {
                _logger.LogInformation(
                    "Message {MessageId} already exists in database.",
                    message.Id);

                return;
            }

            var cleanMessageText =
                System.Text.RegularExpressions.Regex.Replace(
                    messageText ?? string.Empty,
                    "<.*?>",
                    string.Empty);

            var teamsMessage = new TeamsMessage
            {
                GraphMessageId = message.Id,
                TeamId = teamId,
                ChannelId = channelId,
                SenderAzureId = senderId,
                SenderName = senderName,
                MessageText = System.Net.WebUtility
                    .HtmlDecode(cleanMessageText)
                    .Trim(),
                CreatedAt = createdAt?.UtcDateTime
                    ?? DateTime.UtcNow,
                ReceivedAt = DateTime.UtcNow
            };

            _dbContext.TeamsMessages.Add(teamsMessage);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Teams message {MessageId} saved to database.",
                message.Id);

            _logger.LogInformation(
                "===== TEAMS MESSAGE =====");

            _logger.LogInformation(
                "Sender: {SenderName}",
                senderName);

            _logger.LogInformation(
                "Sender ID: {SenderId}",
                senderId);

            _logger.LogInformation(
                "Created: {CreatedAt}",
                createdAt);

            _logger.LogInformation(
                "Message: {MessageText}",
                teamsMessage.MessageText);

            _logger.LogInformation(
                "=========================");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error while getting Teams message from Microsoft Graph.");
        }
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