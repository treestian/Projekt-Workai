using System.Security.Claims;
using Microsoft.Identity.Web;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Extensions;

public static class UserClaimsPrincipalExtensions
{
    public static string? GetAzureId(this ClaimsPrincipal user)
    {
        return user.GetObjectId();
    }

    public static async Task<bool> IsAdminAsync(
        this ClaimsPrincipal user,
        AuthorizationService authorizationService)
    {
        var userAzureId = user.GetAzureId();

        if (string.IsNullOrWhiteSpace(userAzureId))
        {
            return false;
        }

        return await authorizationService.IsAdminAsync(userAzureId);
    }
}

