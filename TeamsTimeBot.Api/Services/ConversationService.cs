using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;
using TeamsTimeBot.Api.Models;

namespace TeamsTimeBot.Api.Services;

public class ConversationService
{
    private readonly AppDbContext _dbContext;

    public ConversationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PendingConversation?> GetPendingAsync(
        string userAzureId)
    {
        return await _dbContext.PendingConversations
            .FirstOrDefaultAsync(x =>
                x.UserAzureId == userAzureId &&
                x.ExpiresAt > DateTime.UtcNow);
    }

    public async Task<PendingConversation> GetOrCreateAsync(
        string userAzureId)
    {
        var existing = await GetPendingAsync(userAzureId);

        if (existing != null)
        {
            return existing;
        }

        var conversation = new PendingConversation
        {
            UserAzureId = userAzureId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };

        _dbContext.PendingConversations.Add(conversation);

        await _dbContext.SaveChangesAsync();

        return conversation;
    }

    public async Task AddMessageAsync(
        string userAzureId,
        string role,
        string content)
    {
        var conversation = await GetOrCreateAsync(userAzureId);

        conversation.Messages.Add(
            new ConversationMessage
            {
                Role = role,
                Content = content
            });

        conversation.ExpiresAt =
            DateTime.UtcNow.AddMinutes(30);

        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveAsync(
        PendingConversation conversation)
    {
        conversation.ExpiresAt =
            DateTime.UtcNow.AddMinutes(30);

        var existing = await _dbContext.PendingConversations
            .FirstOrDefaultAsync(x =>
                x.UserAzureId == conversation.UserAzureId);

        if (existing == null)
        {
            _dbContext.PendingConversations.Add(conversation);
        }
        else
        {
            existing.Messages = conversation.Messages;
            existing.CreatedAt = conversation.CreatedAt;
            existing.ExpiresAt = conversation.ExpiresAt;
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(
        string userAzureId)
    {
        var conversations = await _dbContext.PendingConversations
            .Where(x => x.UserAzureId == userAzureId)
            .ToListAsync();

        if (conversations.Count == 0)
        {
            return;
        }

        _dbContext.PendingConversations.RemoveRange(
            conversations);

        await _dbContext.SaveChangesAsync();
    }
}