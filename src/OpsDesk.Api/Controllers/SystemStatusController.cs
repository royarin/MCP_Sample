using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Api.Data;
using OpsDesk.Api.Models;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SystemStatusController(AppDbContext db) : ControllerBase
{
    /// <summary>Returns the current operational status of all systems.</summary>
    /// <remarks>Optionally filter by status: green, yellow, or red.</remarks>
    [HttpGet]
    [ProducesResponseType<IEnumerable<SystemStatus>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] SystemHealthStatus? status)
    {
        var query = db.SystemStatuses.AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return Ok(await query.ToListAsync());
    }

    /// <summary>Returns the current operational status of a system.</summary>
    /// <remarks>Returns green, yellow, or red status with a human-readable message.</remarks>
    [HttpGet("{systemId}")]
    [ProducesResponseType<SystemStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string systemId)
    {
        var status = await db.SystemStatuses.FindAsync(systemId);
        if (status is null)
            return NotFound(new { error = $"System '{systemId}' not found in status registry." });

        return Ok(status);
    }

    /// <summary>Updates the operational status of a system.</summary>
    /// <remarks>Use to acknowledge or resolve an incident. Accepted status values: green, yellow, red.</remarks>
    [HttpPost("{systemId}")]
    [ProducesResponseType<SystemStatus>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string systemId, [FromBody] UpdateStatusRequest request)
    {
        var status = await db.SystemStatuses.FindAsync(systemId);
        if (status is null)
            return NotFound(new { error = $"System '{systemId}' not found in status registry." });

        status.Status = request.Status;
        status.Message = request.Message;
        status.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(status);
    }
}

public record UpdateStatusRequest(SystemHealthStatus Status, string Message);


