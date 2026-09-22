using Microsoft.AspNetCore.Mvc;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/worklogs")]
public class WorkLogsController : ControllerBase
{
    private readonly WorkLogService _workLogService;

    public WorkLogsController(WorkLogService workLogService)
    {
        _workLogService = workLogService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveWork()
    {
        var activeWork = await _workLogService.GetActiveWorkAsync();

        return Ok(activeWork);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string userAzureId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(userAzureId))
        {
            return BadRequest("Brak userAzureId.");
        }

        if (startDate > endDate)
        {
            return BadRequest(
                "Data początkowa nie może być późniejsza niż końcowa.");
        }

        var summary = await _workLogService.GetSummaryAsync(
            userAzureId,
            startDate,
            endDate);

        if (summary == null)
        {
            return NotFound("Nie znaleziono użytkownika.");
        }

        return Ok(summary);
    }
}
