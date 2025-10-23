using MediConnectAPI.Models;

namespace MyApp.Services
{
    public interface IAuthService
    {
        User? GetByEmail(string email);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateJwtToken(User user);
    }
}
