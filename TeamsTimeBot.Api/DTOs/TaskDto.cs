namespace TeamsTimeBot.Api.DTOs;

public class TaskDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public UserSummaryDto? CreatedBy { get; set; }

    public List<TaskCommentDto> Comments { get; set; } = [];
}

public class TaskCommentDto
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public UserSummaryDto? Author { get; set; }
}

public class UserSummaryDto
{
    public int Id { get; set; }

    public string? DisplayName { get; set; }

    public string? Email { get; set; }
}
