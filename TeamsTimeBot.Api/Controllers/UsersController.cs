using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly GraphService _graphService;
    private readonly UserSyncService _syncService;
    private readonly UserService _userService;

    public UsersController(
        GraphService graphService,
        UserSyncService syncService,
        UserService userService)
    {
        _graphService = graphService;
        _syncService = syncService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsersAsync();

        return Ok(users);
    }

    [HttpGet("sync-test")]
    public async Task<IActionResult> SyncTest()
    {
        var users = await _graphService.GetUsersAsync();

        return Ok(users);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync()
    {
        var result = await _syncService.SyncUsersAsync();

        return Ok(result);
    }

    [HttpGet("sync-settings")]
    public async Task<IActionResult> GetSyncSettings()
    {
        var settings = await _userService.GetSyncSettingsAsync();

        return Ok(new
        {
            intervalHours = settings.UserSyncIntervalHours,
            updatedAt = settings.UpdatedAt
        });
    }

    [HttpPut("sync-settings")]
    public async Task<IActionResult> UpdateSyncSettings(
        [FromBody] UpdateSyncSettingsRequest request)
    {
        var settings = await _userService.UpdateSyncSettingsAsync(
            request.IntervalHours);

        if (settings == null)
        {
            return BadRequest(
                "Interwał musi wynosić od 1 do 168 godzin.");
        }

        return Ok(new
        {
            intervalHours = settings.UserSyncIntervalHours,
            updatedAt = settings.UpdatedAt
        });
    }
}

public record UpdateSyncSettingsRequest(int IntervalHours);

