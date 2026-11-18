// ─────────────────────────────────────────────────────────────────────────────
//  OpsDesk.Mcp.Tasks · Demo 3 (task-shaped side)
//
//  Same OpsDesk API, same SDK, same transport as OpsDesk.Mcp – different tool design:
//    • tools shaped around what an on-call engineer wants done, not around endpoints
//    • descriptions say WHEN to use a tool, not just what it does
//    • the server does the joins (status + owner + runbook + incidents) so the model doesn't have to
//    • errors are sentences the model can relay ("did you mean inventory?"), never raw JSON
//  Five tools instead of eleven. Demo 3 measures the difference in calls, turns and tokens.
// ─────────────────────────────────────────────────────────────────────────────
using Azure.Monitor.OpenTelemetry.AspNetCore;
using ModelContextProtocol.Protocol;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpsDesk.Mcp.Tasks.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("OpsDesk", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
});
builder.Services.AddSingleton<OpsDeskClient>();
builder.Services.AddCors();

builder.Services
    .AddMcpServer(o =>
    {
        o.ServerInfo = new Implementation { Name = "opsdesk-tasks", Version = "1.0.0" };
        o.ServerInstructions =
            "Task-shaped tools for OpsDesk on-call work. When a system is named, start with get_system_briefing. " +
            "For a standup or handover use get_oncall_handover. raise_incident and update_system_status change data – confirm with the user first.";
    })
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<TaskShapedTools>();

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor()
        .WithTracing(t => t.AddSource("Experimental.ModelContextProtocol"));

var app = builder.Build();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.MapMcp("/mcp");
app.MapGet("/health", () => Results.Ok(new { status = "ok", server = "opsdesk-tasks", style = "task-shaped" }));

app.Run();
