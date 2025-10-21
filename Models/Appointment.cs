namespace MediConnectAPI.Models;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int StatusId { get; set; }
    public AppointmentStatus Status { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }
    public string? Reason { get; set; }
}
