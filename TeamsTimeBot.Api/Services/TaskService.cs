using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;
using TeamsTimeBot.Api.DTOs;

namespace TeamsTimeBot.Api.Services;

public class TaskService
{
    private readonly AppDbContext _dbContext;

    public TaskService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

  public async Task<TaskItem?> CreateTaskAsync(
        string userAzureId,
        string name,
        string? description = null)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.AzureId == userAzureId);

        if (user == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalizedName = name.Trim().ToLower();

        var existingTask = await _dbContext.Tasks
            .FirstOrDefaultAsync(x =>
                x.Name.Trim().ToLower() == normalizedName);

        if (existingTask != null)
        {
            return null;
        }

        var now = DateTime.UtcNow;

        var task = new TaskItem
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            IsCompleted = false,
            CreatedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Tasks.Add(task);

        await _dbContext.SaveChangesAsync();

        return task;
    }

       

    public async Task<List<TaskDto>> GetTasksAsync()
    {
        return await _dbContext.Tasks
            .AsNoTracking()
            .OrderByDescending(task => task.UpdatedAt)
            .Select(task => new TaskDto
            {
                Id = task.Id,
                Name = task.Name,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,

                CreatedBy = new UserSummaryDto
                {
                    Id = task.CreatedBy.Id,
                    DisplayName = task.CreatedBy.DisplayName,
                    Email = task.CreatedBy.Email
                },

                Comments = task.Comments
                    .OrderByDescending(comment => comment.CreatedAt)
                    .Select(comment => new TaskCommentDto
                    {
                        Id = comment.Id,
                        Text = comment.Comment,
                        CreatedAt = comment.CreatedAt,

                        Author = new UserSummaryDto
                        {
                            Id = comment.Author.Id,
                            DisplayName = comment.Author.DisplayName,
                            Email = comment.Author.Email
                        }
                    })
                    .ToList()
            })
            .ToListAsync();
    }


   



    public async Task<TaskItem?> GetByIdAsync(int taskId)
    {
        return await _dbContext.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);
    }

    public async Task<TaskItem?> FinishTaskAsync(int taskId)
    {
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return null;
        }

        if (task.IsCompleted)
        {
            return null;
        }

        task.IsCompleted = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return task;
    }

    public async Task<TaskItem?> EditTaskAsync(
        int taskId,
        string? newName = null,
        string? newDescription = null)
    {
        var task = await _dbContext.Tasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(newName))
        {
            task.Name = newName.Trim();
        }

        if (newDescription != null)
        {
            task.Description = newDescription.Trim();
        }

        task.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return task;
    }
}