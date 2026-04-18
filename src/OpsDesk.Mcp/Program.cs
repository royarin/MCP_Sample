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

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.MapMcp("/mcp");

app.Run();
