using System.ComponentModel;
using ModelContextProtocol.Server;

namespace OpsDesk.Mcp.Tools;

[McpServerToolType]
public sealed class SystemOwnerTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("OpsDesk");

    [McpServerTool, Description("Returns the owning team and contact details for all systems.")]
    public async Task<string> GetAllSystemOwners()
    {
        var response = await Client.GetAsync("systemowner");
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Returns the owning team and contact details for a specific system. Returns team name, contact email, and Slack channel.")]
    public async Task<string> GetSystemOwner(
        [Description("The system identifier to look up.")] string systemId)
    {
        var response = await Client.GetAsync($"systemowner/{Uri.EscapeDataString(systemId)}");
        return await response.Content.ReadAsStringAsync();
    }
}
