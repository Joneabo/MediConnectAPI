using MediConnectAPI.Models;

namespace MediConnectAPI.Services
{
    public interface IAuthService
    {
        Task<Usuario?> GetByEmailAsync(string email);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateJwtToken(Usuario usuario);

        Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);

    }
}
