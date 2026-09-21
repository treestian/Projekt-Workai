using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly GraphService _graphService;
    private readonly IConfiguration _configuration;

    public SubscriptionsController(
        GraphService graphService,
        IConfiguration configuration)
    {
        _graphService = graphService;
        _configuration = configuration;
    }

    [HttpPost("channel-messages")]
    public async Task<IActionResult> CreateChannelMessageSubscription(
        [FromQuery] string teamId,
        [FromQuery] string channelId)
    {
        var notificationUrl =
            _configuration["Graph:NotificationUrl"];

        var clientState =
            _configuration["Graph:ClientState"];

        if (string.IsNullOrEmpty(notificationUrl))
        {
            return BadRequest("Graph:NotificationUrl is not configured.");
        }

        if (string.IsNullOrEmpty(clientState))
        {
            return BadRequest("Graph:ClientState is not configured.");
        }

        var subscription = new Subscription
        {
            ChangeType = "created",
            NotificationUrl = notificationUrl,
            Resource = $"/teams/{teamId}/channels/{channelId}/messages",
            ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(50),
            ClientState = clientState
        };

        var result = await _graphService.Client
            .Subscriptions
            .PostAsync(subscription);

        return Ok(result);
    }
}