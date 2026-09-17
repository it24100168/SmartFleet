using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartFleet.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Sample endpoint accessible exclusively to users with the Operator role.
    /// </summary>
    [HttpGet("operator-only")]
    [Authorize(Roles = "Operator")]
    public IActionResult OperatorOnly()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new
        {
            message = "Access granted to Operator-only endpoint.",
            userId,
            email,
            role = "Operator",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sample endpoint accessible exclusively to users with the Technician role.
    /// </summary>
    [HttpGet("technician-only")]
    [Authorize(Roles = "Technician")]
    public IActionResult TechnicianOnly()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new
        {
            message = "Access granted to Technician-only endpoint.",
            userId,
            email,
            role = "Technician",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sample endpoint accessible exclusively to users with the Supervisor role.
    /// </summary>
    [HttpGet("supervisor-only")]
    [Authorize(Roles = "Supervisor")]
    public IActionResult SupervisorOnly()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);

        return Ok(new
        {
            message = "Access granted to Supervisor-only endpoint.",
            userId,
            email,
            role = "Supervisor",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Sample endpoint accessible to any authenticated user regardless of role.
    /// </summary>
    [HttpGet("authenticated")]
    [Authorize]
    public IActionResult AuthenticatedOnly()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new
        {
            message = "Access granted to general authenticated endpoint.",
            userId,
            email,
            role,
            timestamp = DateTime.UtcNow
        });
    }
}
