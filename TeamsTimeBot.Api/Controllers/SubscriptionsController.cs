using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private readonly GraphService _graphService;
    private readonly IConfiguration _configuration;
    private readonly AuthorizationService _authorizationService;

    public SubscriptionsController(
        GraphService graphService,
        IConfiguration configuration,
        AuthorizationService authorizationService)
    {
        _graphService = graphService;
        _configuration = configuration;
        _authorizationService = authorizationService;
    }

    [HttpPost("channel-messages")]
    public async Task<IActionResult> CreateChannelMessageSubscription(
        [FromQuery] string teamId,
        [FromQuery] string channelId)
    {
        var userAzureId = User.FindFirst("oid")?.Value;

        if (string.IsNullOrWhiteSpace(userAzureId))
        {
            return Unauthorized();
        }

        var isAdmin = await _authorizationService.IsAdminAsync(
            userAzureId);

        if (!isAdmin)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(teamId))
        {
            return BadRequest("Brak teamId.");
        }

        if (string.IsNullOrWhiteSpace(channelId))
        {
            return BadRequest("Brak channelId.");
        }

        var notificationUrl =
            _configuration["Graph:NotificationUrl"];

        var clientState =
            _configuration["Graph:ClientState"];

        if (string.IsNullOrWhiteSpace(notificationUrl))
        {
            return BadRequest(
                "Graph:NotificationUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(clientState))
        {
            return BadRequest(
                "Graph:ClientState is not configured.");
        }

        var subscription =
            await _graphService.CreateChannelMessageSubscriptionAsync(
                teamId,
                channelId,
                notificationUrl,
                clientState);

        return Ok(subscription);
    }
}
