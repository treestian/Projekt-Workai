using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("teams")]
    public async Task<IActionResult> ReceiveNotification(
        [FromQuery] string? validationToken)
    {
        if (!string.IsNullOrEmpty(validationToken))
        {
            return Content(validationToken, "text/plain");
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        _logger.LogInformation(
            "Received Teams notification.");

        await _notificationService.ProcessNotificationAsync(body);

        return Ok();
    }
}

