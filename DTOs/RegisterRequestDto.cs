namespace MediConnectAPI.DTOs;

public class RegisterRequest
{
    public string FirstName { get; set; } = null!;
    public string LastName  { get; set; } = null!;
    public string Email     { get; set; } = null!;
    public string? Phone    { get; set; }
    public string Password  { get; set; } = null!;

    // Nuevo: enviar el Id del rol (1=Admin, 2=Doctor, 3=Patient)
    // Para el endpoint público (register) se ignora cualquier valor que no sea Patient.
    public int? RoleId { get; set; }
}
