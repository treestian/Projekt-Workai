using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class AuthorizationService
{
    private readonly AppDbContext _dbContext;

    public AuthorizationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // =========================================================
    // POBIERA UŻYTKOWNIKA
    // =========================================================

    public async Task<User?> GetUserAsync(
        string userAzureId)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(x =>
                x.AzureId == userAzureId &&
                x.IsActive);
    }

    // =========================================================
    // SPRAWDZA CZY UŻYTKOWNIK JEST ADMINEM
    // =========================================================

    public async Task<bool> IsAdminAsync(
        string userAzureId)
    {
        var user =
            await GetUserAsync(userAzureId);

        return user?.Role == UserRole.Admin;
    }

    // =========================================================
    // SPRAWDZA CZY UŻYTKOWNIK MOŻE ZOBACZYĆ DANE
    // INNEGO UŻYTKOWNIKA
    // =========================================================

    public async Task<bool> CanViewUserDataAsync(
        string requestingUserAzureId,
        string targetUserAzureId)
    {
        // Każdy może zobaczyć swoje dane
        if (requestingUserAzureId == targetUserAzureId)
        {
            return true;
        }

        // Dane innych użytkowników może zobaczyć tylko admin
        return await IsAdminAsync(
            requestingUserAzureId);
    }

    // =========================================================
    // SPRAWDZA CZY UŻYTKOWNIK MOŻE ZOBACZYĆ RAPORT ZESPOŁU
    // =========================================================

    public async Task<bool> CanViewTeamReportAsync(
        string userAzureId)
    {
        return await IsAdminAsync(
            userAzureId);
    }

    public async Task<List<User>> FindUsersAsync(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return [];
        }

        search = search.Trim();

        return await _dbContext.Users
            .Where(x =>
                x.IsActive &&
                (
                    (x.DisplayName != null &&
                    x.DisplayName.Contains(search)) ||

                    (x.Email != null &&
                    x.Email.Contains(search)) ||

                    (x.UserPrincipalName != null &&
                    x.UserPrincipalName.Contains(search))
                ))
            .OrderBy(x => x.DisplayName)
            .ToListAsync();
    }


}

