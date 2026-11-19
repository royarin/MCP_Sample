// Demo 3: discover either MCP catalogue at runtime; --ask and --compare are for rehearsal.
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpsBot.Mcp;
using OpsBot.Shared;

try
{
    var config = AgentConfig.Build(args);
    using var tracing = Telemetry.Start("opsbot-mcp", config);
    var label = config["label"];
    if (label is { Length: > 32 } || label?.Any(char.IsControl) == true)
        throw new ArgumentException("--label must be at most 32 characters and contain no control characters.");
    var accent = config["accent"]?.ToLowerInvariant() switch
    {
        null or "" => (ConsoleColor?)null,
        "yellow" => ConsoleColor.Yellow,
        "cyan" => ConsoleColor.Cyan,
        "magenta" => ConsoleColor.Magenta,
        "green" => ConsoleColor.Green,
        _ => throw new ArgumentException("--accent must be yellow, cyan, magenta or green.")
    };
    var compareIdx = Array.IndexOf(args, "--compare");
    if (compareIdx >= 0)
    {
        var endpoints = args.Skip(compareIdx + 1).TakeWhile(a => !a.StartsWith("--")).ToArray();
        if (endpoints.Length != 2) throw new ArgumentException("--compare needs exactly two endpoints");
        var question = config["ask"] ?? "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?";
        var snapshots = new List<UsageMeter.Snapshot>();
        var catalogues = new List<object>();
        foreach (var (compareEndpoint, index) in endpoints.Select((ep, i) => (ep, i)))
        {
            var meter = new UsageMeter();
            var chat = AgentConfig.CreateChatClient(config, meter, maxOutputTokens: 1200);
            var conn = await McpConnection.ConnectAsync(compareEndpoint, config);
            await using var client = conn.Client;
            var tools = PermissionGate.Wrap(conn.Tools, AgentConfig.PermissionMode(config));
            var agent = chat.AsAIAgent(instructions: OpsBotInstructions.McpText, name: OpsBotInstructions.AgentName, tools: [.. tools]);
            var runLabel = index == 0 ? "endpoint-shaped" : "task-shaped";
            Ui.Banner($"OpsBot · MCP → {conn.ServerName} · {runLabel.ToUpperInvariant()} ({conn.ToolCount} tools · {conn.CatalogTokens:N0} catalogue tokens)", tools, accent);
            Ui.PromptMarker(runLabel, accent); Console.WriteLine(question);
            var session = await agent.CreateSessionAsync();
            var snapshot = await ChatLoop.AskOnceAsync(agent, session, meter, question, runLabel);
            snapshots.Add(snapshot);
            catalogues.Add(new { conn.ToolCount, conn.CatalogTokens });
            if (!snapshot.Success) return 1;
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine("══ side by side ".PadRight(78, '═')); Console.ResetColor();
        UsageMeter.PrintScorecard([.. snapshots]);
        if (string.Equals(config["measurement"], "true", StringComparison.OrdinalIgnoreCase))
        {
            if (snapshots.Any(s => s.ModelTurns <= 0 || s.ToolCalls <= 0 || s.InputTokens <= 0 || s.OutputTokens <= 0 || s.Elapsed <= TimeSpan.Zero))
            {
                Ui.Error("Measurement incomplete: model turns, tool calls, token usage and elapsed time are required.");
                return 1;
            }
            Console.WriteLine("MEASUREMENT_JSON:" + JsonSerializer.Serialize(new
            {
                Deployment = AgentConfig.Deployment(config), Catalogues = catalogues, Runs = snapshots
            }));
        }
        return 0;
    }

    var endpoint = config["Mcp:Endpoint"] ?? "http://localhost:5001/mcp";
    var usage = new UsageMeter();
    var chatClient = AgentConfig.CreateChatClient(config, usage, maxOutputTokens: 1200);
    var connection = await McpConnection.ConnectAsync(endpoint, config);
    await using var mcpClient = connection.Client;
    var availableTools = PermissionGate.Wrap(connection.Tools, AgentConfig.PermissionMode(config));
    var assistant = chatClient.AsAIAgent(instructions: OpsBotInstructions.McpText, name: OpsBotInstructions.AgentName, tools: [.. availableTools]);
    var runName = string.IsNullOrWhiteSpace(label) ? connection.ServerName : label;
    Ui.Banner($"OpsBot · MCP → {connection.ServerName} · {runName.ToUpperInvariant()} ({connection.ToolCount} tools · {connection.CatalogTokens:N0} catalogue tokens)", availableTools, accent);
    if (config["ask"] is { Length: > 0 } ask)
    {
        var session = await assistant.CreateSessionAsync();
        return (await ChatLoop.AskOnceAsync(assistant, session, usage, ask, runName)).Success ? 0 : 1;
    }
    await ChatLoop.RunAsync(assistant, config, usage, runLabel: runName, promptLabel: label, accent: accent);
    return 0;
}
catch (Exception ex)
{
    Ui.Error(ex.Message);
    return 1;
}
