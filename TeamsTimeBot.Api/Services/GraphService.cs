using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Delta;

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


    public async Task<UserDeltaResult> GetUsersDeltaAsync(
        string? deltaLink = null)
    {
        var changedUsers = new List<User>();
        var deletedUserIds = new List<string>();

        string? nextLink = deltaLink;
        string? finalDeltaLink = null;

        DeltaGetResponse? response;

        if (string.IsNullOrWhiteSpace(deltaLink))
        {
            response = await _graphClient.Users.Delta
                .GetAsDeltaGetResponseAsync(config =>
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
        }
        else
        {
            response = await _graphClient.Users.Delta
                .WithUrl(deltaLink)
                .GetAsDeltaGetResponseAsync();
        }

        while (response != null)
        {
            if (response.Value != null)
            {
                foreach (var user in response.Value)
                {
                    if (string.IsNullOrWhiteSpace(user.Id))
                    {
                        continue;
                    }

                    if (user.AdditionalData != null &&
                        user.AdditionalData.ContainsKey("@removed"))
                    {
                        deletedUserIds.Add(user.Id);
                    }
                    else
                    {
                        changedUsers.Add(user);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(response.OdataNextLink))
            {
                nextLink = response.OdataNextLink;

                response = await _graphClient.Users.Delta
                    .WithUrl(response.OdataNextLink)
                    .GetAsDeltaGetResponseAsync();

                continue;
            }

            finalDeltaLink = response.OdataDeltaLink;
            break;
        }

        return new UserDeltaResult(
            changedUsers,
            deletedUserIds,
            finalDeltaLink);
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

public record UserDeltaResult(
    List<User> ChangedUsers,
    List<string> DeletedUserIds,
    string? DeltaLink);