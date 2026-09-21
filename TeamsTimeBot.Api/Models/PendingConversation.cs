using System.Text.Json;

namespace TeamsTimeBot.Api.Models;

public class PendingConversation
{
    public int Id { get; set; }

    public string UserAzureId { get; set; } = string.Empty;

    public List<ConversationMessage> Messages { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}