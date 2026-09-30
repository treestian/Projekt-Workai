using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class ReportService
{
    private readonly AppDbContext _dbContext;
    private readonly AuthorizationService _authorizationService;

    public ReportService(
        AppDbContext dbContext,
        AuthorizationService authorizationService)
    {
        _dbContext = dbContext;
        _authorizationService = authorizationService;
    }

    // =========================================================
    // RAPORT WŁASNEGO CZASU
    // =========================================================

    public async Task<WorkReport?> GetMyReportAsync(
        string userAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var user =
            await _authorizationService.GetUserAsync(
                userAzureId);

        if (user == null)
        {
            return null;
        }

        return await BuildUserReportAsync(
            user,
            startDate,
            endDate);
    }

    // =========================================================
    // RAPORT KONKRETNEGO UŻYTKOWNIKA
    // TYLKO ADMIN
    // =========================================================

    public async Task<WorkReport?> GetUserReportAsync(
        string requestingUserAzureId,
        string targetUserAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var canView =
            await _authorizationService
                .CanViewUserDataAsync(
                    requestingUserAzureId,
                    targetUserAzureId);

        if (!canView)
        {
            throw new UnauthorizedAccessException();
        }

        var user =
            await _authorizationService.GetUserAsync(
                targetUserAzureId);

        if (user == null)
        {
            return null;
        }

        return await BuildUserReportAsync(
            user,
            startDate,
            endDate);
    }

    // =========================================================
    // RAPORT CAŁEGO ZESPOŁU
    // TYLKO ADMIN
    // =========================================================

    public async Task<TeamWorkReport?> GetTeamReportAsync(
        string requestingUserAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var canView =
            await _authorizationService
                .CanViewTeamReportAsync(
                    requestingUserAzureId);

        if (!canView)
        {
            throw new UnauthorizedAccessException();
        }

        var users =
            await _dbContext.Users
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayName)
                .ToListAsync();

        var start = PolandTime.ToUtc(startDate.Date);
        var end = PolandTime.ToUtc(endDate.Date.AddDays(1));

        var userIds =
            users
                .Select(x => x.Id)
                .ToList();

        var logs =
            await _dbContext.WorkLogs
                .AsNoTracking()
                .Where(x =>
                    userIds.Contains(x.UserId) &&
                    x.EndedAt.HasValue &&
                    x.StartedAt >= start &&
                    x.StartedAt < end)
                .ToListAsync();

        var taskNames =
            await GetTaskNamesAsync(logs);

        var logsByUser =
            logs
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList());

        var reports =
            users
                .Select(user =>
                    BuildReport(
                        user,
                        logsByUser.TryGetValue(
                            user.Id,
                            out var userLogs)
                            ? userLogs
                            : [],
                        taskNames,
                        startDate,
                        endDate))
                .ToList();

        return new TeamWorkReport
        {
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Users = reports,
            TotalMinutes =
                reports.Sum(x => x.TotalMinutes)
        };
    }

    // =========================================================
    // RAPORT KONKRETNEGO ZADANIA
    //
    // Pracownik:
    //   → widzi tylko swój czas
    //
    // Admin:
    //   → widzi czas wszystkich
    // =========================================================

    public async Task<TaskWorkReport?> GetTaskReportAsync(
        string requestingUserAzureId,
        int taskId,
        DateTime startDate,
        DateTime endDate)
    {
        var requestingUser =
            await _authorizationService.GetUserAsync(
                requestingUserAzureId);

        if (requestingUser == null)
        {
            return null;
        }

        var task =
            await _dbContext.Tasks
                .FirstOrDefaultAsync(x =>
                    x.Id == taskId);

        if (task == null)
        {
            return null;
        }

        var start =
            PolandTime.ToUtc(startDate.Date);

        var end =
            PolandTime.ToUtc(endDate.Date.AddDays(1));

        var query =
            _dbContext.WorkLogs
                .Where(x =>
                    x.TaskId == taskId &&
                    x.EndedAt.HasValue &&
                    x.StartedAt >= start &&
                    x.StartedAt < end);

        // Pracownik widzi tylko swój czas
        if (requestingUser.Role != UserRole.Admin)
        {
            query =
                query.Where(x =>
                    x.UserId == requestingUser.Id);
        }

        var logs =
            await query.ToListAsync();

        var logUserIds =
            logs
                .Select(x => x.UserId)
                .Distinct()
                .ToList();

        var userNames =
            await _dbContext.Users
                .AsNoTracking()
                .Where(x => logUserIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.DisplayName);

        var totalMinutes =
            logs.Sum(x =>
                (int)Math.Round(
                    (x.EndedAt!.Value -
                     x.StartedAt)
                        .TotalMinutes));

        return new TaskWorkReport
        {
            TaskId = task.Id,
            TaskName = task.Name,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalMinutes = totalMinutes,
            Users =
                logs
                    .GroupBy(x => x.UserId)
                    .Select(group =>
                    {
                        var minutes =
                            group.Sum(x =>
                                (int)Math.Round(
                                    (x.EndedAt!.Value -
                                     x.StartedAt)
                                        .TotalMinutes));

                        return new UserTaskWorkReport
                        {
                            UserId = group.Key,
                            UserName =
                                userNames.TryGetValue(
                                    group.Key,
                                    out var displayName)
                                    ? displayName ?? "Nieznany użytkownik"
                                    : "Nieznany użytkownik",
                            Minutes = minutes
                        };
                    })
                    .OrderByDescending(
                        x => x.Minutes)
                    .ToList()
        };
    }

    // =========================================================
    // BUDOWANIE RAPORTU UŻYTKOWNIKA
    // =========================================================

    private async Task<WorkReport?> BuildUserReportAsync(
        User user,
        DateTime startDate,
        DateTime endDate)
    {
        var start =
            PolandTime.ToUtc(startDate.Date);

        var end =
            PolandTime.ToUtc(endDate.Date.AddDays(1));

        var logs =
            await _dbContext.WorkLogs
                .AsNoTracking()
                .Where(x =>
                    x.UserId == user.Id &&
                    x.EndedAt.HasValue &&
                    x.StartedAt >= start &&
                    x.StartedAt < end)
                .ToListAsync();

        var taskNames =
            await GetTaskNamesAsync(logs);

        return BuildReport(
            user,
            logs,
            taskNames,
            startDate,
            endDate);
    }

    private async Task<Dictionary<int, string>> GetTaskNamesAsync(
        List<WorkLog> logs)
    {
        var taskIds =
            logs
                .Select(x => x.TaskId)
                .Distinct()
                .ToList();

        return await _dbContext.Tasks
            .AsNoTracking()
            .Where(x =>
                taskIds.Contains(x.Id))
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name);
    }

    private static WorkReport BuildReport(
        User user,
        List<WorkLog> logs,
        Dictionary<int, string> tasks,
        DateTime startDate,
        DateTime endDate)
    {
        var taskReports =
            logs
                .GroupBy(x => x.TaskId)
                .Select(group =>
                {
                    var minutes =
                        group.Sum(x =>
                            (int)Math.Round(
                                (x.EndedAt!.Value -
                                 x.StartedAt)
                                    .TotalMinutes));

                    return new TaskTimeReport
                    {
                        TaskId = group.Key,
                        TaskName =
                            tasks.TryGetValue(
                                group.Key,
                                out var name)
                                ? name
                                : "Nieznane zadanie",
                        Minutes = minutes
                    };
                })
                .OrderByDescending(
                    x => x.Minutes)
                .ToList();

        return new WorkReport
        {
            UserId = user.Id,
            AzureId = user.AzureId,
            UserName =
                user.DisplayName
                ?? user.Email
                ?? "Nieznany użytkownik",
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            TotalMinutes =
                taskReports.Sum(
                    x => x.Minutes),
            Tasks = taskReports
        };
    }
}

// =============================================================
// DTO RAPORTÓW
// =============================================================

public class WorkReport
{
    public int UserId { get; set; }

    public string AzureId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalMinutes { get; set; }

    public double TotalHours =>
        Math.Round(
            TotalMinutes / 60.0,
            2);

    public List<TaskTimeReport> Tasks { get; set; } = [];
}

public class TaskTimeReport
{
    public int TaskId { get; set; }

    public string TaskName { get; set; } = string.Empty;

    public int Minutes { get; set; }

    public double Hours =>
        Math.Round(
            Minutes / 60.0,
            2);
}

public class TeamWorkReport
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalMinutes { get; set; }

    public double TotalHours =>
        Math.Round(
            TotalMinutes / 60.0,
            2);

    public List<WorkReport> Users { get; set; } = [];
}

public class TaskWorkReport
{
    public int TaskId { get; set; }

    public string TaskName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalMinutes { get; set; }

    public double TotalHours =>
        Math.Round(
            TotalMinutes / 60.0,
            2);

    public List<UserTaskWorkReport> Users { get; set; } = [];
}

public class UserTaskWorkReport
{
    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int Minutes { get; set; }

    public double Hours =>
        Math.Round(
            Minutes / 60.0,
            2);
}
