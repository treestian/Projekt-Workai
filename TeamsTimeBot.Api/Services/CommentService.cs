using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class CommentService
{
    private readonly AppDbContext _dbContext;

    public CommentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaskComment?> AddCommentAsync(
        string userAzureId,
        int taskId,
        string comment)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var taskExists = await _dbContext.Tasks
            .AnyAsync(x => x.Id == taskId);

        if (!taskExists)
        {
            return null;
        }

        var taskComment = new TaskComment
        {
            TaskId = taskId,
            UserId = user.Id,
            Comment = comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskComments.Add(taskComment);

        await _dbContext.SaveChangesAsync();

        return taskComment;
    }
}