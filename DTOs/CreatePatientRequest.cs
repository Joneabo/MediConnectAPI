namespace MediConnectAPI.DTOs;

public class CreatePatientRequest
{
    public int UserId { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? EmergencyContact { get; set; }
}

