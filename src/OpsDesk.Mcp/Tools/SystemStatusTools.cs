using System.ComponentModel;
using ModelContextProtocol.Server;
using OpsDesk.Mcp.Models;

namespace OpsDesk.Mcp.Tools;

[McpServerToolType]
public sealed class SystemStatusTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("OpsDesk");

    [McpServerTool, Description("Returns the current operational status of all systems. Optionally filter by status: green, yellow, or red.")]
    public async Task<string> GetAllSystemStatuses(
        [Description("Optional status filter: green, yellow, or red.")] string? status = null)
    {
        var url = string.IsNullOrWhiteSpace(status)
            ? "systemstatus"
            : $"systemstatus?status={Uri.EscapeDataString(status)}";

        var response = await Client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Returns the current operational status of a specific system.")]
    public async Task<string> GetSystemStatus(
        [Description("The system identifier to look up.")] string systemId)
    {
        var response = await Client.GetAsync($"systemstatus/{Uri.EscapeDataString(systemId)}");
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Updates the operational status of a system. Accepted status values: green, yellow, red.")]
    public async Task<string> UpdateSystemStatus(
        [Description("The system identifier to update.")] string systemId,
        [Description("New status value: green, yellow, or red.")] string status,
        [Description("Human-readable message describing the current state.")] string message)
    {
        var payload = new UpdateStatusRequest(status, message);
        var response = await Client.PostAsJsonAsync($"systemstatus/{Uri.EscapeDataString(systemId)}", payload);
        return await response.Content.ReadAsStringAsync();
    }
}
