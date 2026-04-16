using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsDesk.Api.Data;

namespace OpsDesk.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DatabaseController(AppDbContext db) : ControllerBase
{
    /// <summary>Resets the database to its original seeded state.</summary>
    /// <remarks>
    /// Drops the entire database and recreates it by re-running all migrations and seed data.
    /// </remarks>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reset()
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        return Ok(new { message = "Database has been reset and re-seeded successfully." });
    }
}
