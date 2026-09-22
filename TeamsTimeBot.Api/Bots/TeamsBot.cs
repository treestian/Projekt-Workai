using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using TeamsTimeBot.Api.Services;

namespace TeamsTimeBot.Api.Bots;

public class TeamsBot : ActivityHandler
{
    private readonly BotEngine _botEngine;
    private readonly ILogger<TeamsBot> _logger;

    public TeamsBot(
        BotEngine botEngine,
        ILogger<TeamsBot> logger)
    {
        _botEngine = botEngine;
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
            activity.From?.AadObjectId
            ?? activity.From?.Id;

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
                messageText);

        // =========================================================
        // Nie wysyłamy pustej odpowiedzi
        // =========================================================

        if (string.IsNullOrWhiteSpace(response))
        {
            _logger.LogInformation(
                "BotEngine returned no user-facing response.");

            return;
        }

        // =========================================================
        // Wysyłamy odpowiedź do Teams
        // =========================================================

        await turnContext.SendActivityAsync(
            MessageFactory.Text(response),
            cancellationToken);
    }
}

