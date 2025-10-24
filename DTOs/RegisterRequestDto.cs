namespace MediConnectAPI.DTOs;

public class RegisterRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName  { get; set; } = null!;
    public string Email     { get; set; } = null!;
    public string? Phone    { get; set; }
    public string Password  { get; set; } = null!;

    // Por defecto paciente, pero el admin puede enviar Doctor/Admin
    public string Role { get; set; } = "Patient";  
}
