namespace MediConnectAPI.DTOs;

public class PatientDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContact { get; set; }
}
    