using MediConnectAPI.Models;
using MediConnectAPI.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace MediConnectAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Buscar usuario por email en la base de datos
        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            return await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        // Verificar contraseña con BCrypt
        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        // Generar JWT con los datos del usuario
        public string GenerateJwtToken(Usuario usuario)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"]!;
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiresMinutes = int.Parse(jwtSection["ExpiresMinutes"] ?? "60");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
{
    // Verificar si el usuario ya existe
    var existingUser = await _context.Usuarios
        .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
    if (existingUser)
        return (false, "El correo ya está registrado.");

    // Validar la seguridad de la contraseña (mínimo 6 caracteres, etc.)
    if (request.Password.Length < 6)
        return (false, "La contraseña debe tener al menos 6 caracteres.");

    // Hashear la contraseña con BCrypt
    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

    // Crear nuevo usuario
    var newUser = new Usuario
    {
        Id = Guid.NewGuid(),
        Email = request.Email,
        PasswordHash = hashedPassword,
        Rol = request.Rol ?? "User"
    };

    // Guardar en la base de datos
    _context.Usuarios.Add(newUser);
    await _context.SaveChangesAsync();

    return (true, "Usuario registrado exitosamente.");
}

