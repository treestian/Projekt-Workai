using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ToListAsync();
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

