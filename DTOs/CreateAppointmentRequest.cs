namespace MediConnectAPI.DTOs;

public class CreateAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int StatusId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Reason { get; set; }
}

