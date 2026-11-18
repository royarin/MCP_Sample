using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpsDesk.Mcp.Tasks.Models;

namespace OpsDesk.Mcp.Tasks.Tools;

/// <summary>
/// Five task-shaped tools. The attributes below are the prompt-engineering surface:
/// names are verbs, descriptions say when to use the tool, inputs are described, results are flat
/// records with an explicit NextAction / Explanation, and failures are sentences the model can relay.
/// </summary>
[McpServerToolType]
public sealed class TaskShapedTools(OpsDeskClient api)
{
    // ── results ────────────────────────────────────────────────────────────────

    public sealed record SystemBriefing(
        string SystemId, string Status, string StatusMessage, DateTimeOffset StatusUpdatedAt,
        string OwnerTeam, string OwnerEmail, string OwnerSlack,
        string? RunbookTitle, string? RunbookUrl, DateOnly? RunbookLastReviewed,
        IReadOnlyList<string> OpenIncidents, string NextAction);

    public sealed record HandoverSummary(int Systems, int Degraded, IReadOnlyList<string> DegradedSystems, IReadOnlyList<string> RecentIncidents, string Summary);

    public sealed record SystemMatch(string SystemId, string OwnerTeam, string Status);

    // ── tools ──────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "get_system_briefing"),
     Description("Everything an on-call engineer needs about ONE system in a single call: current status and message, " +
                 "owning team with email and Slack channel, the approved runbook, open incidents, and a recommended next action. " +
                 "Use this FIRST whenever a system is named. Read-only.")]
    public async Task<SystemBriefing> GetSystemBriefing(
        [Description("System id, e.g. inventory, auth, payments")] string systemId, CancellationToken ct)
    {
        var id = await api.ResolveSystemIdAsync(systemId, ct);
        var status = await api.StatusAsync(id, ct);
        var owner = await api.OwnerAsync(id, ct);
        var runbook = await api.RunbookAsync(id, ct);
        var incidents = (await api.AllIncidentsAsync(ct)).Where(i => i.SystemId.Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();

        var st = status?.Status ?? "unknown";
        var next = (st.ToLowerInvariant(), incidents.Count) switch
        {
            ("red", 0) => $"System is RED with no incident raised yet. Raise a Critical incident with raise_incident and notify {owner?.TeamName} ({owner?.SlackChannel}).",
            ("red", _) => $"System is RED and {incidents.Count} incident(s) exist – confirm the owner has been notified and follow the runbook.",
            ("yellow", 0) => "System is degraded. Monitor; raise a Medium incident if it persists or users are affected.",
            ("yellow", _) => "System is degraded with an incident open – check whether the runbook steps have been applied.",
            ("green", > 0) => "System reports healthy but has incidents on record – possible monitoring gap; verify and close stale incidents.",
            _ => "System is healthy. Nothing to do."
        };

        return new SystemBriefing(id, st, status?.Message ?? "", status?.UpdatedAt ?? default,
            owner?.TeamName ?? "unknown", owner?.ContactEmail ?? "", owner?.SlackChannel ?? "",
            runbook?.Title, runbook?.Url, runbook?.LastReviewed,
            incidents.OrderByDescending(i => i.CreatedAt).Select(i => $"{i.IncidentNumber} · {i.Severity} · {i.Title} · {i.CreatedAt:yyyy-MM-dd HH:mm}Z").ToList(),
            next);
    }

    [McpServerTool(Name = "get_oncall_handover"),
     Description("Summary of all systems that are not green, with owners and recent incidents – for a standup or on-call handover. " +
                 "Use when the user asks for an overview, 'what's going on', or a handover. Read-only.")]
    public async Task<HandoverSummary> GetOncallHandover(CancellationToken ct)
    {
        var statuses = await api.AllStatusesAsync(ct);
        var owners = (await api.AllOwnersAsync(ct)).ToDictionary(o => o.SystemId, StringComparer.OrdinalIgnoreCase);
        var incidents = await api.AllIncidentsAsync(ct);
        var degraded = statuses.Where(s => !s.Status.Equals("green", StringComparison.OrdinalIgnoreCase)).ToList();
        return new HandoverSummary(statuses.Count, degraded.Count,
            degraded.Select(s => $"{s.SystemId} · {s.Status.ToUpperInvariant()} · {s.Message} · owner {(owners.TryGetValue(s.SystemId, out var o) ? $"{o.TeamName} {o.SlackChannel}" : "unknown")}").ToList(),
            incidents.OrderByDescending(i => i.CreatedAt).Take(5).Select(i => $"{i.IncidentNumber} · {i.SystemId} · {i.Severity} · {i.Title}").ToList(),
            degraded.Count == 0 ? "All systems green." : $"{degraded.Count} of {statuses.Count} systems need attention; start with any RED system that has no incident.");
    }

    [McpServerTool(Name = "find_system"),
     Description("Finds systems by partial id or by owning team name. Use when the user is unsure of the exact system id. Read-only.")]
    public async Task<IReadOnlyList<SystemMatch>> FindSystem(
        [Description("Part of a system id or team name, e.g. 'inv' or 'Supply Chain'")] string nameContains, CancellationToken ct)
    {
        var owners = await api.AllOwnersAsync(ct);
        var statuses = (await api.AllStatusesAsync(ct)).ToDictionary(s => s.SystemId, StringComparer.OrdinalIgnoreCase);
        var hits = owners.Where(o => o.SystemId.Contains(nameContains, StringComparison.OrdinalIgnoreCase) || o.TeamName.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                         .Select(o => new SystemMatch(o.SystemId, o.TeamName, statuses.TryGetValue(o.SystemId, out var s) ? s.Status : "unknown")).ToList();
        if (hits.Count == 0) throw new McpException($"No system or team matches '{nameContains}'. Known systems: {string.Join(", ", owners.Select(o => o.SystemId))}.");
        return hits;
    }

    [McpServerTool(Name = "raise_incident"),
     Description("Raises an incident for a system and optionally notifies the owning team. IRREVERSIBLE – confirm with the user first. " +
                 "Validates the system id (suggests the closest match on a typo) and the severity (low, medium, high, critical).")]
    public async Task<string> RaiseIncident(
        [Description("System id, e.g. inventory")] string systemId,
        [Description("Short title")] string title,
        [Description("What is happening and the impact")] string description,
        [Description("low, medium, high or critical")] string severity,
        [Description("Who is reporting, e.g. a name or handle")] string reportedBy,
        [Description("true to e-mail the owning team immediately")] bool notifyOwner,
        CancellationToken ct)
    {
        var id = await api.ResolveSystemIdAsync(systemId, ct);
        var sev = severity.Trim().ToLowerInvariant();
        if (sev is not ("low" or "medium" or "high" or "critical"))
            throw new McpException($"Severity '{severity}' is not valid. Use low, medium, high or critical.");

        var incident = await api.CreateIncidentAsync(id, new CreateIncidentRequest(title, description, sev, reportedBy), ct);
        var result = $"Raised {incident.IncidentNumber} ({sev}) for {id}: \"{incident.Title}\". System status at creation: {incident.CurrentStatus}.";
        if (notifyOwner)
        {
            var n = await api.NotifyAsync(incident.IncidentNumber, ct);
            result += $" Owner notified at {n.SentTo} – subject \"{n.Subject}\".";
        }
        else
        {
            var owner = await api.OwnerAsync(id, ct);
            result += $" Owner not notified yet – {owner?.TeamName} ({owner?.ContactEmail}, {owner?.SlackChannel}).";
        }
        return result;
    }

    [McpServerTool(Name = "update_system_status"),
     Description("Acknowledges or resolves: sets a system's status to green, yellow or red with a message. IRREVERSIBLE – confirm with the user first.")]
    public async Task<string> UpdateSystemStatus(
        [Description("System id")] string systemId,
        [Description("green, yellow or red")] string status,
        [Description("Human-readable message describing the current state")] string message,
        CancellationToken ct)
    {
        var id = await api.ResolveSystemIdAsync(systemId, ct);
        var st = status.Trim().ToLowerInvariant();
        if (st is not ("green" or "yellow" or "red"))
            throw new McpException($"Status '{status}' is not valid. Use green, yellow or red.");
        var updated = await api.UpdateStatusAsync(id, new UpdateStatusRequest(st, message), ct);
        return $"{id} is now {updated.Status.ToUpperInvariant()}: \"{updated.Message}\" (updated {updated.UpdatedAt:HH:mm}Z).";
    }
}
