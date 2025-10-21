using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using MediConnectAPI.Data;
using MediConnectAPI.Models;
using MediConnectAPI.DTOs;

namespace MediConnectAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MediConnectContext _db;

    public AuthController(MediConnectContext db)
    {
        _db = db;
    }

    // ----------------------
    // REGISTRO
    // ----------------------
    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterRequest request)
    {
        // Verificar si el email ya existe
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("El correo ya está registrado.");

        // Normalizamos el rol (por default será Patient)
        var roleName = string.IsNullOrWhiteSpace(request.Role) ? "Patient" : request.Role;
        roleName = roleName.Trim();

        // Buscar rol en la BD
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return BadRequest("Rol inválido.");

        // Si intenta crear un Doctor o Admin desde aquí sin ser admin, lo bloqueamos
        if (roleName != "Patient")
            return Forbid("Solo un administrador puede crear usuarios de tipo Doctor o Admin.");

        // Crear el usuario
        var user = new User
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            Phone     = request.Phone,
            Password  = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId    = role.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            Role = role.Name
        });
    }

    // ----------------------
    // REGISTRO POR ADMIN
    // ----------------------
    [Authorize(Roles = "Admin")]
    [HttpPost("register-by-admin")]
    public async Task<ActionResult> RegisterByAdmin(RegisterRequest request)
    {
        // Validar si email existe
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("El correo ya está registrado.");

        // Rol válido
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            return BadRequest("Rol inválido.");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            Phone     = request.Phone,
            Password  = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId    = role.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(RegisterByAdmin), new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            Role = role.Name
        });
    }
}
