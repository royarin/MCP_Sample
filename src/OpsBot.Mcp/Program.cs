// ─────────────────────────────────────────────────────────────────────────────
//  OpsBot.Mcp · the Demo 1 agent with the hand-wired file deleted – tools come from an MCP server
//
//  Compare with OpsBot.Tools: no OpsDeskFunctions.cs, no API base URL, no knowledge of OpsDesk at all.
//  Connect, tools/list, hand whatever comes back to the model. Point it at any server:
//
//    dotnet run -- --Mcp:Endpoint https://<opsdesk-mcp>/mcp        --ask "<question>"
//    dotnet run -- --compare https://<opsdesk-mcp>/mcp https://<opsdesk-tasks>/mcp --ask "<question>"   (Demo 3)
//    dotnet run -- --Mcp:Endpoint https://<apim>/opsdesk-secure/mcp --Mcp:Auth entra                     (Demo 4)
//
//  Flags: --permissions ask|auto (default ask → y/n/always prompt before write tools)
//  Config: AzureOpenAI:*, Mcp:Endpoint, Mcp:ApimSubscriptionKey, Mcp:Auth, Mcp:Entra:{TenantId,ClientId,ClientSecret,Scope}
// ─────────────────────────────────────────────────────────────────────────────
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpsBot.Mcp;
using OpsBot.Shared;

var config = AgentConfig.Build(args);
using var tracing = Telemetry.Start("opsbot-mcp", config);

var compareIdx = Array.IndexOf(args, "--compare");
if (compareIdx >= 0)
{
    // ── Demo 3: same question against two servers, side-by-side scorecard ──
    var endpoints = args.Skip(compareIdx + 1).TakeWhile(a => !a.StartsWith("--")).ToArray();
    if (endpoints.Length != 2) { Ui.Error("--compare needs exactly two endpoints"); return 1; }
    var question = config["ask"] ?? "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?";

    var snapshots = new List<UsageMeter.Snapshot>();
    foreach (var ep in endpoints)
    {
        var meter = new UsageMeter();
        var chat = AgentConfig.CreateChatClient(config, meter);
        var conn = await McpConnection.ConnectAsync(ep, config);
        await using var _ = conn.Client;
        var tools = PermissionGate.Wrap(conn.Tools, AgentConfig.PermissionMode(config));
        var agent = chat.AsAIAgent(instructions: OpsBotInstructions.Text, name: OpsBotInstructions.AgentName, tools: [.. tools]);
        Ui.Banner($"OpsBot · MCP → {conn.ServerName}  ({conn.ToolCount} tools, {conn.CatalogTokens:N0} catalogue tokens)", tools);
        Ui.PromptMarker(); Console.WriteLine(question);
        var session = await agent.CreateSessionAsync();
        snapshots.Add(await ChatLoop.AskOnceAsync(agent, session, meter, question, conn.ServerName));
    }
    Console.ForegroundColor = ConsoleColor.DarkYellow; Console.WriteLine("══ side by side ".PadRight(78, '═')); Console.ResetColor();
    UsageMeter.PrintScorecard([.. snapshots]);
    return 0;
}

// ── Demo 2 / Demo 4: one server, interactive (or --ask) ──
{
    var endpoint = config["Mcp:Endpoint"] ?? "http://localhost:5001/mcp";
    var meter = new UsageMeter();
    var chat = AgentConfig.CreateChatClient(config, meter);
    var conn = await McpConnection.ConnectAsync(endpoint, config);
    await using var _ = conn.Client;
    var tools = PermissionGate.Wrap(conn.Tools, AgentConfig.PermissionMode(config));
    var agent = chat.AsAIAgent(instructions: OpsBotInstructions.Text, name: OpsBotInstructions.AgentName, tools: [.. tools]);
    Ui.Banner($"OpsBot · MCP → {conn.ServerName}", tools);
    await ChatLoop.RunAsync(agent, config, meter, runLabel: conn.ServerName);
    return 0;
}
