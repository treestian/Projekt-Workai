using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Bots;

public class TeamsBot : ActivityHandler
{
    private readonly BotEngine _botEngine;
    private readonly PendingMentions _pendingMentions;
    private readonly PendingChoice _pendingChoice;
    private readonly ILogger<TeamsBot> _logger;

    public TeamsBot(
        BotEngine botEngine,
        PendingMentions pendingMentions,
        PendingChoice pendingChoice,
        ILogger<TeamsBot> logger)
    {
        _botEngine = botEngine;
        _pendingMentions = pendingMentions;
        _pendingChoice = pendingChoice;
        _logger = logger;
    }

    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        var activity =
            turnContext.Activity;

        var messageText =
            activity.Text?.Trim()
            ?? string.Empty;

        var senderId =
            activity.From?.AadObjectId;

        _logger.LogInformation(
            "Received Teams activity. SenderId: {SenderId}, Text: {Text}",
            senderId,
            messageText);

        // =========================================================
        // Brak wiadomości
        // =========================================================

        if (string.IsNullOrWhiteSpace(messageText))
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text(
                    " Nie otrzymałem żadnej wiadomości."),
                cancellationToken);

            return;
        }

        // =========================================================
        // Przekazujemy wiadomość do BotEngine
        // =========================================================

        var response =
            await _botEngine.HandleAsync(
                senderId ?? string.Empty,
                messageText,
                cancellationToken);

        // =========================================================
        // Nie wysyłamy pustej odpowiedzi
        // =========================================================

        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogInformation(
                "BotEngine returned no user-facing response.");

            await SendMentionsAsync(
                turnContext,
                cancellationToken);

            return;
        }

        // =========================================================
        // Wysyłamy odpowiedź do Teams
        // =========================================================

        if (_pendingChoice.HasChoice)
        {
            await turnContext.SendActivityAsync(
                BuildChoiceActivity(response),
                cancellationToken);
        }
        else
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text(response),
                cancellationToken);
        }

        await SendMentionsAsync(
            turnContext,
            cancellationToken);
    }

    private IMessageActivity BuildChoiceActivity(
        string fallbackQuestion)
    {
        var question = _pendingChoice.Question ?? fallbackQuestion;

        var card = new
        {
            type = "AdaptiveCard",
            version = "1.4",

            body = new object[]
            {
                new
                {
                    type = "TextBlock",
                    text = question,
                    wrap = true
                }
            },

            actions = _pendingChoice.Options
                .Select(option => new
                {
                    type = "Action.Submit",
                    title = option,
                    data = new
                    {
                        msteams = new
                        {
                            type = "imBack",
                            value = option
                        }
                    }
                })
                .ToArray()
        };

        var attachment = new Attachment
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = JObject.FromObject(card)
        };

        return MessageFactory.Attachment(attachment);
    }

    private async Task SendMentionsAsync(
        ITurnContext turnContext,
        CancellationToken cancellationToken)
    {
        if (_pendingMentions.Items.Count == 0)
        {
            return;
        }

        foreach (var pending in _pendingMentions.Items)
        {
            try
            {
                var member = await TeamsInfo.GetMemberAsync(
                    turnContext,
                    pending.AzureId,
                    cancellationToken);

                if (member == null || string.IsNullOrWhiteSpace(member.Id))
                {
                    _logger.LogWarning(
                        "Nie znaleziono członka zespołu dla {AzureId}.",
                        pending.AzureId);

                    continue;
                }

                var name = member.Name
                    ?? pending.DisplayName
                    ?? "użytkowniku";

                var escapedName = name
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;");

                var mention = new Mention
                {
                    Type = "mention",
                    Mentioned = new ChannelAccount(member.Id, name),
                    Text = $"<at>{escapedName}</at>"
                };

                var activity = MessageFactory.Text(
                    $"{mention.Text} {pending.Message}");

                activity.Entities = new List<Entity> { mention };

                await turnContext.SendActivityAsync(
                    activity,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Nie udało się oznaczyć użytkownika {AzureId}.",
                    pending.AzureId);
            }
        }
    }
}

