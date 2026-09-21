using System.Text.RegularExpressions;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class MessageParserService
{
    public ParsedMessage Parse(string messageText)
    {
        var result = new ParsedMessage
        {
            OriginalText = messageText
        };

        // START
        var startMatch = Regex.Match(
            messageText,
            @"^Rozpoczynam pracę nad zadaniem\s*#(\d+)\s*$",
            RegexOptions.IgnoreCase);

        if (startMatch.Success)
        {
            result.Action = "START";
            result.TaskId = int.Parse(startMatch.Groups[1].Value);

            return result;
        }

        // STOP
        var stopMatch = Regex.Match(
            messageText,
            @"^Kończę pracę nad zadaniem\s*#(\d+)\s*$",
            RegexOptions.IgnoreCase);

        if (stopMatch.Success)
        {
            result.Action = "STOP";
            result.TaskId = int.Parse(stopMatch.Groups[1].Value);

            return result;
        }

        // COMMENT
        var commentMatch = Regex.Match(
            messageText,
            @"^Dodaj komentarz do zadania\s*#(\d+)\s*:\s*(.+)$",
            RegexOptions.IgnoreCase);

        if (commentMatch.Success)
        {
            result.Action = "COMMENT";
            result.TaskId = int.Parse(commentMatch.Groups[1].Value);
            result.Comment = commentMatch.Groups[2].Value.Trim();

            return result;
        }

        result.Action = "UNKNOWN";

        return result;
    }
}