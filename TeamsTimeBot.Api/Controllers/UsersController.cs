using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using TeamsTimeBot.Api.Authorization;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserSyncService _syncService;
    private readonly UserService _userService;

    public UsersController(
        UserSyncService syncService,
        UserService userService)
    {
        _syncService = syncService;
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var azureId = User.GetObjectId();

        if (string.IsNullOrWhiteSpace(azureId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetByAzureIdAsync(azureId);

        if (user == null)
        {
            return NotFound(
                "Twoje konto nie zostało jeszcze zsynchronizowane z Azure AD.");
        }

        return Ok(user);
    }

    [HttpGet]
    [Admin]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsersAsync();

        return Ok(users);
    }


    [HttpPost("sync")]
    [Admin]
    public async Task<IActionResult> Sync()
    {
        var result = await _syncService.SyncUsersAsync();

        return Ok(result);
    }

    [HttpGet("sync-settings")]
    [Admin]
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
    [Admin]
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
