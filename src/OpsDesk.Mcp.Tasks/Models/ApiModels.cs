namespace OpsDesk.Mcp.Tasks.Models;

// What the OpsDesk API returns. Enums arrive as camelCase strings (green / critical …).
public record SystemStatusDto(string SystemId, string Status, string Message, DateTimeOffset UpdatedAt);
public record SystemOwnerDto(string SystemId, string TeamName, string ContactEmail, string SlackChannel);
public record RunbookDto(string SystemId, string Title, string Url, DateOnly LastReviewed);
public record IncidentDto(string IncidentNumber, string SystemId, string Title, string Description, string Severity,
                          string ReportedBy, string CurrentStatus, string StatusMessage, DateTimeOffset CreatedAt);
public record NotificationDto(string IncidentNumber, string SentTo, string Subject, string Body, DateTimeOffset NotifiedAt);

public record CreateIncidentRequest(string Title, string Description, string Severity, string ReportedBy);
public record UpdateStatusRequest(string Status, string Message);
