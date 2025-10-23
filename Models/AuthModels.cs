namespace MediConnectAPI.Models
{
    public class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!; // hash con BCrypt
        public string Role { get; set; } = "User";
    }
}
