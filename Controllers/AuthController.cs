using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    private readonly IConfiguration _config;

    public AuthController(MediConnectContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ----------------------
    // REGISTRO
    // ----------------------
    [HttpPost("register")]
    public async Task<ActionResult> Register(DTOs.RegisterRequest request)
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
    public async Task<ActionResult> RegisterByAdmin(DTOs.RegisterRequest request)
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
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id
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
    
    [HttpPost("login")]
[AllowAnonymous]
public async Task<ActionResult> Login([FromBody] DTOs.LoginRequest request)
{
    // 1. Find the user (also load the Role)
    var user = await _db.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user == null)
        return Unauthorized("Credenciales inválidas.");

    // 2. Verify password with BCrypt
    bool passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
    if (!passwordOk)
        return Unauthorized("Credenciales inválidas.");

    // 3. Build token claims
    var claims = new[]
    {
        new Claim("userId", user.Id.ToString()),
        new Claim("role",   user.Role.Name),
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    // 4. Build signing credentials
    var jwtSection   = _config.GetSection("Jwt");
    var keyBytes     = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
    var creds        = new SigningCredentials(
        new SymmetricSecurityKey(keyBytes),
        SecurityAlgorithms.HmacSha256
    );

    // 5. Create token
    var token = new JwtSecurityToken(
        issuer: jwtSection["Issuer"],
        audience: jwtSection["Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(6),
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    // 6. Return token + basic profile info
    return Ok(new
    {
        token = tokenString,
        expiresAt = token.ValidTo,
        user = new
        {
            id = user.Id,
            firstName = user.FirstName,
            lastName = user.LastName,
            email = user.Email,
            role = user.Role.Name
        }
    });
}

}
