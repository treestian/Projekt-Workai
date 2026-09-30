namespace TeamsTimeBot.Api.Services;

public class PendingChoice
{
    private const int MaxOptions = 6;

    public string? Question { get; private set; }

    public List<string> Options { get; } = [];

    public bool HasChoice => Options.Count > 0;

    public void Clear()
    {
        Question = null;
        Options.Clear();
    }

    public void Set(
        string? question,
        IEnumerable<string?> options)
    {
        Options.Clear();

        Question = string.IsNullOrWhiteSpace(question)
            ? null
            : question.Trim();

        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option))
            {
                continue;
            }

            var trimmed = option.Trim();

            if (Options.Contains(trimmed))
            {
                continue;
            }

            Options.Add(trimmed);

            if (Options.Count == MaxOptions)
            {
                break;
            }
        }
    }
}
