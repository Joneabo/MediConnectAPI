using MediConnectAPI.Models;

namespace MediConnectAPI.Services
{
    public interface IAuthService
    {
        User? GetByEmail(string email);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateJwtToken(User user);
    }
}
