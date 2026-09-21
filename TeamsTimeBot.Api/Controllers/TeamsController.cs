using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/teams")]
public class TeamsController : ControllerBase
{
    private readonly GraphService _graphService;

    public TeamsController(GraphService graphService)
    {
        _graphService = graphService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _graphService.Client.Teams
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "displayName",
                    "description",
                    "visibility"
                };
            });

        return Ok(teams?.Value);
    }

    [HttpGet("{teamId}/channels")]
    public async Task<IActionResult> GetChannels(string teamId)
    {
        var channels = await _graphService.Client.Teams[teamId]
            .Channels
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "displayName",
                    "description",
                    "membershipType"
                };
            });

        return Ok(channels?.Value);
    }
    [HttpGet("{teamId}/channels/{channelId}/messages")]
    public async Task<IActionResult> GetMessages(
        string teamId,
        string channelId)
    {
        var messages = await _graphService.Client
            .Teams[teamId]
            .Channels[channelId]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "createdDateTime",
                    "subject",
                    "body",
                    "from"
                };

                config.QueryParameters.Top = 20;
            });

        return Ok(messages?.Value);
    }
}

