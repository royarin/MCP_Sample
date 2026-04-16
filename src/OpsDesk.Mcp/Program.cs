using OpsDesk.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("OpsDesk", client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<SystemStatusTools>()
    .WithTools<SystemOwnerTools>()
    .WithTools<RunbookTools>()
    .WithTools<IncidentTools>();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
