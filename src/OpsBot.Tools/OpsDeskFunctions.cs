using System.ComponentModel;
using System.Net.Http.Json;

namespace OpsBot.Tools;

/// <summary>
/// Demo 1b – the "obvious" fix: hand-write a function per API call and hand them to the model.
///
/// It works. Now look at what it costs:
///   • descriptions written once, for this host, by this developer – no other host can discover them;
///   • the API's base URL (and any key) live in THIS app's config – every other host would copy them;
///   • one function per endpoint: the model has to do the joins itself (status + owner + runbook + incidents);
///   • errors come back as raw status codes and JSON for the model to interpret.
/// This file is what MCP replaces.
/// </summary>
public sealed class OpsDeskFunctions(HttpClient http)
{
    [Description("Returns the operational health status (Green/Yellow/Red) of one or all systems from the OpsDesk API.")]
    public Task<string> GetSystemStatus([Description("The system ID to look up. Omit to retrieve all systems.")] string? systemId = null) =>
        Get(systemId is null ? "/SystemStatus" : $"/SystemStatus/{Uri.EscapeDataString(systemId)}");

    [Description("Returns the owning team and contact details (team name, email, Slack channel) for one or all systems from the OpsDesk API.")]
    public Task<string> GetSystemOwner([Description("The system ID to look up. Omit to retrieve all systems.")] string? systemId = null) =>
        Get(systemId is null ? "/SystemOwner" : $"/SystemOwner/{Uri.EscapeDataString(systemId)}");

    [Description("Returns the approved runbook (title, URL, last review date) for one or all systems from the OpsDesk API.")]
    public Task<string> GetRunbook([Description("The system ID to look up. Omit to retrieve all systems.")] string? systemId = null) =>
        Get(systemId is null ? "/Runbook" : $"/Runbook/{Uri.EscapeDataString(systemId)}");

    [Description("Returns all incidents, most recent first.")]
    public Task<string> GetIncidents() => Get("/Incident");

    [Description("Creates an incident for a system.")]
    public async Task<string> CreateIncident(
        [Description("System ID")] string systemId,
        [Description("Title")] string title,
        [Description("Description")] string description,
        [Description("low, medium, high or critical")] string severity,
        [Description("Reporter")] string reportedBy)
    {
        var response = await http.PostAsJsonAsync($"/Incident/{Uri.EscapeDataString(systemId)}",
            new { title, description, severity, reportedBy });
        return $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}";
    }

    private async Task<string> Get(string url)
    {
        var response = await http.GetAsync(url);
        return $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}";
    }
}
