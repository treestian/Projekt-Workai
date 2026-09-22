using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class UserSyncService
{
    private readonly GraphService _graphService;
    private readonly AppDbContext _db;

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

        var graphUsers = await _graphService.GetAllUsersAsync();

        var activeAzureIds = new HashSet<string>();

        var localUsers = await _db.Users.ToDictionaryAsync(
            user => user.AzureId);

        foreach (var graphUser in graphUsers)
        {
            if (string.IsNullOrWhiteSpace(graphUser.Id))
            {
                continue;
            }

            activeAzureIds.Add(graphUser.Id);

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

        foreach (var localUser in localUsers.Values)
        {
            if (!activeAzureIds.Contains(localUser.AzureId)
                && localUser.IsActive)
            {
                localUser.IsActive = false;
                localUser.UpdatedAt = DateTime.UtcNow;
                deactivated++;
            }
        }

        await _db.SaveChangesAsync();

        return new SyncResult(
            graphUsers.Count,
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