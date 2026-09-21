using Azure.Identity;
using Microsoft.Graph;

namespace TeamsTimeBot.Api.Services;

public class GraphService
{
    private readonly GraphServiceClient _graphClient;

    public GraphService(
        IConfiguration configuration)
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

    public GraphServiceClient Client => _graphClient;
}