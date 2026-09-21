using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.DTOs;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class TaskResolver
{
    private readonly AppDbContext _dbContext;

    public TaskResolver(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TaskResolutionResult> ResolveAsync(
        int? taskId,
        string? taskName,
        bool includeCompleted = false)
    {
        // =========================================================
        // 1. Mamy konkretne ID
        // =========================================================

        if (taskId.HasValue)
        {
            var task = await _dbContext.Tasks
                .FirstOrDefaultAsync(x =>
                    x.Id == taskId.Value);

            if (task == null)
            {
                return TaskResolutionResult.NotFound();
            }

            if (!includeCompleted && task.IsCompleted)
            {
                return TaskResolutionResult.NotFound();
            }

            return TaskResolutionResult.Found(task);
        }

        // =========================================================
        // 2. Nie mamy ani ID, ani nazwy
        // =========================================================

        if (string.IsNullOrWhiteSpace(taskName))
        {
            return TaskResolutionResult.NotFound();
        }

        var searchText =
            Normalize(taskName);

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return TaskResolutionResult.NotFound();
        }

        // =========================================================
        // 3. Pobieramy zadania
        // =========================================================

        var query =
            _dbContext.Tasks.AsQueryable();

        if (!includeCompleted)
        {
            query = query.Where(x =>
                !x.IsCompleted);
        }

        var tasks =
            await query.ToListAsync();

        if (tasks.Count == 0)
        {
            return TaskResolutionResult.NotFound();
        }

        // =========================================================
        // 4. DOKŁADNE dopasowanie nazwy
        // =========================================================

        var exactMatches =
            tasks
                .Where(x =>
                    Normalize(x.Name) == searchText)
                .ToList();

        // Jedno dokładne dopasowanie
        if (exactMatches.Count == 1)
        {
            Console.WriteLine(
                $"TASK RESOLVER: Exact match -> " +
                $"ID={exactMatches[0].Id}, " +
                $"Name={exactMatches[0].Name}");

            return TaskResolutionResult.Found(
                exactMatches[0]);
        }

        // Kilka dokładnie takich samych nazw
        if (exactMatches.Count > 1)
        {
            Console.WriteLine(
                $"TASK RESOLVER: Ambiguous exact match -> " +
                $"found {exactMatches.Count} tasks.");

            return TaskResolutionResult.Ambiguous(
                exactMatches);
        }

        // =========================================================
        // 5. Dopasowanie całej frazy
        // =========================================================

        var phraseMatches =
            tasks
                .Where(x =>
                    Normalize(x.Name)
                        .Contains(searchText))
                .ToList();

        if (phraseMatches.Count == 1)
        {
            Console.WriteLine(
                $"TASK RESOLVER: Phrase match -> " +
                $"ID={phraseMatches[0].Id}, " +
                $"Name={phraseMatches[0].Name}");

            return TaskResolutionResult.Found(
                phraseMatches[0]);
        }

        if (phraseMatches.Count > 1)
        {
            var ranked =
                RankTasks(
                    phraseMatches,
                    searchText);

            Console.WriteLine(
                $"TASK RESOLVER: Ambiguous phrase match -> " +
                $"found {phraseMatches.Count} tasks.");

            return TaskResolutionResult.Ambiguous(
                ranked);
        }

        // =========================================================
        // 6. Dopasowanie wszystkich słów
        // =========================================================

        var searchWords =
            searchText
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

        if (searchWords.Count == 0)
        {
            return TaskResolutionResult.NotFound();
        }

        var wordMatches =
            tasks
                .Where(task =>
                {
                    var normalizedName =
                        Normalize(task.Name);

                    return searchWords.All(
                        word =>
                            normalizedName.Contains(word));
                })
                .ToList();

        if (wordMatches.Count == 1)
        {
            Console.WriteLine(
                $"TASK RESOLVER: Word match -> " +
                $"ID={wordMatches[0].Id}, " +
                $"Name={wordMatches[0].Name}");

            return TaskResolutionResult.Found(
                wordMatches[0]);
        }

        if (wordMatches.Count > 1)
        {
            var ranked =
                RankTasks(
                    wordMatches,
                    searchText);

            Console.WriteLine(
                $"TASK RESOLVER: Ambiguous word match -> " +
                $"found {wordMatches.Count} tasks.");

            return TaskResolutionResult.Ambiguous(
                ranked);
        }

        // =========================================================
        // 7. Częściowe dopasowanie
        // =========================================================

        var partialMatches =
            tasks
                .Where(task =>
                {
                    var normalizedName =
                        Normalize(task.Name);

                    return searchWords.Any(
                        word =>
                            normalizedName.Contains(word));
                })
                .ToList();

        if (partialMatches.Count == 1)
        {
            Console.WriteLine(
                $"TASK RESOLVER: Partial match -> " +
                $"ID={partialMatches[0].Id}, " +
                $"Name={partialMatches[0].Name}");

            return TaskResolutionResult.Found(
                partialMatches[0]);
        }

        if (partialMatches.Count > 1)
        {
            var ranked =
                RankTasks(
                    partialMatches,
                    searchText);

            Console.WriteLine(
                $"TASK RESOLVER: Ambiguous partial match -> " +
                $"found {partialMatches.Count} tasks.");

            return TaskResolutionResult.Ambiguous(
                ranked);
        }

        // =========================================================
        // 8. Nic nie znaleziono
        // =========================================================

        Console.WriteLine(
            $"TASK RESOLVER: Not found -> \"{taskName}\"");

        return TaskResolutionResult.NotFound();
    }

    private static List<TaskItem> RankTasks(
        IEnumerable<TaskItem> tasks,
        string searchText)
    {
        var searchWords =
            searchText
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

        return tasks
            .Select(task =>
            {
                var normalizedName =
                    Normalize(task.Name);

                var score = 0;

                // Cała szukana fraza występuje w nazwie
                if (normalizedName.Contains(searchText))
                {
                    score += 100;
                }

                // Nazwa zaczyna się od szukanej frazy
                if (normalizedName.StartsWith(searchText))
                {
                    score += 50;
                }

                // Każde pasujące słowo
                foreach (var word in searchWords)
                {
                    if (normalizedName.Contains(word))
                    {
                        score += 10;
                    }
                }

                return new
                {
                    Task = task,
                    Score = score
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Task.UpdatedAt)
            .Select(x => x.Task)
            .ToList();
    }

    private static string Normalize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized =
            value
                .Trim()
                .ToLowerInvariant();

        normalized =
            normalized.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            var category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC);
    }
}

public class TaskResolutionResult
{
    public TaskResolutionStatus Status
    {
        get;
        private set;
    }

    public TaskItem? Task
    {
        get;
        private set;
    }

    public IReadOnlyList<TaskItem> Candidates
    {
        get;
        private set;
    } = [];

    private TaskResolutionResult()
    {
    }

    public static TaskResolutionResult Found(
        TaskItem task)
    {
        return new TaskResolutionResult
        {
            Status =
                TaskResolutionStatus.Found,

            Task = task
        };
    }

    public static TaskResolutionResult NotFound()
    {
        return new TaskResolutionResult
        {
            Status =
                TaskResolutionStatus.NotFound
        };
    }

    public static TaskResolutionResult Ambiguous(
        IReadOnlyList<TaskItem> candidates)
    {
        return new TaskResolutionResult
        {
            Status =
                TaskResolutionStatus.Ambiguous,

            Candidates = candidates
        };
    }

    public List<TaskCandidateDto> GetCandidateDtos()
    {
        return Candidates
            .Select(x =>
                new TaskCandidateDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    IsCompleted = x.IsCompleted
                })
            .ToList();
    }
}

public enum TaskResolutionStatus
{
    Found,
    NotFound,
    Ambiguous
}
