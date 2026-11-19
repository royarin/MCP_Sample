using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using OpsBot.Shared;

namespace OpsBot.Mcp;

/// <summary>
/// Connects to an MCP endpoint with an optional APIM subscription key and discovers its tools.
/// </summary>
public static class McpConnection
{
    public sealed record Connected(McpClient Client, IList<AITool> Tools, string ServerName, int ToolCount, int CatalogTokens);

    public static async Task<Connected> ConnectAsync(string endpoint, IConfiguration config, CancellationToken ct = default)
    {
        var headers = new Dictionary<string, string>();
        var endpointUri = new Uri(endpoint);

        if (config["Mcp:ApimSubscriptionKey"] is { Length: > 0 } key)
            headers["Ocp-Apim-Subscription-Key"] = key;
        else if (endpointUri.Host.EndsWith(".azure-api.net", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mcp:ApimSubscriptionKey is required when Mcp:Endpoint points to API Management.");

        // ModelContextProtocol 1.x: HttpClientTransport speaks Streamable HTTP (falls back to legacy SSE).
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = endpointUri,
            Name = "opsdesk",
            AdditionalHeaders = headers,
        });

        var client = await McpClient.CreateAsync(transport, cancellationToken: ct);

        // ── Runtime discovery – the "Dynamic discovery" slide happening live ──
        var mcpTools = await client.ListToolsAsync(cancellationToken: ct);
        IList<AITool> tools = [.. mcpTools];                              // McpClientTool : AIFunction

        var (count, tokens) = ToolCatalogMeter.Measure(tools, AgentConfig.Deployment(config));
        var name = client.ServerInfo?.Name ?? endpointUri.Host;
        var prefix = $"discovered {count} tools from '";
        var suffix = $"' · {tokens:N0} tokens of tool definitions sent on every model call";
        var available = Math.Max(0, 97 - prefix.Length - suffix.Length);
        var shortName = name.Length > available && available >= 3 ? name[..(available - 3)] + "..." : name;
        var line = prefix + shortName + suffix;
        if (line.Length <= 97) Ui.Info(line);
        else
        {
            Ui.Info($"discovered {count} tools from '{name}' · {tokens:N0} tokens of tool definitions");
            Ui.Info("sent on every model call");
        }
        return new Connected(client, tools, name, count, tokens);
    }
}
