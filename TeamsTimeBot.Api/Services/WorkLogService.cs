using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.DTOs;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class WorkLogService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WorkLogService> _logger;

    public WorkLogService(
        AppDbContext dbContext,
        ILogger<WorkLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StartWorkResult?> StartWorkAsync(
        string userAzureId,
        int taskId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var existingWorkLog = await _dbContext.WorkLogs
            .FirstOrDefaultAsync(x =>
                x.UserId == user.Id &&
                x.TaskId == taskId &&
                x.EndedAt == null);

        if (existingWorkLog != null)
        {
            _logger.LogInformation(
                "Work log already active. " +
                "UserId: {UserId}, TaskId: {TaskId}, WorkLogId: {WorkLogId}",
                user.Id,
                taskId,
                existingWorkLog.Id);

            return new StartWorkResult(
                existingWorkLog,
                [],
                true);
        }

        var activeWorkLogs = await _dbContext.WorkLogs
            .Where(x =>
                x.UserId == user.Id &&
                x.EndedAt == null)
            .ToListAsync();

        foreach (var activeWorkLog in activeWorkLogs)
        {
            activeWorkLog.EndedAt = now;
            activeWorkLog.Status = "Niedokonczone";

            _logger.LogInformation(
                "Interrupted active work log. " +
                "WorkLogId: {WorkLogId}, TaskId: {TaskId}",
                activeWorkLog.Id,
                activeWorkLog.TaskId);
        }

        var workLog = new WorkLog
        {
            UserId = user.Id,
            TaskId = taskId,
            StartedAt = now,
            EndedAt = null,
            Status = "W trakcie"
        };

        _dbContext.WorkLogs.Add(workLog);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Work started. " +
            "WorkLogId: {WorkLogId}, UserId: {UserId}, TaskId: {TaskId}",
            workLog.Id,
            user.Id,
            taskId);

        return new StartWorkResult(
            workLog,
            activeWorkLogs,
            false);
    }

    public async Task<WorkLog?> StopWorkAsync(
        string userAzureId,
        int taskId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var activeWorkLog = await _dbContext.WorkLogs
            .FirstOrDefaultAsync(x =>
                x.UserId == user.Id &&
                x.TaskId == taskId &&
                x.EndedAt == null);

        if (activeWorkLog == null)
        {
            return null;
        }

        activeWorkLog.EndedAt = DateTime.UtcNow;
        activeWorkLog.Status = "Zakonczone";

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Work stopped. " +
            "WorkLogId: {WorkLogId}, TaskId: {TaskId}",
            activeWorkLog.Id,
            taskId);

        return activeWorkLog;
    }

    public async Task<int> StopAllActiveForTaskAsync(int taskId)
    {
        var activeWorkLogs = await _dbContext.WorkLogs
            .Where(x =>
                x.TaskId == taskId &&
                x.EndedAt == null)
            .ToListAsync();

        if (activeWorkLogs.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;

        foreach (var workLog in activeWorkLogs)
        {
            workLog.EndedAt = now;
            workLog.Status = "Zakonczone";
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Stopped {Count} active work logs after finishing task {TaskId}.",
            activeWorkLogs.Count,
            taskId);

        return activeWorkLogs.Count;
    }

    public async Task<WorkLog?> AddManualTimeAsync(
        string userAzureId,
        int taskId,
        int manualMinutes,
        DateTime? workDate = null)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        if (manualMinutes is <= 0 or > 24 * 60)
        {
            return null;
        }

        var date = (workDate ?? PolandTime.Now).Date;

        var startedAt = PolandTime.ToUtc(date);
        var endedAt = startedAt.AddMinutes(manualMinutes);

        var workLog = new WorkLog
        {
            UserId = user.Id,
            TaskId = taskId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            Status = "Zakonczone"
        };

        _dbContext.WorkLogs.Add(workLog);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Manual work time added. " +
            "WorkLogId: {WorkLogId}, TaskId: {TaskId}, " +
            "Minutes: {Minutes}, Date: {Date}",
            workLog.Id,
            taskId,
            manualMinutes,
            date);

        return workLog;
    }

    public async Task<List<ActiveWorkDto>> GetActiveWorkAsync(
        string userAzureId,
        bool isAdmin)
    {
        var query = _dbContext.WorkLogs
            .AsNoTracking()
            .Where(log => log.EndedAt == null);

        if (!isAdmin)
        {
            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.AzureId == userAzureId &&
                    x.IsActive);

            if (user == null)
            {
                return [];
            }

            query = query.Where(log => log.UserId == user.Id);
        }

        var activeWork = await query
            .OrderByDescending(log => log.StartedAt)
            .Select(log => new ActiveWorkDto
            {
                Id = log.Id,
                StartedAt = log.StartedAt,
                Status = log.Status,

                Task = _dbContext.Tasks
                    .Where(task => task.Id == log.TaskId)
                    .Select(task => new ActiveWorkTaskDto
                    {
                        Id = task.Id,
                        Name = task.Name
                    })
                    .FirstOrDefault(),

                User = _dbContext.Users
                    .Where(user => user.Id == log.UserId)
                    .Select(user => new ActiveWorkUserDto
                    {
                        Id = user.Id,
                        DisplayName = user.DisplayName,
                        Email = user.Email
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var workLog in activeWork)
        {
            workLog.StartedAt = DateTime.SpecifyKind(
                workLog.StartedAt,
                DateTimeKind.Utc);
        }

        return activeWork;
    }


    public async Task<WorkLogSummaryDto?> GetSummaryAsync(
        string userAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var logs = await _dbContext.WorkLogs
            .Where(x =>
                x.UserId == user.Id &&
                x.EndedAt.HasValue &&
                x.StartedAt >= start &&
                x.StartedAt < end)
            .ToListAsync();


        var taskIds = logs
            .Select(x => x.TaskId)
            .Distinct()
            .ToList();

        var tasks = await _dbContext.Tasks
            .Where(x => taskIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name);

        var taskSummaries = logs
            .GroupBy(x => x.TaskId)
            .Select(group =>
            {
                var minutes = (int)Math.Round(
                    group.Sum(x =>
                        (x.EndedAt!.Value - x.StartedAt)
                            .TotalMinutes));

                return new WorkLogTaskSummaryDto
                {
                    TaskId = group.Key,

                    TaskName = tasks.TryGetValue(
                        group.Key,
                        out var taskName)
                        ? taskName
                        : "Nieznane zadanie",

                    Minutes = minutes,

                    Hours = Math.Round(
                        minutes / 60.0,
                        2)
                };
            })
            .OrderByDescending(x => x.Minutes)
            .ToList();

        var totalMinutes = taskSummaries.Sum(x => x.Minutes);

        return new WorkLogSummaryDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalMinutes = totalMinutes,

            TotalHours = Math.Round(
                totalMinutes / 60.0,
                2),

            Tasks = taskSummaries
        };
    }
}

public sealed record StartWorkResult(
    WorkLog WorkLog,
    IReadOnlyList<WorkLog> InterruptedWorkLogs,
    bool AlreadyActive);