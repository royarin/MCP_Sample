using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Api.Data;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class RunbookController(AppDbContext db) : ControllerBase
{
    /// <summary>Returns all approved runbooks.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Models.SystemRunbook>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await db.SystemRunbooks.ToListAsync());

    /// <summary>Returns the approved runbook URL for a system.</summary>
    /// <remarks>Returns the runbook title, URL, and last review date.</remarks>
    [HttpGet("{systemId}")]
    [ProducesResponseType<Models.SystemRunbook>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string systemId)
    {
        var runbook = await db.SystemRunbooks.FindAsync(systemId);
        if (runbook is null)
            return NotFound(new { error = $"No runbook found for system '{systemId}'." });

        return Ok(runbook);
    }
}


