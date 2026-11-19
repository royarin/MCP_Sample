using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using OpsBot.Enterprise;
using OpsBot.Shared;
using System.Text.Json;

if (args.Any(a => a.Split('=')[0] is "--ask" or "--compare"))
{
    Ui.Error("OpsBot.Enterprise is interactive. Type a question after startup.");
    return 1;
}

string[] flags = ["--no-key", "--no-token", "--open"];
var normalized = args.Select(a => flags.Contains(a) ? $"{a}=true" : a).ToArray();
var config = AgentConfig.Build(normalized);
using var tracing = Telemetry.Start("opsbot-enterprise", config, sampleAll: true);
McpClient? client = null;
HttpClient? http = null;
IChatClient? chat = null;
AIAgent? agent = null;
AgentSession? session = null;
var meter = new UsageMeter();

async Task ConnectAsync()
{
    agent = null;
    session = null;
    if (client is not null) await client.DisposeAsync();
    client = null;
    http?.Dispose();
    chat?.Dispose();
    chat = null;
    config = AgentConfig.Build(normalized);
    GatewayHandler? gateway = null;
    try
    {
        var open = bool.TryParse(config["open"], out var useOpen) && useOpen;
        var endpoint = config["discover"] is { Length: > 0 } apiId
            ? await ApiCenterResolver.ResolveAsync(config, apiId)
            : open ? config["Mcp:OpenEndpoint"] : config["Mcp:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) throw new InvalidOperationException("Mcp:Endpoint is required.");
        Ui.Info($"endpoint: {endpoint}");
        var key = open ? config["Mcp:OpenSubscriptionKey"] : config["Mcp:ApimSubscriptionKey"];
        if (bool.TryParse(config["no-key"], out var noKey) && noKey) key = null;
        Ui.Info($"subscription key: {(string.IsNullOrWhiteSpace(key) ? "missing" : "present")}");

        var noToken = bool.TryParse(config["no-token"], out var omitToken) && omitToken;
        Azure.Core.TokenCredential? credential = null;
        var scope = config["Mcp:Entra:Scope"] ?? "api://opsdesk-mcp/.default";
        if (!noToken)
        {
            var identityName = config["identity"] ?? "default";
            if (identityName is not ("default" or "norole"))
                throw new InvalidOperationException("--identity must be default or norole.");
            var identity = config.GetSection($"Mcp:Entra:Identities:{identityName}");
            var tenant = config["Mcp:Entra:TenantId"];
            if (string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(identity["ClientId"]) ||
                string.IsNullOrWhiteSpace(identity["ClientSecret"]))
                throw new InvalidOperationException($"Configure the {identityName} Entra identity in appsettings.Development.json.");
            credential = new ClientSecretCredential(tenant, identity["ClientId"], identity["ClientSecret"]);
            var token = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext([scope]), CancellationToken.None);
            Ui.Info($"identity: {identity["ClientId"]}");
            Ui.Info($"roles: {TokenClaims.Roles(token.Token)}; expires: {token.ExpiresOn:O}");
        }
        else Ui.Info("identity: no token; roles: none");

        gateway = new GatewayHandler(credential, scope) { InnerHandler = new HttpClientHandler() };
        http = new HttpClient(gateway) { Timeout = TimeSpan.FromSeconds(60) };
        var headers = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(key)) headers["Ocp-Apim-Subscription-Key"] = key;
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            AdditionalHeaders = headers
        }, http, ownsHttpClient: false);
        client = await McpClient.CreateAsync(transport);
        var tools = await client.ListToolsAsync();
        Ui.Info($"discovered {tools.Count} tools from '{client.ServerInfo?.Name ?? "MCP server"}'");
        Ui.Banner("OpsBot · ENTERPRISE", tools.Cast<AITool>().ToList(), showPermissionMessage: false);
        chat = AgentConfig.CreateChatClient(config, meter, maxOutputTokens: 1200);
        agent = chat.AsAIAgent(name: OpsBotInstructions.AgentName,
            instructions: OpsBotInstructions.Text + """

            Only use the tools currently exposed by this server. If no write tool is available,
            say this connection is read-only and do not claim to create or notify an incident.
            For an all-systems overview, retrieve fresh status, owner, runbook, and incidents;
            do not reuse operational facts from an earlier question.
            If a tool reports Rate limit exceeded, stop requesting tools immediately.
            Give only the facts already retrieved and explicitly explain the gateway limit.
            Do not retry or wait out that limit.
            """, tools: [.. tools]);
        session = await agent.CreateSessionAsync();
    }
    catch (Exception ex) when (ex is HttpRequestException or AuthenticationFailedException or
        McpException or InvalidOperationException or ArgumentException or JsonException or OperationCanceledException)
    {
        Ui.Error(gateway?.LastFailure ?? ex.Message);
        Ui.Info("Type reconnect to retry after updating local settings, or exit.");
    }
}

try
{
    await ConnectAsync();
    while (true)
    {
        Ui.PromptMarker();
        var input = Console.ReadLine();
        if (input is null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
        if (string.IsNullOrWhiteSpace(input)) continue;
        if (input.Trim().Equals("reconnect", StringComparison.OrdinalIgnoreCase))
        {
            await ConnectAsync();
            continue;
        }
        if (input.Trim().Equals("stats", StringComparison.OrdinalIgnoreCase))
        {
            meter.PrintSession("enterprise");
            continue;
        }
        if (input.Trim().Equals("reset-stats", StringComparison.OrdinalIgnoreCase))
        {
            meter.ResetSession();
            Ui.Info("session statistics reset");
            continue;
        }
        if (agent is null || session is null)
        {
            Ui.Error("Not connected. Type reconnect or exit.");
            continue;
        }
        await ChatLoop.AskOnceAsync(agent, session, meter, input, "enterprise", printTraceId: true);
    }
}
finally
{
    if (client is not null) await client.DisposeAsync();
    http?.Dispose();
    chat?.Dispose();
}
return 0;
