using System.ComponentModel.DataAnnotations;

namespace MediConnectAPI.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? ControlledMedications { get; set; }
        public string? Notes { get; set; }

        public ICollection<ClinicalHistory> ClinicalHistories { get; set; } = new List<ClinicalHistory>();
    }
}
