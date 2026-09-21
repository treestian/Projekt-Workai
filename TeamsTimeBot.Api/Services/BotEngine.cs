using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class BotEngine
{
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
        string message)
    {
        if (string.IsNullOrWhiteSpace(userAzureId))
        {
            return "❌ Nie udało się rozpoznać użytkownika.";
        }
        Console.WriteLine(
            $"BOT USER AZURE ID: {userAzureId}");

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

        // =========================================================
        // AI obsługuje całą logikę:
        //
        // AI → Tool → Backend → Tool Result → AI → odpowiedź
        // =========================================================

        var response =
            await _aiService.GetResponseAsync(
                conversation.Messages,
                userAzureId);

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
        }

        await _conversationService.SaveAsync(conversation);

        return response;
    }
}
