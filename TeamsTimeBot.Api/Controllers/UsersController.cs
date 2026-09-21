using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly GraphService _graphService;
    private readonly UserSyncService _syncService;
    private readonly AppDbContext _db;

    public UsersController(
        GraphService graphService,
        UserSyncService syncService,
        AppDbContext db)
    {
        _graphService = graphService;
        _syncService = syncService;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .Select(u => new
            {
                id = u.Id,
                azureId = u.AzureId,
                displayName = u.DisplayName,
                email = u.Email,
                userPrincipalName = u.UserPrincipalName,
                isActive = u.IsActive,
                createdAt = u.CreatedAt,
                updatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("sync-test")]
    public async Task<IActionResult> SyncTest()
    {
        var users = await _graphService.Client.Users
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "displayName",
                    "mail",
                    "userPrincipalName"
                };

                config.QueryParameters.Top = 20;
            });

        return Ok(users?.Value);
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
        var settings = await GetOrCreateSyncSettingsAsync();

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
        if (request.IntervalHours is < 1 or > 168)
        {
            return BadRequest("Interwał musi wynosić od 1 do 168 godzin.");
        }

        var settings = await GetOrCreateSyncSettingsAsync();
        settings.UserSyncIntervalHours = request.IntervalHours;
        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            intervalHours = settings.UserSyncIntervalHours,
            updatedAt = settings.UpdatedAt
        });
    }

    private async Task<SyncSettings> GetOrCreateSyncSettingsAsync()
    {
        var settings = await _db.SyncSettings.FirstOrDefaultAsync();

        if (settings != null)
        {
            return settings;
        }

        settings = new SyncSettings
        {
            UserSyncIntervalHours = 24,
            UpdatedAt = DateTime.UtcNow
        };

        _db.SyncSettings.Add(settings);
        await _db.SaveChangesAsync();

        return settings;
    }


public record UpdateSyncSettingsRequest(int IntervalHours);
}