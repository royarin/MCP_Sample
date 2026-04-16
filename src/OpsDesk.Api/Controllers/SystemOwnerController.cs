using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Api.Data;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SystemOwnerController(AppDbContext db) : ControllerBase
{
    /// <summary>Returns the owning team and contact details for all systems.</summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<Models.SystemOwner>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await db.SystemOwners.ToListAsync());

    /// <summary>Returns the owning team and contact details for a system.</summary>
    /// <remarks>Returns team name, contact email, and Slack channel. Source: CMDB registry.</remarks>
    [HttpGet("{systemId}")]
    [ProducesResponseType<Models.SystemOwner>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string systemId)
    {
        var owner = await db.SystemOwners.FindAsync(systemId);
        if (owner is null)
            return NotFound(new { error = $"No ownership record found for system '{systemId}'." });

        return Ok(owner);
    }
}


