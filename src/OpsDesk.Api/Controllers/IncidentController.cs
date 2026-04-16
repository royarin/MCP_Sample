using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Api.Data;
using OpsDesk.Api.Models;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class IncidentController(AppDbContext db) : ControllerBase
{
    /// <summary>Returns all incidents, ordered most recent first.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Incident>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var incidents = await db.Incidents.ToListAsync();
        return Ok(incidents.OrderByDescending(i => i.CreatedAt));
    }

    /// <summary>Returns a single incident by incident number.</summary>
    [HttpGet("{incidentNumber}")]
    [ProducesResponseType<Incident>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string incidentNumber)
    {
        var incident = await db.Incidents.FindAsync(incidentNumber);
        return incident is null
            ? NotFound(new { error = $"Incident '{incidentNumber}' not found." })
            : Ok(incident);
    }

    /// <summary>Creates a new incident for a system.</summary>
    /// <remarks>
    /// Generates a sequential incident number, captures the current system status snapshot,
    /// and persists the incident to the database.
    /// </remarks>
    [HttpPost("{systemId}")]
    [ProducesResponseType<Incident>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(string systemId, [FromBody] CreateIncidentRequest request)
    {
        var owner = await db.SystemOwners.FindAsync(systemId);
        if (owner is null)
            return NotFound(new { error = $"System '{systemId}' not found." });

        var currentStatus = await db.SystemStatuses.FindAsync(systemId);

        var count = await db.Incidents.CountAsync() + 1;
        var number = $"INC-{count:D5}";

        var incident = new Incident
        {
            IncidentNumber = number,
            SystemId       = systemId,
            Title          = request.Title,
            Description    = request.Description,
            Severity       = request.Severity,
            ReportedBy     = request.ReportedBy,
            CurrentStatus  = currentStatus?.Status ?? SystemHealthStatus.Unknown,
            StatusMessage  = currentStatus?.Message ?? string.Empty,
            CreatedAt      = DateTimeOffset.UtcNow
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Create), new { systemId }, incident);
    }

    /// <summary>Sends an email notification to the system owner for an existing incident.</summary>
    /// <remarks>
    /// Looks up the system owner from the database and composes a mock email notification.
    /// No real email is sent — the composed message is returned in the response.
    /// </remarks>
    [HttpPost("{incidentNumber}/notify")]
    [ProducesResponseType<NotificationConfirmation>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Notify(string incidentNumber)
    {
        var incident = await db.Incidents.FindAsync(incidentNumber);
        if (incident is null)
            return NotFound(new { error = $"Incident '{incidentNumber}' not found." });

        var owner = await db.SystemOwners.FindAsync(incident.SystemId);
        if (owner is null)
            return NotFound(new { error = $"No owner found for system '{incident.SystemId}'." });

        var subject = $"[{incident.Severity.ToString().ToUpper()}] Incident {incident.IncidentNumber} raised for {incident.SystemId}";
        var body = $"""
            Incident Number : {incident.IncidentNumber}
            System          : {incident.SystemId}
            Severity        : {incident.Severity}
            Title           : {incident.Title}

            {incident.Description}

            Current Status  : {incident.CurrentStatus} — {incident.StatusMessage}
            Reported By     : {incident.ReportedBy}
            Created At      : {incident.CreatedAt:u}

            Please investigate and take appropriate action.
            """;

        return Ok(new NotificationConfirmation(
            IncidentNumber: incidentNumber,
            SentTo: owner.ContactEmail,
            Subject: subject,
            Body: body,
            NotifiedAt: DateTimeOffset.UtcNow
        ));
    }
}

public record CreateIncidentRequest(string Title, string Description, IncidentSeverity Severity, string ReportedBy);

public record NotificationConfirmation(
    string IncidentNumber,
    string SentTo,
    string Subject,
    string Body,
    DateTimeOffset NotifiedAt
);
