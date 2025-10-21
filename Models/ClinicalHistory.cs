using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.Models
{
    public class ClinicalHistory
    {
        public int Id { get; set; }

        [Required]
        public int MedicalRecordId { get; set; }
        public MedicalRecord MedicalRecord { get; set; } = null!;

        [Required]
        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; } = null!;

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }
}
