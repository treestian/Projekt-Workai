using TeamsTimeBot.Api.DTOs;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class ActionExecutor
{
    private readonly TaskResolver _taskResolver;
    private readonly WorkLogService _workLogService;
    private readonly TaskService _taskService;
    private readonly CommentService _commentService;
    private readonly ReportService _reportService;
    private readonly AuthorizationService _authorizationService;
    private readonly PendingMentions _pendingMentions;

    public ActionExecutor(
        TaskResolver taskResolver,
        WorkLogService workLogService,
        TaskService taskService,
        CommentService commentService,
        ReportService reportService,
        AuthorizationService authorizationService,
        PendingMentions pendingMentions)
    {
        _taskResolver = taskResolver;
        _workLogService = workLogService;
        _taskService = taskService;
        _commentService = commentService;
        _reportService = reportService;
        _authorizationService = authorizationService;
        _pendingMentions = pendingMentions;
    }

    private async Task<string> GetActorNameAsync(string userAzureId)
    {
        var actor = await _authorizationService.GetUserAsync(userAzureId);

        return actor?.DisplayName ?? "Ktoś";
    }

    private async Task NotifyWorkersAsync(
        IEnumerable<TaskWorker> workers,
        string actorUserAzureId,
        Func<string, string> buildMessage)
    {
        var affected = workers
            .Where(x => x.AzureId != actorUserAzureId)
            .ToList();

        if (affected.Count == 0)
        {
            return;
        }

        var actorName = await GetActorNameAsync(actorUserAzureId);

        foreach (var worker in affected)
        {
            _pendingMentions.Add(
                worker.AzureId,
                worker.DisplayName,
                buildMessage(actorName));
        }
    }

    public async Task<ActionExecutionResult> StartTimeAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution = await _taskResolver.ResolveAsync(
            response.TaskId,
            response.TaskName);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return ActionExecutionResult.Ambiguous(
                resolution.GetCandidateDtos());
        }

        if (resolution.Task == null)
        {
            return ActionExecutionResult.Error();
        }

        var result = await _workLogService.StartWorkAsync(
            userAzureId,
            resolution.Task.Id);

        if (result == null)
        {
            return ActionExecutionResult.Error();
        }

        if (result.AlreadyActive)
        {
            return ActionExecutionResult.AlreadyActive(
                resolution.Task.Id,
                resolution.Task.Name);
        }

        return ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);
    }

    public async Task<ActionExecutionResult> StopTimeAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution = await _taskResolver.ResolveAsync(
            response.TaskId,
            response.TaskName,
            includeCompleted: true);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return ActionExecutionResult.Ambiguous(
                resolution.GetCandidateDtos());
        }

        if (resolution.Task == null)
        {
            return ActionExecutionResult.Error();
        }

        var result = await _workLogService.StopWorkAsync(
            userAzureId,
            resolution.Task.Id);

        if (result == null)
        {
            return ActionExecutionResult.NoActiveWork(
                resolution.Task.Id,
                resolution.Task.Name);
        }

        return ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);
    }

    public async Task<ActionExecutionResult> FinishTaskAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution = await _taskResolver.ResolveAsync(
            response.TaskId,
            response.TaskName);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return ActionExecutionResult.Ambiguous(
                resolution.GetCandidateDtos());
        }

        if (resolution.Task == null)
        {
            return ActionExecutionResult.Error();
        }

        var result = await _taskService.FinishTaskAsync(
            resolution.Task.Id);

        if (result.Status == TaskFinishStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (result.Status == TaskFinishStatus.AlreadyCompleted)
        {
            return ActionExecutionResult.AlreadyCompleted(
                resolution.Task.Id,
                resolution.Task.Name);
        }

        var stoppedWorkers =
            await _workLogService.StopAllActiveForTaskAsync(
                resolution.Task.Id);

        await NotifyWorkersAsync(
            stoppedWorkers,
            userAzureId,
            actorName =>
                $"{actorName} zakończył(a) zadanie „{resolution.Task.Name}”, "
                + "więc zatrzymałem Twój pomiar czasu.");

        var success = ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);

        success.StoppedWorkLogs = stoppedWorkers.Count;

        return success;
    }

    public async Task<ActionExecutionResult> CreateTaskAsync(
        string userAzureId,
        LLMResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.NewTaskName))
        {
            return ActionExecutionResult.MissingTaskName();
        }

        var result = await _taskService.CreateTaskAsync(
            userAzureId,
            response.NewTaskName,
            response.Description);

        return result.Status switch
        {
            TaskCreateStatus.Created =>
                ActionExecutionResult.Success(
                    result.Task!.Id,
                    result.Task.Name),

            TaskCreateStatus.DuplicateName =>
                ActionExecutionResult.DuplicateTaskName(
                    result.Task!.Id,
                    result.Task.Name),

            TaskCreateStatus.InvalidName =>
                ActionExecutionResult.MissingTaskName(),

            _ => ActionExecutionResult.Error()
        };
    }

    public async Task<ActionExecutionResult> EditTaskAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution = await _taskResolver.ResolveAsync(
            response.TaskId,
            response.TaskName,
            includeCompleted: true);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return ActionExecutionResult.Ambiguous(
                resolution.GetCandidateDtos());
        }

        if (resolution.Task == null)
        {
            return ActionExecutionResult.Error();
        }

        var result = await _taskService.EditTaskAsync(
            resolution.Task.Id,
            response.NewTaskName,
            response.Description);

        if (result.Status == TaskEditStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (result.Status == TaskEditStatus.DuplicateName)
        {
            return ActionExecutionResult.DuplicateTaskName(
                resolution.Task.Id,
                resolution.Task.Name);
        }

        var task = result.Task!;

        if (!string.IsNullOrWhiteSpace(response.Comment))
        {
            var comment = await _commentService.AddCommentAsync(
                userAzureId,
                resolution.Task.Id,
                response.Comment);

            if (comment == null)
            {
                return ActionExecutionResult.Error();
            }

            var workers = await _workLogService.GetActiveWorkersAsync(
                resolution.Task.Id);

            await NotifyWorkersAsync(
                workers,
                userAzureId,
                actorName =>
                    $"{actorName} dodał(a) komentarz do zadania "
                    + $"„{resolution.Task.Name}”, nad którym pracujesz: "
                    + $"„{response.Comment.Trim()}”");
        }

        return ActionExecutionResult.Success(
            task.Id,
            task.Name);
    }

    public async Task<ActionExecutionResult> AddCommentAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution = await _taskResolver.ResolveAsync(
            response.TaskId,
            response.TaskName,
            includeCompleted: true);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return ActionExecutionResult.Ambiguous(
                resolution.GetCandidateDtos());
        }

        if (resolution.Task == null)
        {
            return ActionExecutionResult.Error();
        }

        if (string.IsNullOrWhiteSpace(response.Comment))
        {
            return ActionExecutionResult.MissingComment();
        }

        var comment = await _commentService.AddCommentAsync(
            userAzureId,
            resolution.Task.Id,
            response.Comment);

        if (comment == null)
        {
            return ActionExecutionResult.Error();
        }

        var workers = await _workLogService.GetActiveWorkersAsync(
            resolution.Task.Id);

        await NotifyWorkersAsync(
            workers,
            userAzureId,
            actorName =>
                $"{actorName} dodał(a) komentarz do zadania "
                + $"„{resolution.Task.Name}”, nad którym pracujesz: "
                + $"„{response.Comment.Trim()}”");

        return ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);
    }

    public async Task<ActionExecutionResult> AddManualTimeAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution = await _taskResolver.ResolveAsync(
            response.TaskId,
            response.TaskName,
            includeCompleted: true);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return ActionExecutionResult.NotFound();
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return ActionExecutionResult.Ambiguous(
                resolution.GetCandidateDtos());
        }

        if (resolution.Task == null)
        {
            return ActionExecutionResult.Error();
        }

        if (!response.ManualMinutes.HasValue ||
            response.ManualMinutes.Value <= 0)
        {
            return ActionExecutionResult.MissingManualTime();
        }

        var result = await _workLogService.AddManualTimeAsync(
            userAzureId,
            resolution.Task.Id,
            response.ManualMinutes.Value,
            response.WorkDate);

        if (result == null)
        {
            return ActionExecutionResult.Error();
        }

        return ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);
    }

    public async Task<WorkReportResult> GetMyWorkReportAsync(
        string userAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var report = await _reportService.GetMyReportAsync(
            userAzureId,
            startDate,
            endDate);

        if (report == null)
        {
            return new WorkReportResult
            {
                StatusCode = 500
            };
        }

        return new WorkReportResult
        {
            StatusCode = 200,
            Report = report
        };
    }

    public async Task<WorkReportResult> GetUserWorkReportAsync(
        string requestingUserAzureId,
        string targetUserAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var report = await _reportService.GetUserReportAsync(
                requestingUserAzureId,
                targetUserAzureId,
                startDate,
                endDate);

            if (report == null)
            {
                return new WorkReportResult
                {
                    StatusCode = 404
                };
            }

            return new WorkReportResult
            {
                StatusCode = 200,
                Report = report
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new WorkReportResult
            {
                StatusCode = 403
            };
        }
    }

    public async Task<TeamWorkReportResult> GetTeamWorkReportAsync(
        string requestingUserAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var report = await _reportService.GetTeamReportAsync(
                requestingUserAzureId,
                startDate,
                endDate);

            if (report == null)
            {
                return new TeamWorkReportResult
                {
                    StatusCode = 500
                };
            }

            return new TeamWorkReportResult
            {
                StatusCode = 200,
                Report = report
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new TeamWorkReportResult
            {
                StatusCode = 403
            };
        }
    }

    public async Task<TaskWorkReportResult> GetTaskWorkReportAsync(
        string requestingUserAzureId,
        int? taskId,
        string? taskName,
        DateTime startDate,
        DateTime endDate)
    {
        var resolution = await _taskResolver.ResolveAsync(
            taskId,
            taskName,
            includeCompleted: true);

        if (resolution.Status == TaskResolutionStatus.NotFound)
        {
            return new TaskWorkReportResult
            {
                StatusCode = 404
            };
        }

        if (resolution.Status == TaskResolutionStatus.Ambiguous)
        {
            return new TaskWorkReportResult
            {
                StatusCode = 409,
                Candidates = resolution.GetCandidateDtos()
            };
        }

        if (resolution.Task == null)
        {
            return new TaskWorkReportResult
            {
                StatusCode = 500
            };
        }

        var report = await _reportService.GetTaskReportAsync(
            requestingUserAzureId,
            resolution.Task.Id,
            startDate,
            endDate);

        if (report == null)
        {
            return new TaskWorkReportResult
            {
                StatusCode = 404
            };
        }

        return new TaskWorkReportResult
        {
            StatusCode = 200,
            Report = report
        };
    }

    public async Task<FindUserResult> FindUserAsync(
        string requestingUserAzureId,
        string search)
    {
        var isAdmin = await _authorizationService.IsAdminAsync(
            requestingUserAzureId);

        if (!isAdmin)
        {
            return new FindUserResult
            {
                StatusCode = 403
            };
        }

        var users = await _authorizationService.FindUsersAsync(search);

        if (users.Count == 0)
        {
            return new FindUserResult
            {
                StatusCode = 404
            };
        }

        return new FindUserResult
        {
            StatusCode = 200,
            Users = users
                .Select(user => new UserSearchResult
                {
                    AzureId = user.AzureId,
                    DisplayName = user.DisplayName,
                    Email = user.Email
                })
                .ToList()
        };
    }
}

public class ActionExecutionResult
{
    public int StatusCode { get; set; }

    public string Result { get; set; } = string.Empty;

    public int? TaskId { get; set; }

    public string? TaskName { get; set; }

    public int StoppedWorkLogs { get; set; }

    public List<TaskCandidateDto> Candidates { get; set; } = [];

    public static ActionExecutionResult Success(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            StatusCode = 200,
            Result = "SUCCESS",
            TaskId = taskId,
            TaskName = taskName
        };
    }

    public static ActionExecutionResult NotFound()
    {
        return new ActionExecutionResult
        {
            StatusCode = 404,
            Result = "NOT_FOUND"
        };
    }

    public static ActionExecutionResult Ambiguous(
        List<TaskCandidateDto> candidates)
    {
        return new ActionExecutionResult
        {
            StatusCode = 409,
            Result = "AMBIGUOUS",
            Candidates = candidates
        };
    }

    public static ActionExecutionResult NoActiveWork(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            StatusCode = 404,
            Result = "NO_ACTIVE_WORK",
            TaskId = taskId,
            TaskName = taskName
        };
    }

    public static ActionExecutionResult MissingTaskName()
    {
        return new ActionExecutionResult
        {
            StatusCode = 400,
            Result = "MISSING_TASK_NAME"
        };
    }

    public static ActionExecutionResult MissingComment()
    {
        return new ActionExecutionResult
        {
            StatusCode = 400,
            Result = "MISSING_COMMENT"
        };
    }

    public static ActionExecutionResult MissingManualTime()
    {
        return new ActionExecutionResult
        {
            StatusCode = 400,
            Result = "MISSING_MANUAL_TIME"
        };
    }

    public static ActionExecutionResult Error()
    {
        return new ActionExecutionResult
        {
            StatusCode = 500,
            Result = "ERROR"
        };
    }

    public static ActionExecutionResult DuplicateTaskName(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            StatusCode = 409,
            Result = "DUPLICATE_TASK_NAME",
            TaskId = taskId,
            TaskName = taskName
        };
    }

    public static ActionExecutionResult AlreadyCompleted(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            StatusCode = 409,
            Result = "ALREADY_COMPLETED",
            TaskId = taskId,
            TaskName = taskName
        };
    }

    public static ActionExecutionResult AlreadyActive(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            StatusCode = 409,
            Result = "ALREADY_ACTIVE",
            TaskId = taskId,
            TaskName = taskName
        };
    }
}

public class WorkReportResult
{
    public int StatusCode { get; set; }

     public string Status => StatusCode switch
    {
        200 => "SUCCESS",
        403 => "FORBIDDEN",
        404 => "NOT_FOUND",
        _   => "ERROR"
    };


    public WorkReport? Report { get; set; }
}

public class TeamWorkReportResult
{
    public int StatusCode { get; set; }

    public string Status => StatusCode switch
    {
        200 => "SUCCESS",
        403 => "FORBIDDEN",
        404 => "NOT_FOUND",
        _   => "ERROR"
    };

    public TeamWorkReport? Report { get; set; }
}

public class TaskWorkReportResult
{
    public int StatusCode { get; set; }

    public string Status => StatusCode switch
    {
        200 => "SUCCESS",
        403 => "FORBIDDEN",
        404 => "NOT_FOUND",
        409 => "AMBIGUOUS",
        _   => "ERROR"
    };

    public TaskWorkReport? Report { get; set; }

    public List<TaskCandidateDto> Candidates { get; set; } = [];
}

public class FindUserResult
{
    public int StatusCode { get; set; }
     public string Status => StatusCode switch
    {
        200 => "SUCCESS",
        403 => "FORBIDDEN",
        404 => "NOT_FOUND",
        _   => "ERROR"
    };


    public List<UserSearchResult> Users { get; set; } = [];
}

public class UserSearchResult
{
    public string? AzureId { get; set; }

    public string? DisplayName { get; set; }

    public string? Email { get; set; }
}