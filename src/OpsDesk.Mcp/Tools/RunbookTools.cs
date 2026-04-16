using System.ComponentModel;
using ModelContextProtocol.Server;

namespace OpsDesk.Mcp.Tools;

[McpServerToolType]
public sealed class RunbookTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("OpsDesk");

    [McpServerTool, Description("Returns all approved runbooks.")]
    public async Task<string> GetAllRunbooks()
    {
        var response = await Client.GetAsync("runbook");
        return await response.Content.ReadAsStringAsync();
    }

    [McpServerTool, Description("Returns the approved runbook for a specific system. Returns the runbook title, URL, and last review date.")]
    public async Task<string> GetRunbook(
        [Description("The system identifier to look up the runbook for.")] string systemId)
    {
        var response = await Client.GetAsync($"runbook/{Uri.EscapeDataString(systemId)}");
        return await response.Content.ReadAsStringAsync();
    }
}
