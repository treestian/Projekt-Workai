using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.DTOs;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ToListAsync();

        return users
            .Select(MapToDto)
            .ToList();
    }

    public async Task<UserDto?> GetByAzureIdAsync(string azureId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AzureId == azureId &&
                x.IsActive);

        return user == null
            ? null
            : MapToDto(user);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            AzureId = user.AzureId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            UserPrincipalName = user.UserPrincipalName,
            IsActive = user.IsActive,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }


    public async Task<SyncSettings> GetSyncSettingsAsync()
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

    public async Task<SyncSettings?> UpdateSyncSettingsAsync(
        int intervalHours)
    {
        if (intervalHours is < 1 or > 168)
        {
            return null;
        }

        var settings = await GetSyncSettingsAsync();

        settings.UserSyncIntervalHours = intervalHours;
        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return settings;
    }
}

