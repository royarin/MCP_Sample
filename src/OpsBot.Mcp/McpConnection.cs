using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using OpsBot.Shared;

namespace OpsBot.Mcp;

/// <summary>
/// Connects to one MCP endpoint and returns its tools as AIFunctions. Handles the three ways a server is
/// reached in the demos: plain (Container Apps), APIM subscription key (opsdesk-apim), Entra bearer token
/// (opsdesk-secure). Prints what it discovered and what the catalogue costs in tokens.
/// </summary>
public static class McpConnection
{
    public sealed record Connected(McpClient Client, IList<AITool> Tools, string ServerName, int ToolCount, int CatalogTokens);

    public static async Task<Connected> ConnectAsync(string endpoint, IConfiguration config, CancellationToken ct = default)
    {
        var headers = new Dictionary<string, string>();

        if (config["Mcp:ApimSubscriptionKey"] is { Length: > 0 } key)
            headers["Ocp-Apim-Subscription-Key"] = key;

        if (string.Equals(config["Mcp:Auth"], "entra", StringComparison.OrdinalIgnoreCase))
            headers["Authorization"] = "Bearer " + await AcquireTokenAsync(config, ct);

        // ModelContextProtocol 1.x: HttpClientTransport speaks Streamable HTTP (falls back to legacy SSE).
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(endpoint),
            Name = "opsdesk",
            AdditionalHeaders = headers,
        });

        var client = await McpClient.CreateAsync(transport, cancellationToken: ct);

        // ── Runtime discovery – the "Dynamic discovery" slide happening live ──
        var mcpTools = await client.ListToolsAsync(cancellationToken: ct);
        IList<AITool> tools = [.. mcpTools];                              // McpClientTool : AIFunction

        var (count, tokens) = ToolCatalogMeter.Measure(tools, AgentConfig.Deployment(config));
        var name = client.ServerInfo?.Name ?? new Uri(endpoint).Host;
        Ui.Info($"connected to {endpoint}");
        Ui.Info($"discovered {count} tool(s) from '{name}' · {tokens:N0} tokens of tool definitions sent on every model call");
        return new Connected(client, tools, name, count, tokens);
    }

    private static async Task<string> AcquireTokenAsync(IConfiguration config, CancellationToken ct)
    {
        var e = config.GetSection("Mcp:Entra");
        var scope = e["Scope"] ?? "api://opsdesk-mcp/.default";
        TokenCredential credential = e["ClientSecret"] is { Length: > 0 } secret
            ? new ClientSecretCredential(e["TenantId"], e["ClientId"], secret)   // service agent (Demo 4)
            : new DefaultAzureCredential();                                      // az login – a user's delegated token
        var token = await credential.GetTokenAsync(new TokenRequestContext([scope]), ct);

        var claims = DecodeJwtPayload(token.Token);
        var roles = claims.TryGetProperty("roles", out var r) ? string.Join(", ", r.EnumerateArray().Select(x => x.GetString())) : "-";
        var who = claims.TryGetProperty("azp", out var azp) ? azp.GetString()
                : claims.TryGetProperty("appid", out var appid) ? appid.GetString()
                : claims.TryGetProperty("preferred_username", out var upn) ? upn.GetString() : "?";
        Ui.Info($"identity: {who}  roles: [{roles}]  expires: {token.ExpiresOn:HH:mm}Z");
        return token.Token;
    }

    /// <summary>Display only – validation happens at the gateway.</summary>
    private static JsonElement DecodeJwtPayload(string jwt)
    {
        var payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        return JsonDocument.Parse(Convert.FromBase64String(payload)).RootElement;
    }
}
