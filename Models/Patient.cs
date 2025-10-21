using System.ComponentModel.DataAnnotations;


namespace MediConnectAPI.Models;

public class Patient
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime BirthDate { get; set; }
    public string? Gender { get; set; }   // "M", "F", "O"
    public string? EmergencyContact { get; set; }
    public MedicalRecord? MedicalRecord { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
