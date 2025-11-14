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
    // REGISTRO (público): permite Patient (3) y Doctor (2)
    // ----------------------
    [HttpPost("register")]
    public async Task<ActionResult> Register(DTOs.RegisterRequest request)
    {
        // Verificar si el email ya existe
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("El correo ya está registrado.");

        // Mapear el código recibido (1=Admin, 2=Doctor, 3=Patient) a nombre de rol
        var code = request.RoleId ?? 3; // por defecto Patient
        string? roleName = code switch
        {
            1 => "Admin",
            2 => "Doctor",
            3 => "Patient",
            _ => null
        };

        if (roleName is null)
            return BadRequest("RoleId inválido. Use 1=Admin, 2=Doctor, 3=Patient.");

        // En el endpoint público no permitimos crear Admin
        if (roleName == "Admin")
            return Forbid("Solo un administrador puede crear usuarios de tipo Admin.");

        // Validaciones adicionales para doctores
        if (roleName == "Doctor")
        {
            if (string.IsNullOrWhiteSpace(request.LicenseNumber))
                return BadRequest("LicenseNumber es obligatorio para doctores.");
            if (request.SpecialtyId is null)
                return BadRequest("Debe especificar SpecialtyId para doctores.");

            var specialtyExists = await _db.Specialties.AnyAsync(s => s.Id == request.SpecialtyId.Value);
            if (!specialtyExists)
                return BadRequest("La especialidad indicada no existe.");
        }

        // Resolver el Id real por nombre para no depender de IDs de BD
        var roleId = await _db.Roles.Where(r => r.Name == roleName).Select(r => r.Id).FirstOrDefaultAsync();
        if (roleId == 0)
            return StatusCode(500, $"No existe el rol '{roleName}' en la base de datos.");

        var role = await _db.Roles.FindAsync(roleId);

        // Crear el usuario
        var user = new User
        {
            FirstName = request.FirstName,
            LastName  = request.LastName,
            Email     = request.Email,
            Phone     = request.Phone,
            Password  = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId    = roleId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (role.Name == "Patient")
        {
            var hasPatient = await _db.Patients.AnyAsync(p => p.UserId == user.Id);
            if (!hasPatient)
            {
                _db.Patients.Add(new Patient
                {
                    UserId = user.Id,
                    BirthDate = request.BirthDate ?? DateTime.UtcNow.Date,
                    Gender = request.Gender,
                    EmergencyContact = request.EmergencyContact
                });
                await _db.SaveChangesAsync();
            }
        }
        else if (role.Name == "Doctor")
        {
            var hasDoctor = await _db.Doctors.AnyAsync(d => d.UserId == user.Id);
            if (!hasDoctor)
            {
                _db.Doctors.Add(new Doctor
                {
                    UserId = user.Id,
                    LicenseNumber = request.LicenseNumber!,
                    SpecialtyId = request.SpecialtyId!.Value
                });
                await _db.SaveChangesAsync();
            }
        }

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

        if (request.RoleId is null)
            return BadRequest("Debe especificar RoleId (1=Admin, 2=Doctor, 3=Patient).");

        // Mapear el código a nombre y resolver Id real por nombre
        string? nameFromCode = request.RoleId switch
        {
            1 => "Admin",
            2 => "Doctor",
            3 => "Patient",
            _ => null
        };
        if (nameFromCode is null)
            return BadRequest("RoleId inválido. Use 1=Admin, 2=Doctor, 3=Patient.");

        var roleId = await _db.Roles.Where(r => r.Name == nameFromCode).Select(r => r.Id).FirstOrDefaultAsync();
        if (roleId == 0)
            return StatusCode(500, $"No existe el rol '{nameFromCode}' en la base de datos.");

        var role = await _db.Roles.FindAsync(roleId);

        if (nameFromCode == "Doctor")
        {
            if (string.IsNullOrWhiteSpace(request.LicenseNumber))
                return BadRequest("LicenseNumber es obligatorio para doctores.");
            if (request.SpecialtyId is null)
                return BadRequest("Debe especificar SpecialtyId para doctores.");
            var specialtyExists = await _db.Specialties.AnyAsync(s => s.Id == request.SpecialtyId.Value);
            if (!specialtyExists)
                return BadRequest("La especialidad indicada no existe.");
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role!.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (role.Name == "Patient")
        {
            var hasPatient = await _db.Patients.AnyAsync(p => p.UserId == user.Id);
            if (!hasPatient)
            {
                _db.Patients.Add(new Patient
                {
                    UserId = user.Id,
                    BirthDate = request.BirthDate ?? DateTime.UtcNow.Date,
                    Gender = request.Gender,
                    EmergencyContact = request.EmergencyContact
                });
                await _db.SaveChangesAsync();
            }
        }
        else if (role.Name == "Doctor")
        {
            var hasDoctor = await _db.Doctors.AnyAsync(d => d.UserId == user.Id);
            if (!hasDoctor)
            {
                _db.Doctors.Add(new Doctor
                {
                    UserId = user.Id,
                    LicenseNumber = request.LicenseNumber!,
                    SpecialtyId = request.SpecialtyId!.Value
                });
                await _db.SaveChangesAsync();
            }
        }

        return CreatedAtAction(nameof(RegisterByAdmin), new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            Role = role!.Name
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
        // Also emit standard role claim for compatibility with default RoleClaimType
        new Claim(ClaimTypes.Role, user.Role.Name),
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
