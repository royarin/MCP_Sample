using System;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace OpsDesk.Mcp.Prompts;

[McpServerPromptType]
public class IncidentTriagePrompt
{
    [McpServerPrompt(Name = "summarize_system_health")]
    [Description("Generates a comprehensive health dashboard across all systems, correlating current statuses, open incidents, and owner contacts. Ideal for a quick operational standup or on-call handover.")]
    public string SummarizeSystemHealth()
    {
        return @"Please produce a full operational health dashboard for all systems managed by OpsDesk as of " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + @" UTC.

**Data Collection (run these tools):**
1. `GetAllSystemStatuses` — retrieve the current health of every system
2. `GetAllSystemOwners` — retrieve team name, contact email, and Slack channel for every system
3. `GetAllIncidents` — retrieve all incidents (most recent first)

**Analysis Requirements:**

1. **Status Breakdown:**
   - Count systems by health status: Green / Yellow / Red
   - List every non-Green system with its current status message and how long it has been in that state (if determinable)

2. **Incident Correlation:**
   - For each non-Green system, list any open or recent incidents (match on systemId)
   - Highlight the most severe incident per system (Critical > High > Medium > Low)
   - Flag any system that has an incident but is still showing Green (potential monitoring gap)

3. **Escalation Contacts:**
   - For every degraded system, include the owning team name, contact email, and Slack channel

4. **Overall Risk Assessment:**
   - Identify systemic patterns (e.g., multiple systems down at the same time suggesting infrastructure-level impact)
   - Surface any Critical or High severity incidents that have not yet been notified

5. **Output Format:**
   Present findings as:
   - **Executive Summary** — 2-3 sentences describing the overall health posture right now
   - **Degraded Systems Table** — columns: System | Status | Severity | Incident # | Owner Team | Slack Channel
   - **Green Systems** — brief list confirming healthy systems
   - **Recommended Immediate Actions** — prioritized by severity

**Reference Timestamp:** " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + @" UTC

Produce a concise, actionable dashboard suitable for an on-call engineer or engineering manager.";
    }

    [McpServerPrompt(Name = "triage_incidents_by_severity")]
    [Description("Produces a prioritized incident triage report grouping all open incidents by severity (Critical → High → Medium → Low), with recommended next steps, runbook links, and owner escalation paths for each.")]
    public string TriageIncidentsBySeverity()
    {
        return @"Please perform a full incident triage across all OpsDesk incidents as of " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + @" UTC.

**Data Collection (run these tools):**
1. `GetAllIncidents` — retrieve all incidents ordered most recent first
2. For each unique systemId found in incidents:
   - `GetSystemStatus` — get current health status of the affected system
   - `GetRunbook` — retrieve the runbook title and URL for the system
   - `GetSystemOwner` — retrieve team name, contact email, and Slack channel

**Triage Requirements:**

1. **Severity Grouping (Critical first):**
   Group all incidents into four severity bands: Critical → High → Medium → Low.
   Within each band, order by most recent `createdAt` first.

2. **Per-Incident Detail:**
   For each incident, include:
   - Incident number, title, and description
   - Affected system and its **current** health status
   - Time since created (e.g., ""2 hours ago"")
   - Reporting party (`reportedBy`)
   - Owning team, contact email, and Slack channel
   - Link to the system's runbook (if available)

3. **Recommended Actions:**
   For each Critical or High incident:
   - State whether the system owner has been notified (check if `NotifyIncident` should be called)
   - Suggest the first runbook step or remediation action based on the incident description
   - Indicate if the current system status has changed since the incident was raised

4. **Cross-Incident Patterns:**
   - Identify if multiple incidents share the same systemId (repeated failures)
   - Identify if multiple incidents were created within a short window (potential cascading event)
   - Flag any incidents where the system is now Green but the incident is still open (possible stale incident)

5. **Output Format:**
   Present findings as:
   - **Triage Summary** — total incident count broken down by severity
   - **Critical Incidents** — full detail per incident (see above)
   - **High Incidents** — full detail per incident
   - **Medium Incidents** — condensed table: Incident # | System | Title | Owner | Runbook
   - **Low Incidents** — condensed table: Incident # | System | Title | Owner
   - **Pattern Observations** — any systemic or correlated issues
   - **Recommended Priority Actions** — ordered action list for the on-call engineer

**Reference Timestamp:** " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + @" UTC

Deliver a clear, prioritized triage report that enables an on-call engineer to immediately understand what needs attention and in what order.";
    }
}
