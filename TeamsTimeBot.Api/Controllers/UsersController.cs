using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserSyncService _syncService;
    private readonly UserService _userService;
    private readonly AuthorizationService _authorizationService;

    public UsersController(
        UserSyncService syncService,
        UserService userService,
        AuthorizationService authorizationService)
    {
        _syncService = syncService;
        _userService = userService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetUsersAsync();

        return Ok(users);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync()
    {
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

        var result = await _syncService.SyncUsersAsync();

        return Ok(result);
    }

    [HttpGet("sync-settings")]
    public async Task<IActionResult> GetSyncSettings()
    {
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

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
        if (!await IsCurrentUserAdminAsync())
        {
            return Forbid();
        }

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

    private async Task<bool> IsCurrentUserAdminAsync()
    {
        var userAzureId = User.FindFirst("oid")?.Value;

        if (string.IsNullOrWhiteSpace(userAzureId))
        {
            return false;
        }

        return await _authorizationService.IsAdminAsync(userAzureId);
    }
}

public record UpdateSyncSettingsRequest(int IntervalHours);
