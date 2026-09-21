using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;

namespace TeamsTimeBot.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks()
    {
        var tasks = await _db.Tasks
            .AsNoTracking()
            .OrderByDescending(task => task.UpdatedAt)
            .Select(task => new
            {
                id = task.Id,
                name = task.Name,
                description = task.Description,
                isCompleted = task.IsCompleted,
                createdAt = task.CreatedAt,
                updatedAt = task.UpdatedAt,
                createdBy = _db.Users
                    .Where(user => user.Id == task.CreatedByUserId)
                    .Select(user => new
                    {
                        id = user.Id,
                        displayName = user.DisplayName,
                        email = user.Email
                    })
                    .FirstOrDefault(),
                comments = _db.TaskComments
                    .Where(comment => comment.TaskId == task.Id)
                    .OrderByDescending(comment => comment.CreatedAt)
                    .Select(comment => new
                    {
                        id = comment.Id,
                        text = comment.Comment,
                        createdAt = comment.CreatedAt,
                        author = _db.Users
                            .Where(user => user.Id == comment.UserId)
                            .Select(user => new
                            {
                                id = user.Id,
                                displayName = user.DisplayName,
                                email = user.Email
                            })
                            .FirstOrDefault()
                    })
                    .ToList()
            })
            .ToListAsync();

        return Ok(tasks);
    }
}