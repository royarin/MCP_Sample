// ─────────────────────────────────────────────────────────────────────────────
//  OpsBot.NoTools · Demo 1a – an agent with no tools
//
//  Ask: "What's wrong with inventory, who do I call, is there a runbook, and is anything open on it?"
//  The grounding rules make it admit it cannot verify anything. "The model is fine. It just has no hands."
//
//  dotnet run -- --ask "<question>"      one-shot (used by scripts/demo1.ps1)
// ─────────────────────────────────────────────────────────────────────────────
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpsBot.Shared;

var config = AgentConfig.Build(args);
using var tracing = Telemetry.Start("opsbot-notools", config);

var meter = new UsageMeter();
var chatClient = AgentConfig.CreateChatClient(config, meter);

var agent = chatClient.AsAIAgent(instructions: OpsBotInstructions.Text, name: OpsBotInstructions.AgentName);

Ui.Banner("OpsBot · NO TOOLS", tools: null);
await ChatLoop.RunAsync(agent, config, meter, runLabel: "no tools");
