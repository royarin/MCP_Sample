extern alias AzureIdentity;

using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace OpsBot.Shared;

/// <summary>Configuration + chat-client factory shared by the OpsBot.* console hosts.</summary>
public static class AgentConfig
{
    public const string UserSecretsId = "opsdesk-mcp-demos";

    /// <summary>appsettings.json → appsettings.Development.json → user-secrets → env → --Key=value / --ask "...".</summary>
    public static IConfigurationRoot Build(string[] args) => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
        .AddUserSecrets(UserSecretsId, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    public static string Deployment(IConfiguration c) =>
        c["Foundry:DeploymentName"] ?? c["AzureOpenAI:DeploymentName"] ?? c["AzureOpenAI:Deployment"] ?? "gpt-4o";

    /// <summary>Foundry project chat client wrapped in <see cref="ToolTraceChatClient"/>.</summary>
    public static IChatClient CreateChatClient(IConfiguration c, UsageMeter meter)
    {
        var endpoint = c["Foundry:ProjectEndpoint"] ?? throw new InvalidOperationException(
            "Foundry:ProjectEndpoint is not configured in appsettings.Development.json.");
        var project = new AIProjectClient(new Uri(endpoint), CreateCredential());
        IChatClient inner = project.GetProjectOpenAIClient()
            .GetProjectResponsesClientForModel(Deployment(c))
            .AsIChatClient();
        return new ToolTraceChatClient(inner, meter);
    }

    public static TokenCredential CreateCredential(
        string? tenantId = null,
        string? clientId = null,
        string? clientSecret = null)
    {
        if (string.IsNullOrWhiteSpace(clientSecret))
            return new AzureIdentity::Azure.Identity.DefaultAzureCredential();

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Mcp:Entra:TenantId and Mcp:Entra:ClientId are required with Mcp:Entra:ClientSecret.");

        return new AzureIdentity::Azure.Identity.ClientSecretCredential(tenantId, clientId, clientSecret);
    }

    public static PermissionGate.Mode PermissionMode(IConfiguration c) =>
        string.Equals(c["permissions"], "auto", StringComparison.OrdinalIgnoreCase) ? PermissionGate.Mode.Auto : PermissionGate.Mode.Ask;
}
