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

    public ActionExecutor(
        TaskResolver taskResolver,
        WorkLogService workLogService,
        TaskService taskService,
        CommentService commentService,
        ReportService reportService,
        AuthorizationService authorizationService)
    {
        _taskResolver = taskResolver;
        _workLogService = workLogService;
        _taskService = taskService;
        _commentService = commentService;
        _reportService = reportService;
        _authorizationService = authorizationService;
    }

    public async Task<ActionExecutionResult> StartTimeAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution =
            await _taskResolver.ResolveAsync(
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

        var result =
            await _workLogService.StartWorkAsync(
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
        var resolution =
            await _taskResolver.ResolveAsync(
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

        var result =
            await _workLogService.StopWorkAsync(
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
        var resolution =
            await _taskResolver.ResolveAsync(
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

        var task =
            await _taskService.FinishTaskAsync(
                resolution.Task.Id);

        if (task == null)
        {
            return ActionExecutionResult.Error();
        }

        await _workLogService.StopWorkAsync(
            userAzureId,
            resolution.Task.Id);

        return ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);
    }

    public async Task<ActionExecutionResult> CreateTaskAsync(
        string userAzureId,
        LLMResponse response)
    {
        if (string.IsNullOrWhiteSpace(
            response.NewTaskName))
        {
            return ActionExecutionResult.MissingTaskName();
        }

        var task =
            await _taskService.CreateTaskAsync(
                userAzureId,
                response.NewTaskName,
                response.Description);

        if (task == null)
        {
            return ActionExecutionResult.Error();
        }

        return ActionExecutionResult.Success(
            task.Id,
            task.Name);
    }

    public async Task<ActionExecutionResult> EditTaskAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution =
            await _taskResolver.ResolveAsync(
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

        // =========================================================
        // EDYCJA ZADANIA
        // =========================================================

        var task =
            await _taskService.EditTaskAsync(
                resolution.Task.Id,
                response.NewTaskName,
                response.Description);

        if (task == null)
        {
            return ActionExecutionResult.Error();
        }

        // =========================================================
        // JEŻELI AI PRZEKAZAŁO KOMENTARZ,
        // ZAPISUJEMY GO DO BAZY
        // =========================================================

        if (!string.IsNullOrWhiteSpace(response.Comment))
        {
            var comment =
                await _commentService.AddCommentAsync(
                    userAzureId,
                    resolution.Task.Id,
                    response.Comment);

            if (comment == null)
            {
                return ActionExecutionResult.Error();
            }
        }

        return ActionExecutionResult.Success(
            task.Id,
            task.Name);
    }

    public async Task<ActionExecutionResult> AddCommentAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution =
            await _taskResolver.ResolveAsync(
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

        if (string.IsNullOrWhiteSpace(
            response.Comment))
        {
            return ActionExecutionResult.MissingComment();
        }

        var comment =
            await _commentService.AddCommentAsync(
                userAzureId,
                resolution.Task.Id,
                response.Comment);

        if (comment == null)
        {
            return ActionExecutionResult.Error();
        }

        return ActionExecutionResult.Success(
            resolution.Task.Id,
            resolution.Task.Name);
    }

    public async Task<ActionExecutionResult> AddManualTimeAsync(
        string userAzureId,
        LLMResponse response)
    {
        var resolution =
            await _taskResolver.ResolveAsync(
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

        var result =
            await _workLogService.AddManualTimeAsync(
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

    // =========================================================
    // MÓJ RAPORT CZASU PRACY
    // =========================================================

    public async Task<object> GetMyWorkReportAsync(
        string userAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        var report =
            await _reportService.GetMyReportAsync(
                userAzureId,
                startDate,
                endDate);

        if (report == null)
        {
            return new
            {
                status = "ERROR"
            };
        }

        return new
        {
            status = "SUCCESS",
            report
        };
    }


    // =========================================================
    // RAPORT KONKRETNEGO UŻYTKOWNIKA
    // TYLKO ADMIN
    // =========================================================

    public async Task<object> GetUserWorkReportAsync(
        string requestingUserAzureId,
        string targetUserAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var report =
                await _reportService.GetUserReportAsync(
                    requestingUserAzureId,
                    targetUserAzureId,
                    startDate,
                    endDate);

            if (report == null)
            {
                return new
                {
                    status = "NOT_FOUND"
                };
            }

            return new
            {
                status = "SUCCESS",
                report
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new
            {
                status = "FORBIDDEN"
            };
        }
    }


    // =========================================================
    // RAPORT CAŁEGO ZESPOŁU
    // TYLKO ADMIN
    // =========================================================

    public async Task<object> GetTeamWorkReportAsync(
        string requestingUserAzureId,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            var report =
                await _reportService.GetTeamReportAsync(
                    requestingUserAzureId,
                    startDate,
                    endDate);

            if (report == null)
            {
                return new
                {
                    status = "ERROR"
                };
            }

            return new
            {
                status = "SUCCESS",
                report
            };
        }
        catch (UnauthorizedAccessException)
        {
            return new
            {
                status = "FORBIDDEN"
            };
        }
    }

    public async Task<object> FindUserAsync(
        string requestingUserAzureId,
        string search)
    {
        var isAdmin =
            await _authorizationService.IsAdminAsync(
                requestingUserAzureId);

        if (!isAdmin)
        {
            return new
            {
                status = "FORBIDDEN"
            };
        }

        var users =
            await _authorizationService.FindUsersAsync(search);

        if (users.Count == 0)
        {
            return new
            {
                status = "NOT_FOUND"
            };
        }

        return new
        {
            status = "SUCCESS",
            users = users.Select(x => new
            {
                azureId = x.AzureId,
                displayName = x.DisplayName,
                email = x.Email
            }).ToList()
        };
    }

}

public class ActionExecutionResult
{
    public string Status { get; set; } = string.Empty;

    public int? TaskId { get; set; }

    public string? TaskName { get; set; }

    public List<TeamsTimeBot.Api.DTOs.TaskCandidateDto> Candidates { get; set; }
        = [];

    public static ActionExecutionResult Success(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            Status = "SUCCESS",
            TaskId = taskId,
            TaskName = taskName
        };
    }

    public static ActionExecutionResult NotFound()
    {
        return new ActionExecutionResult
        {
            Status = "NOT_FOUND"
        };
    }

    public static ActionExecutionResult Ambiguous(
        List<TeamsTimeBot.Api.DTOs.TaskCandidateDto> candidates)
    {
        return new ActionExecutionResult
        {
            Status = "AMBIGUOUS",
            Candidates = candidates
        };
    }

    public static ActionExecutionResult NoActiveWork(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            Status = "NO_ACTIVE_WORK",
            TaskId = taskId,
            TaskName = taskName
        };
    }

    public static ActionExecutionResult MissingTaskName()
    {
        return new ActionExecutionResult
        {
            Status = "MISSING_TASK_NAME"
        };
    }

    public static ActionExecutionResult MissingComment()
    {
        return new ActionExecutionResult
        {
            Status = "MISSING_COMMENT"
        };
    }

    public static ActionExecutionResult MissingManualTime()
    {
        return new ActionExecutionResult
        {
            Status = "MISSING_MANUAL_TIME"
        };
    }

    public static ActionExecutionResult Error()
    {
        return new ActionExecutionResult
        {
            Status = "ERROR"
        };
    }

    public static ActionExecutionResult AlreadyActive(
        int taskId,
        string taskName)
    {
        return new ActionExecutionResult
        {
            Status = "ALREADY_ACTIVE",
            TaskId = taskId,
            TaskName = taskName
        };
    }
}

