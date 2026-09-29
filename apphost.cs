#:package Aspire.Hosting.JavaScript@13.5.4
#:sdk Aspire.AppHost.Sdk@13.5.4
#:property AspireUseCliBundle=true

#pragma warning disable ASPIRECERTIFICATES001
#pragma warning disable ASPIREJAVASCRIPT001
#pragma warning disable ASPIRECSHARPAPPS001

var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddCSharpApp(
        "api",
        "./TeamsTimeBot.Api/TeamsTimeBot.Api.csproj"
    )
    .WithHttpEndpoint(
        port: 80,
        name: "http"
    )
    .WithExternalHttpEndpoints();

var web = builder
    .AddDockerfile(
        "web",
        "./TeamsTimeBot.Web"
    )
    .WithHttpEndpoint(
        port: 3000,
        env: "PORT"
    )
    .WithReference(api)
    .WithEnvironment(
        "NUXT_PUBLIC_API_URL",
        "https://api.victoriouspond-25b7f784.northeurope.azurecontainerapps.io"
    )
    .WithEnvironment(
        "NUXT_PUBLIC_AZURE_CLIENT_ID",
        "5b1a2444-7399-4278-a4c3-ad96ea6271ce"
    )
    .WithEnvironment(
        "NUXT_PUBLIC_AZURE_TENANT_ID",
        "5452339e-6596-48fb-acc1-27bcdf09abf2"
    )
    .WithExternalHttpEndpoints();
    
builder.Build().Run();