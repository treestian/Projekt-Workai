using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/worklogs")]
public class WorkLogsController : ControllerBase
{
    private readonly WorkLogService _workLogService;
    private readonly AppDbContext _db;

    public WorkLogsController(
        WorkLogService workLogService,
        AppDbContext db)
    {
        _workLogService = workLogService;
        _db = db;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveWork()
    {
        var activeWork = await _db.WorkLogs
            .AsNoTracking()
            .Where(log => log.EndedAt == null)
            .OrderByDescending(log => log.StartedAt)
            .Select(log => new
            {
                id = log.Id,
                startedAt = log.StartedAt,
                status = log.Status,
                task = _db.Tasks
                    .Where(task => task.Id == log.TaskId)
                    .Select(task => new
                    {
                        id = task.Id,
                        name = task.Name
                    })
                    .FirstOrDefault(),
                user = _db.Users
                    .Where(user => user.Id == log.UserId)
                    .Select(user => new
                    {
                        id = user.Id,
                        displayName = user.DisplayName,
                        email = user.Email
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var response = activeWork
            .Select(log => new
            {
                log.id,
                startedAt = DateTime.SpecifyKind(
                    log.startedAt,
                    DateTimeKind.Utc),
                log.status,
                log.task,
                log.user
            });

        return Ok(response);
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