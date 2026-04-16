using System.ComponentModel;
using ModelContextProtocol.Server;
using OpsDesk.Mcp.Models;

namespace OpsDesk.Mcp.Tools;

[McpServerToolType]
public sealed class IncidentTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("OpsDesk");

    [McpServerTool, Description("Returns all incidents, ordered most recent first.")]
    public async Task<string> GetAllIncidents()
    {
        var response = await Client.GetAsync("incident");
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Creates a new incident for a system. Generates a sequential incident number and captures the current system status snapshot.")]
    public async Task<string> CreateIncident(
        [Description("The system identifier the incident is raised against.")] string systemId,
        [Description("Short title describing the incident.")] string title,
        [Description("Detailed description of the incident.")] string description,
        [Description("Severity level of the incident: low, medium, high, or critical.")] string severity,
        [Description("Name or identifier of the person reporting the incident.")] string reportedBy)
    {
        var payload = new CreateIncidentRequest(title, description, severity, reportedBy);
        var response = await Client.PostAsJsonAsync($"incident/{Uri.EscapeDataString(systemId)}", payload);
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Sends an email notification to the system owner for an existing incident. Returns the composed notification message (no real email is sent).")]
    public async Task<string> NotifyIncident(
        [Description("The incident number (e.g. INC-00001) to notify the owner about.")] string incidentNumber)
    {
        var response = await Client.PostAsync($"incident/{Uri.EscapeDataString(incidentNumber)}/notify", null);
        return await response.Content.ReadAsStringAsync();
    }
}
