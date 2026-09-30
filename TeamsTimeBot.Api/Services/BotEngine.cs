using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class BotEngine
{
    private const int MaxHistoryMessages = 20;

    private readonly AIService _aiService;
    private readonly ConversationService _conversationService;

    public BotEngine(
        AIService aiService,
        ConversationService conversationService)
    {
        _aiService = aiService;
        _conversationService = conversationService;
    }

    public async Task<string?> HandleAsync(
        string userAzureId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userAzureId))
        {
            return "Nie udało się rozpoznać użytkownika.";
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var conversation =
            await _conversationService.GetOrCreateAsync(userAzureId);

        conversation.Messages.Add(
            new ConversationMessage
            {
                Role = "user",
                Content = message.Trim()
            });

        TrimHistory(conversation.Messages);

        // =========================================================
        // AI obsługuje całą logikę:
        //
        // AI → Tool → Backend → Tool Result → AI → odpowiedź
        // =========================================================

        var response =
            await _aiService.GetResponseAsync(
                conversation.Messages,
                userAzureId,
                cancellationToken);

        // =========================================================
        // Zapisz odpowiedź AI do historii
        // =========================================================

        if (!string.IsNullOrWhiteSpace(response))
        {
            conversation.Messages.Add(
                new ConversationMessage
                {
                    Role = "assistant",
                    Content = response
                });

            TrimHistory(conversation.Messages);
        }

        await _conversationService.SaveAsync(conversation);

        return response;
    }

    private static void TrimHistory(
        List<ConversationMessage> messages)
    {
        var excess = messages.Count - MaxHistoryMessages;

        if (excess > 0)
        {
            messages.RemoveRange(0, excess);
        }
    }
}
