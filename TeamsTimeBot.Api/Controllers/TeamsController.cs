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
        var teams = await _graphService.GetTeamsAsync();

        return Ok(teams);
    }

    [HttpGet("{teamId}/channels")]
    public async Task<IActionResult> GetChannels(string teamId)
    {
        var channels = await _graphService.GetChannelsAsync(teamId);

        return Ok(channels);
    }

    [HttpGet("{teamId}/channels/{channelId}/messages")]
    public async Task<IActionResult> GetMessages(
        string teamId,
        string channelId)
    {
        var messages = await _graphService.GetChannelMessagesAsync(
            teamId,
            channelId);

        return Ok(messages);
    }
}

