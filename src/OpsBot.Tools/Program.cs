// ─────────────────────────────────────────────────────────────────────────────
//  OpsBot.Tools · Demo 1b – the same agent with hand-wired functions
//
//  Same question as OpsBot.NoTools – now answered correctly, because five functions call the
//  existing OpsDesk API over HttpClient. Tool calls are traced in the console; CreateIncident asks for
//  permission first. Then open OpsDeskFunctions.cs and count what is bespoke to this one host.
//
//  Config: OpsDesk:BaseUrl (deployed API), AzureOpenAI:* via user-secrets.   --ask "<question>" for one shot.
// ─────────────────────────────────────────────────────────────────────────────
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpsBot.Shared;
using OpsBot.Tools;

var config = AgentConfig.Build(args);
using var tracing = Telemetry.Start("opsbot-tools", config);

// The API client – base URL (and any key) are now *this agent's* problem.
var opsBaseUrl = config["OpsDesk:BaseUrl"] ?? "http://localhost:5047";
var http = new HttpClient { BaseAddress = new Uri(opsBaseUrl) };
var functions = new OpsDeskFunctions(http);

// Hand-wired: one AIFunction per method, bound to this process.
IList<AITool> tools = PermissionGate.Wrap(
[
    AIFunctionFactory.Create(functions.GetSystemStatus),
    AIFunctionFactory.Create(functions.GetSystemOwner),
    AIFunctionFactory.Create(functions.GetRunbook),
    AIFunctionFactory.Create(functions.GetIncidents),
    AIFunctionFactory.Create(functions.CreateIncident),
], AgentConfig.PermissionMode(config));

var meter = new UsageMeter();
var chatClient = AgentConfig.CreateChatClient(config, meter);
var agent = chatClient.AsAIAgent(instructions: OpsBotInstructions.Text, name: OpsBotInstructions.AgentName, tools: [.. tools]);

Ui.Banner($"OpsBot · HAND-WIRED FUNCTIONS → {opsBaseUrl}", tools);
await ChatLoop.RunAsync(agent, config, meter, runLabel: "hand-wired");
