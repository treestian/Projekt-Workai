using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class ConversationService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private readonly AppDbContext _dbContext;

    public ConversationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PendingConversation> GetOrCreateAsync(
        string userAzureId)
    {
        var now = DateTime.UtcNow;

        var conversations = await _dbContext.PendingConversations
            .Where(x => x.UserAzureId == userAzureId)
            .ToListAsync();

        var expired = conversations
            .Where(x => x.ExpiresAt <= now)
            .ToList();

        if (expired.Count > 0)
        {
            _dbContext.PendingConversations.RemoveRange(expired);
        }

        var active = conversations
            .FirstOrDefault(x => x.ExpiresAt > now);

        if (active != null)
        {
            if (expired.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }

            return active;
        }

        var conversation = new PendingConversation
        {
            UserAzureId = userAzureId,
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime)
        };

        _dbContext.PendingConversations.Add(conversation);

        await _dbContext.SaveChangesAsync();

        return conversation;
    }

    public async Task SaveAsync(PendingConversation conversation)
    {
        conversation.ExpiresAt = DateTime.UtcNow.Add(Lifetime);

        await _dbContext.SaveChangesAsync();
    }
}
