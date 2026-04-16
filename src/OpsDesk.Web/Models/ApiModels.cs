namespace OpsDesk.Web.Models;

public enum SystemHealthStatus { Unknown, Green, Yellow, Red }

public enum IncidentSeverity { Low, Medium, High, Critical }

public class SystemStatus
{
    public string SystemId { get; set; } = string.Empty;
    public SystemHealthStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SystemOwner
{
    public string SystemId { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string SlackChannel { get; set; } = string.Empty;
}

public class SystemRunbook
{
    public string SystemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateOnly LastReviewed { get; set; }
}

public class Incident
{
    public string IncidentNumber { get; set; } = string.Empty;
    public string SystemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public SystemHealthStatus CurrentStatus { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;
    public string ReportedBy { get; set; } = string.Empty;
}

public class UpdateStatusRequest
{
    public SystemHealthStatus Status { get; set; } = SystemHealthStatus.Green;
    public string Message { get; set; } = string.Empty;
}

public class NotificationConfirmation
{
    public string IncidentNumber { get; set; } = string.Empty;
    public string SentTo { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset NotifiedAt { get; set; }
}
