using Microsoft.EntityFrameworkCore;

using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class UserSyncService
{
    private readonly GraphService _graphService;
    private readonly AppDbContext _db;
    private const string AdminAzureId =
    "24940820-21cb-4c70-bb2c-9b088075bb35";

    public UserSyncService(
        GraphService graphService,
        AppDbContext db)
    {
        _graphService = graphService;
        _db = db;
    }

    public async Task<SyncResult> SyncUsersAsync()
    {
        var added = 0;
        var updated = 0;
        var deactivated = 0; 

        var settings = await _db.SyncSettings
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new SyncSettings
            {
                UserSyncIntervalHours = 24,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SyncSettings.Add(settings);

            await _db.SaveChangesAsync();
        }

    
        var deltaResult = await _graphService.GetUsersDeltaAsync(
            settings.UsersDeltaLink);

        var localUsers = await _db.Users
            .ToDictionaryAsync(user => user.AzureId);

        

        foreach (var graphUser in deltaResult.ChangedUsers)
        {
            if (string.IsNullOrWhiteSpace(graphUser.Id))
            {
                continue;
            }

            if (!localUsers.TryGetValue(
                    graphUser.Id,
                    out var existingUser))
            {
                var newUser = new User
                {
                    AzureId = graphUser.Id,
                    DisplayName = graphUser.DisplayName,
                    Email = graphUser.Mail,
                    UserPrincipalName = graphUser.UserPrincipalName,
                    IsActive = true,
                    Role = graphUser.Id == AdminAzureId
                        ? UserRole.Admin
                        : UserRole.Employee,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.Users.Add(newUser);

                added++;

                continue;
            }

            var hasChanges = false;

            if (existingUser.DisplayName != graphUser.DisplayName)
            {
                existingUser.DisplayName = graphUser.DisplayName;
                hasChanges = true;
            }

            if (existingUser.Email != graphUser.Mail)
            {
                existingUser.Email = graphUser.Mail;
                hasChanges = true;
            }

            if (existingUser.UserPrincipalName !=
                graphUser.UserPrincipalName)
            {
                existingUser.UserPrincipalName =
                    graphUser.UserPrincipalName;

                hasChanges = true;
            }
            if (existingUser.AzureId == AdminAzureId &&
                existingUser.Role != UserRole.Admin)
            {
                existingUser.Role = UserRole.Admin;
                hasChanges = true;
            }
            if (!existingUser.IsActive)
            {
                existingUser.IsActive = true;
                hasChanges = true;
            }

            if (hasChanges)
            {
                existingUser.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
        }

        foreach (var deletedAzureId in deltaResult.DeletedUserIds)
        {
            if (!localUsers.TryGetValue(
                    deletedAzureId,
                    out var existingUser))
            {
                continue;
            }

            if (!existingUser.IsActive)
            {
                continue;
            }

            existingUser.IsActive = false;
            existingUser.UpdatedAt = DateTime.UtcNow;

            deactivated++;
        }

        if (!string.IsNullOrWhiteSpace(deltaResult.DeltaLink))
        {
            settings.UsersDeltaLink = deltaResult.DeltaLink;
        }

        settings.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();


        var totalChanges =
            deltaResult.ChangedUsers.Count +
            deltaResult.DeletedUserIds.Count;

        return new SyncResult(
            totalChanges,
            added,
            updated,
            deactivated);
    }
}

public record SyncResult(
    int TotalFromGraph,
    int Added,
    int Updated,
    int Deactivated);