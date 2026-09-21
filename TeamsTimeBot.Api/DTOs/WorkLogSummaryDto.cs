namespace TeamsTimeBot.Api.DTOs;

public class WorkLogSummaryDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int TotalMinutes { get; set; }

    public double TotalHours { get; set; }

    public List<WorkLogTaskSummaryDto> Tasks { get; set; } = new();
}

public class WorkLogTaskSummaryDto
{
    public int TaskId { get; set; }

    public string TaskName { get; set; } = string.Empty;

    public int Minutes { get; set; }

    public double Hours { get; set; }
}