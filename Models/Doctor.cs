namespace MediConnectAPI.Models;

public class Doctor
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string LicenseNumber { get; set; } = null!;
    public int SpecialtyId { get; set; }
    public DoctorSpecialty Specialty { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
