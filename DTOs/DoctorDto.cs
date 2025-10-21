namespace MediConnectAPI.DTOs;

public class DoctorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string LicenseNumber { get; set; } = null!;
    public string Specialty { get; set; } = null!;
}
