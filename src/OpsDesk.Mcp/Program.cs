// ─────────────────────────────────────────────────────────────────────────────
//  OpsDesk.Mcp · Demo 2a / Demo 3 (endpoint-shaped side)
//
//  A thin MCP façade over the existing OpsDesk API: one tool per API operation, API summaries as
//  descriptions, raw JSON results. Correct, discoverable – and the baseline Demo 3 measures against.
//  The tools are deliberately left as they were; the task-shaped alternative lives in OpsDesk.Mcp.Tasks.
// ─────────────────────────────────────────────────────────────────────────────
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpsDesk.Mcp.Prompts;
using OpsDesk.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("OpsDesk", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddCors();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<SystemStatusTools>()
    .WithTools<SystemOwnerTools>()
    .WithTools<RunbookTools>()
    .WithTools<IncidentTools>()
    .WithPrompts<IncidentTriagePrompt>();

// Traces → Application Insights when APPLICATIONINSIGHTS_CONNECTION_STRING is set (Demo 4 observability).
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor()
        .WithTracing(t => t.AddSource("Experimental.ModelContextProtocol"));   // MCP SDK spans (tools/call etc.)

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.MapMcp("/mcp");
app.MapGet("/health", () => Results.Ok(new { status = "ok", server = "opsdesk-mcp", style = "endpoint-shaped" }));

app.Run();
