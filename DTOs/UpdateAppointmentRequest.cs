namespace MediConnectAPI.DTOs;

public class UpdateAppointmentRequest
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int StatusId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string? Reason { get; set; }
}

