using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.DTOs;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class WorkLogService
{
    private readonly AppDbContext _dbContext;

    public WorkLogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StartWorkResult?> StartWorkAsync(
        string userAzureId,
        int taskId)
    {
        var user =
            await _dbContext.Users
                .FirstOrDefaultAsync(x =>
                    x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        // =========================================================
        // 1. Sprawdź, czy użytkownik już pracuje nad tym zadaniem
        // =========================================================

        var existingWorkLog =
            await _dbContext.WorkLogs
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.TaskId == taskId &&
                    x.EndedAt == null);

        if (existingWorkLog != null)
        {
            Console.WriteLine(
                $"WORKLOG: Already active -> " +
                $"UserId={user.Id}, " +
                $"TaskId={taskId}, " +
                $"WorkLogId={existingWorkLog.Id}");

            return new StartWorkResult(
                existingWorkLog,
                [],
                true);
        }

        // =========================================================
        // 2. Sprawdź, czy użytkownik pracuje nad innym zadaniem
        // =========================================================

        var activeWorkLogs =
            await _dbContext.WorkLogs
                .Where(x =>
                    x.UserId == user.Id &&
                    x.EndedAt == null)
                .ToListAsync();

        // =========================================================
        // 3. Zakończ poprzednie aktywne pomiary
        // =========================================================

        foreach (var activeWorkLog in activeWorkLogs)
        {
            activeWorkLog.EndedAt = now;
            activeWorkLog.Status = "Niedokonczone";

            Console.WriteLine(
                $"WORKLOG: Interrupted -> " +
                $"WorkLogId={activeWorkLog.Id}, " +
                $"TaskId={activeWorkLog.TaskId}");
        }

        // =========================================================
        // 4. Utwórz nowy pomiar
        // =========================================================

        var workLog =
            new WorkLog
            {
                UserId = user.Id,
                TaskId = taskId,
                StartedAt = now,
                EndedAt = null,
                Status = "W trakcie"
            };

        _dbContext.WorkLogs.Add(workLog);

        await _dbContext.SaveChangesAsync();

        Console.WriteLine(
            $"WORKLOG: Started -> " +
            $"WorkLogId={workLog.Id}, " +
            $"UserId={user.Id}, " +
            $"TaskId={taskId}");

        return new StartWorkResult(
            workLog,
            activeWorkLogs,
            false);
    }

    public async Task<WorkLog?> StopWorkAsync(
        string userAzureId,
        int taskId)
    {
        var user =
            await _dbContext.Users
                .FirstOrDefaultAsync(x =>
                    x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var activeWorkLog =
            await _dbContext.WorkLogs
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.TaskId == taskId &&
                    x.EndedAt == null);

        if (activeWorkLog == null)
        {
            return null;
        }

        activeWorkLog.EndedAt =
            DateTime.UtcNow;

        activeWorkLog.Status =
            "Zakonczone";

        await _dbContext.SaveChangesAsync();

        Console.WriteLine(
            $"WORKLOG: Stopped -> " +
            $"WorkLogId={activeWorkLog.Id}, " +
            $"TaskId={taskId}");

        return activeWorkLog;
    }


public async Task<WorkLog?> AddManualTimeAsync(
    string userAzureId,
    int taskId,
    int manualMinutes,
    DateTime? workDate = null)
{
    var user =
        await _dbContext.Users
            .FirstOrDefaultAsync(x =>
                x.AzureId == userAzureId);

    if (user == null)
    {
        return null;
    }

    if (manualMinutes <= 0)
    {
        return null;
    }

    // =========================================================
    // DATA, KTÓREJ DOTYCZY CZAS PRACY
    //
    // Jeżeli użytkownik nie podał daty,
    // używamy dzisiejszej daty.
    // =========================================================

    var date =
        (workDate ?? DateTime.UtcNow).Date;

    // =========================================================
    // RĘCZNY WPIS CZASU
    //
    // Jeżeli użytkownik podał np. 120 minut za 20.09,
    // zapisujemy:
    //
    // 20.09 00:00 → 20.09 02:00
    //
    // Dzięki temu wpis należy do właściwego dnia.
    // =========================================================

    var startedAt =
        date;

    var endedAt =
        startedAt.AddMinutes(manualMinutes);

    var workLog =
        new WorkLog
        {
            UserId = user.Id,
            TaskId = taskId,
            StartedAt = startedAt,
            EndedAt = endedAt,
            Status = "Zakonczone"
        };

    _dbContext.WorkLogs.Add(workLog);

    await _dbContext.SaveChangesAsync();

    Console.WriteLine(
        $"WORKLOG: Manual time -> " +
        $"WorkLogId={workLog.Id}, " +
        $"TaskId={taskId}, " +
        $"Minutes={manualMinutes}, " +
        $"Date={date:yyyy-MM-dd}");

    return workLog;
}



    public async Task<WorkLogSummaryDto?> GetSummaryAsync(
        string userAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var user =
            await _dbContext.Users
                .FirstOrDefaultAsync(x =>
                    x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var logs =
            await _dbContext.WorkLogs
                .Where(x =>
                    x.UserId == user.Id &&
                    x.EndedAt.HasValue &&
                    x.StartedAt >= startDate &&
                    x.StartedAt <= endDate)
                .ToListAsync();

        var taskIds =
            logs
                .Select(x => x.TaskId)
                .Distinct()
                .ToList();

        var tasks =
            await _dbContext.Tasks
                .Where(x =>
                    taskIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.Name);

        var taskSummaries =
            logs
                .GroupBy(x => x.TaskId)
                .Select(group =>
                {
                    var minutes =
                        (int)Math.Round(
                            group.Sum(x =>
                                (x.EndedAt!.Value -
                                 x.StartedAt)
                                    .TotalMinutes));

                    return new WorkLogTaskSummaryDto
                    {
                        TaskId = group.Key,

                        TaskName =
                            tasks.TryGetValue(
                                group.Key,
                                out var taskName)
                                ? taskName
                                : "Nieznane zadanie",

                        Minutes = minutes,

                        Hours =
                            Math.Round(
                                minutes / 60.0,
                                2)
                    };
                })
                .OrderByDescending(
                    x => x.Minutes)
                .ToList();

        var totalMinutes =
            taskSummaries.Sum(
                x => x.Minutes);

        return new WorkLogSummaryDto
        {
            StartDate = startDate,
            EndDate = endDate,

            TotalMinutes =
                totalMinutes,

            TotalHours =
                Math.Round(
                    totalMinutes / 60.0,
                    2),

            Tasks =
                taskSummaries
        };
    }

    public async Task<WorkLog?> StopActiveWorkAsync(
        string userAzureId,
        int taskId)
    {
        var user =
            await _dbContext.Users
                .FirstOrDefaultAsync(x =>
                    x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        var activeWorkLog =
            await _dbContext.WorkLogs
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.TaskId == taskId &&
                    x.EndedAt == null);

        if (activeWorkLog == null)
        {
            return null;
        }

        activeWorkLog.EndedAt =
            DateTime.UtcNow;

        activeWorkLog.Status =
            "Zakonczone";

        await _dbContext.SaveChangesAsync();

        return activeWorkLog;
    }
}

public sealed record StartWorkResult(
    WorkLog WorkLog,
    IReadOnlyList<WorkLog> InterruptedWorkLogs,
    bool AlreadyActive);
