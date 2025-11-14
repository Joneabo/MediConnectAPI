using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/_debug")] 
public class DiagnosticsController : ControllerBase
{
    // Authenticated echo of claims and role checks
    [Authorize]
    [HttpGet("me")] 
    public ActionResult GetMe()
    {
        var identity = User.Identity as ClaimsIdentity;
        var roleType = identity?.RoleClaimType ?? ClaimTypes.Role;
        var roles = User.Claims.Where(c => c.Type == roleType).Select(c => c.Value).ToList();

        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated ?? false,
            name = User.Identity?.Name,
            roleClaimType = roleType,
            roles,
            hasRolePatient = User.IsInRole("Patient"),
            hasRoleDoctor = User.IsInRole("Doctor"),
            hasRoleAdmin = User.IsInRole("Admin"),
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}

