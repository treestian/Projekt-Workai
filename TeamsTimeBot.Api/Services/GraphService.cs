using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace TeamsTimeBot.Api.Services;

public class GraphService
{
    private readonly GraphServiceClient _graphClient;

    public GraphService(IConfiguration configuration)
    {
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        var credential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret);

        _graphClient = new GraphServiceClient(
            credential,
            new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = new List<User>();

        var response = await _graphClient.Users.GetAsync(config =>
        {
            config.QueryParameters.Select = new[]
            {
                "id",
                "displayName",
                "mail",
                "userPrincipalName"
            };

            config.QueryParameters.Top = 999;
        });

        while (response != null)
        {
            if (response.Value != null)
            {
                users.AddRange(response.Value);
            }

            if (string.IsNullOrEmpty(response.OdataNextLink))
            {
                break;
            }

            response = await _graphClient.Users
                .WithUrl(response.OdataNextLink)
                .GetAsync();
        }

        return users;
    }

    public async Task<Microsoft.Graph.Models.ChatMessage?> GetChannelMessageAsync(
        string teamId,
        string channelId,
        string messageId)
    {
        return await _graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .GetAsync();
    }
    public async Task<Subscription?> CreateChannelMessageSubscriptionAsync(
        string teamId,
        string channelId,
        string notificationUrl,
        string clientState)
    {
        var subscription = new Subscription
        {
            ChangeType = "created",
            NotificationUrl = notificationUrl,
            Resource = $"/teams/{teamId}/channels/{channelId}/messages",
            ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(50),
            ClientState = clientState
        };

        return await _graphClient.Subscriptions.PostAsync(subscription);
    }
    public async Task<List<Team>> GetTeamsAsync()
    {
        var response = await _graphClient.Teams.GetAsync(config =>
        {
            config.QueryParameters.Select = new[]
            {
                "id",
                "displayName",
                "description",
                "visibility"
            };
        });

        return response?.Value ?? new List<Team>();
    }

    public async Task<List<Channel>> GetChannelsAsync(string teamId)
    {
        var response = await _graphClient
            .Teams[teamId]
            .Channels
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "displayName",
                    "description",
                    "membershipType"
                };
            });

        return response?.Value ?? new List<Channel>();
    }

    public async Task<List<ChatMessage>> GetChannelMessagesAsync(
        string teamId,
        string channelId)
    {
        var response = await _graphClient
            .Teams[teamId]
            .Channels[channelId]
            .Messages
            .GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id",
                    "createdDateTime",
                    "subject",
                    "body",
                    "from"
                };

                config.QueryParameters.Top = 20;
            });

        return response?.Value ?? new List<ChatMessage>();
    }


    public async Task<List<User>> GetUsersAsync(int limit = 20)
    {
        var response = await _graphClient.Users.GetAsync(config =>
        {
            config.QueryParameters.Select = new[]
            {
                "id",
                "displayName",
                "mail",
                "userPrincipalName"
            };

            config.QueryParameters.Top = limit;
        });

        return response?.Value ?? new List<User>();
    }



}