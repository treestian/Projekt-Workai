using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder;

namespace TeamsTimeBot.Api.Bots;

public class AdapterWithErrorHandler : CloudAdapter
{
    private readonly ILogger<CloudAdapter> _logger;

    public AdapterWithErrorHandler(
        BotFrameworkAuthentication authentication,
        ILogger<CloudAdapter> logger)
        : base(authentication, logger)
    {
        _logger = logger;

        OnTurnError = async (turnContext, exception) =>
        {
            _logger.LogError(
                exception,
                "Błąd podczas obsługi wiadomości bota.");

            await turnContext.SendActivityAsync(
                MessageFactory.Text(
                    "Wystąpił błąd podczas obsługi wiadomości."));
        };
    }
}