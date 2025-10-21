namespace MediConnectAPI.DTOs;

public class AppointmentDto
{
    public int Id { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Reason { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = null!;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = null!;
    public string Specialty { get; set; } = null!;
    public string Status { get; set; } = null!;
}
