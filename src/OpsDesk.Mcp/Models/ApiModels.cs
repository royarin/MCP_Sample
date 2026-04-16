namespace OpsDesk.Mcp.Models;

/// <summary>Request body for updating a system's operational status.</summary>
public record UpdateStatusRequest(string Status, string Message);

/// <summary>Request body for creating a new incident.</summary>
public record CreateIncidentRequest(string Title, string Description, string Severity, string ReportedBy);
