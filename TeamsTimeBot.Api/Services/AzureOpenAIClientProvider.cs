#pragma warning disable OPENAI001

using Azure.Identity;
using OpenAI.Responses;
using System.ClientModel.Primitives;

namespace TeamsTimeBot.Api.Services;

public class AzureOpenAIClientProvider
{
    public ResponsesClient Client { get; }

    public string DeploymentName { get; }

    public AzureOpenAIClientProvider(IConfiguration configuration)
    {
        var endpoint =
            configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException(
                "AzureOpenAI:Endpoint is not configured.");

        DeploymentName =
            configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException(
                "AzureOpenAI:DeploymentName is not configured.");

        var tenantId =
            configuration["AzureAd:TenantId"]
            ?? throw new InvalidOperationException(
                "AzureAd:TenantId is not configured.");

        var clientId =
            configuration["AzureAd:ClientId"]
            ?? throw new InvalidOperationException(
                "AzureAd:ClientId is not configured.");

        var clientSecret =
            configuration["AzureAd:ClientSecret"]
            ?? throw new InvalidOperationException(
                "AzureAd:ClientSecret is not configured.");

        var credential =
            new ClientSecretCredential(
                tenantId,
                clientId,
                clientSecret);

        var policy =
            new BearerTokenPolicy(
                credential,
                "https://cognitiveservices.azure.com/.default");

        var options =
            new ResponsesClientOptions
            {
                Endpoint = new Uri(endpoint)
            };

        Client =
            new ResponsesClient(
                policy,
                options);
    }
}

#pragma warning restore OPENAI001
