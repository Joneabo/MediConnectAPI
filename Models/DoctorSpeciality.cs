namespace MediConnectAPI.Models;

public class DoctorSpecialty
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
