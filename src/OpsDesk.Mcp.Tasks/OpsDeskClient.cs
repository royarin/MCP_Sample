using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;
using OpsDesk.Mcp.Tasks.Models;

namespace OpsDesk.Mcp.Tasks;

/// <summary>Typed access to the existing OpsDesk API plus the helpers the task-shaped tools share.</summary>
public sealed class OpsDeskClient(IHttpClientFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private HttpClient Http => factory.CreateClient("OpsDesk");

    public async Task<List<SystemStatusDto>> AllStatusesAsync(CancellationToken ct) =>
        await Http.GetFromJsonAsync<List<SystemStatusDto>>("SystemStatus", Json, ct) ?? [];
    public async Task<List<SystemOwnerDto>> AllOwnersAsync(CancellationToken ct) =>
        await Http.GetFromJsonAsync<List<SystemOwnerDto>>("SystemOwner", Json, ct) ?? [];
    public async Task<List<IncidentDto>> AllIncidentsAsync(CancellationToken ct) =>
        await Http.GetFromJsonAsync<List<IncidentDto>>("Incident", Json, ct) ?? [];

    public Task<SystemStatusDto?> StatusAsync(string id, CancellationToken ct) => GetOrNull<SystemStatusDto>($"SystemStatus/{Uri.EscapeDataString(id)}", ct);
    public Task<SystemOwnerDto?> OwnerAsync(string id, CancellationToken ct) => GetOrNull<SystemOwnerDto>($"SystemOwner/{Uri.EscapeDataString(id)}", ct);
    public Task<RunbookDto?> RunbookAsync(string id, CancellationToken ct) => GetOrNull<RunbookDto>($"Runbook/{Uri.EscapeDataString(id)}", ct);

    public async Task<IncidentDto> CreateIncidentAsync(string systemId, CreateIncidentRequest req, CancellationToken ct)
    {
        using var r = await Http.PostAsJsonAsync($"Incident/{Uri.EscapeDataString(systemId)}", req, Json, ct);
        if (!r.IsSuccessStatusCode) throw new McpException($"The incident system refused the request ({(int)r.StatusCode}). Ask an operator to raise it manually.");
        return (await r.Content.ReadFromJsonAsync<IncidentDto>(Json, ct))!;
    }

    public async Task<NotificationDto> NotifyAsync(string incidentNumber, CancellationToken ct)
    {
        using var r = await Http.PostAsync($"Incident/{Uri.EscapeDataString(incidentNumber)}/notify", content: null, ct);
        if (!r.IsSuccessStatusCode) throw new McpException($"Could not notify the owner of {incidentNumber} ({(int)r.StatusCode}).");
        return (await r.Content.ReadFromJsonAsync<NotificationDto>(Json, ct))!;
    }

    public async Task<SystemStatusDto> UpdateStatusAsync(string systemId, UpdateStatusRequest req, CancellationToken ct)
    {
        using var r = await Http.PostAsJsonAsync($"SystemStatus/{Uri.EscapeDataString(systemId)}", req, Json, ct);
        if (!r.IsSuccessStatusCode) throw new McpException($"Status update for '{systemId}' was refused ({(int)r.StatusCode}). Allowed values: green, yellow, red.");
        return (await r.Content.ReadFromJsonAsync<SystemStatusDto>(Json, ct))!;
    }

    /// <summary>Resolves a system id; on a miss, throws an McpException with a "did you mean" suggestion.</summary>
    public async Task<string> ResolveSystemIdAsync(string systemId, CancellationToken ct)
    {
        var known = (await AllOwnersAsync(ct)).Select(o => o.SystemId).ToList();
        var exact = known.FirstOrDefault(k => k.Equals(systemId?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;
        var best = known.Select(k => (k, d: Levenshtein(k, systemId ?? ""))).OrderBy(x => x.d).FirstOrDefault();
        var hint = best.k is not null && best.d <= 3
            ? $" Did you mean '{best.k}'? Ask the user to confirm the corrected system id before retrying."
            : $" Known systems: {string.Join(", ", known)}.";
        throw new McpException($"Unknown system '{systemId}'.{hint}");
    }

    private async Task<T?> GetOrNull<T>(string url, CancellationToken ct) where T : class
    {
        using var r = await Http.GetAsync(url, ct);
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<T>(Json, ct);
    }

    private static int Levenshtein(string a, string b)
    {
        a = a.ToLowerInvariant(); b = b.ToLowerInvariant();
        var d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }
}
