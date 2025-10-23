namespace MediConnectAPI.Models
{
    public class RegisterRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string? Rol { get; set; } = "User"; // opcional, por defecto "User"
    }
}
