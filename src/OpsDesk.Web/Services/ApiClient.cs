using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpsDesk.Web.Models;

namespace OpsDesk.Web.Services;

public class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    // ── System Status ────────────────────────────────────────────────────────
    public Task<List<SystemStatus>?> GetAllStatusesAsync(SystemHealthStatus? filter = null)
    {
        var url = filter is null
            ? "systemstatus"
            : $"systemstatus?status={filter.Value.ToString().ToLower()}";
        return http.GetFromJsonAsync<List<SystemStatus>>(url, JsonOptions);
    }

    public Task<SystemStatus?> GetStatusAsync(string systemId) =>
        http.GetFromJsonAsync<SystemStatus>($"systemstatus/{systemId}", JsonOptions);

    public async Task<SystemStatus?> UpdateStatusAsync(string systemId, UpdateStatusRequest request)
    {
        var response = await http.PostAsJsonAsync($"systemstatus/{systemId}", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SystemStatus>(JsonOptions);
    }

    // ── System Owner ─────────────────────────────────────────────────────────
    public Task<List<SystemOwner>?> GetAllOwnersAsync() =>
        http.GetFromJsonAsync<List<SystemOwner>>("systemowner", JsonOptions);

    public Task<SystemOwner?> GetOwnerAsync(string systemId) =>
        http.GetFromJsonAsync<SystemOwner>($"systemowner/{systemId}", JsonOptions);

    // ── Runbooks ─────────────────────────────────────────────────────────────
    public Task<List<SystemRunbook>?> GetAllRunbooksAsync() =>
        http.GetFromJsonAsync<List<SystemRunbook>>("runbook", JsonOptions);

    public Task<SystemRunbook?> GetRunbookAsync(string systemId) =>
        http.GetFromJsonAsync<SystemRunbook>($"runbook/{systemId}", JsonOptions);

    // ── Incidents ────────────────────────────────────────────────────────────
    public Task<List<Incident>?> GetAllIncidentsAsync() =>
        http.GetFromJsonAsync<List<Incident>>("incident", JsonOptions);

    public Task<Incident?> GetIncidentAsync(string incidentNumber) =>
        http.GetFromJsonAsync<Incident>($"incident/{Uri.EscapeDataString(incidentNumber)}", JsonOptions);

    public async Task<Incident?> CreateIncidentAsync(string systemId, CreateIncidentRequest request)
    {
        var response = await http.PostAsJsonAsync($"incident/{systemId}", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Incident>(JsonOptions);
    }

    public async Task<NotificationConfirmation?> NotifyOwnerAsync(string incidentNumber)
    {
        var response = await http.PostAsync($"incident/{incidentNumber}/notify", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NotificationConfirmation>(JsonOptions);
    }

    // ── Database ─────────────────────────────────────────────────────────────
    public async Task<string> ResetDatabaseAsync()
    {
        var response = await http.PostAsync("database/reset", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOptions);
        return result?["message"] ?? "Done.";
    }
}
