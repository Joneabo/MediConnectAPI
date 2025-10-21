using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using MediConnectAPI.Models;
using MediConnectAPI.DTOs;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MediConnectContext _db;
    public UsersController(MediConnectContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role.Name
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role.Name
            })
            .FirstOrDefaultAsync();

        return user is null ? NotFound("Usuario no encontrado.") : Ok(user);
    }
}
