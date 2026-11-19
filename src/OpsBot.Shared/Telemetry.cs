using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpsBot.Shared;

/// <summary>
/// One root span per question ("agent.question") plus HttpClient and MCP SDK spans, exported to
/// Application Insights when APPLICATIONINSIGHTS_CONNECTION_STRING is set. Demo 4: the agent span sits on
/// top of the APIM → MCP server → API trace.
/// </summary>
public static class Telemetry
{
    public static readonly ActivitySource Source = new("OpsBot.Agent");

    public static TracerProvider? Start(string serviceName, IConfiguration c, bool sampleAll = false)
    {
        var cs = c["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(cs)) return null;
        var builder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddSource(Source.Name)
            .AddSource("Experimental.ModelContextProtocol")      // MCP C# SDK client/server spans
            .AddSource("Experimental.Microsoft.Extensions.AI")  // chat client spans (when enabled)
            .AddHttpClientInstrumentation()
            .AddAzureMonitorTraceExporter(o => o.ConnectionString = cs);
        if (sampleAll) builder.SetSampler(new AlwaysOnSampler());
        return builder.Build();
    }
}
