using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace OpsBot.Shared;

/// <summary>Configuration + chat-client factory shared by the OpsBot.* console hosts.</summary>
public static class AgentConfig
{
    public const string UserSecretsId = "opsdesk-mcp-demos";

    /// <summary>appsettings.json → appsettings.Development.json → user-secrets → env → --Key=value / --ask "...".</summary>
    public static IConfigurationRoot Build(string[] args) => new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
        .AddUserSecrets(UserSecretsId, reloadOnChange: false)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    public static string Deployment(IConfiguration c) => c["AzureOpenAI:DeploymentName"] ?? c["AzureOpenAI:Deployment"] ?? "gpt-4o";

    /// <summary>Azure OpenAI chat client wrapped in <see cref="ToolTraceChatClient"/>.</summary>
    public static IChatClient CreateChatClient(IConfiguration c, UsageMeter meter)
    {
        var endpoint = c["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException(
            "AzureOpenAI:Endpoint not configured. Run: dotnet user-secrets set AzureOpenAI:Endpoint https://<resource>.openai.azure.com/");
        var apiKey = c["AzureOpenAI:ApiKey"];

        AzureOpenAIClient azure = string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())      // az login
            : new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        IChatClient inner = azure.GetChatClient(Deployment(c)).AsIChatClient();
        return new ToolTraceChatClient(inner, meter);
    }

    public static PermissionGate.Mode PermissionMode(IConfiguration c) =>
        string.Equals(c["permissions"], "auto", StringComparison.OrdinalIgnoreCase) ? PermissionGate.Mode.Auto : PermissionGate.Mode.Ask;
}
