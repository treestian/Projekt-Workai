using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeamsTimeBot.Api.Extensions;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(
        AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        var authorizationService =
            context.HttpContext.RequestServices
                .GetRequiredService<AuthorizationService>();

        if (!await user.IsAdminAsync(authorizationService))
        {
            context.Result = new ForbidResult();
        }
    }
}
