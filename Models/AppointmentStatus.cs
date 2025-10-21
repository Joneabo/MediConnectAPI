namespace MediConnectAPI.Models;

public class AppointmentStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = null!; // Example: "Scheduled", "Completed", "Cancelled"

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
