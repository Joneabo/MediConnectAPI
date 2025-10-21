namespace MediConnectAPI.DTOs
{
    public class MedicalRecordDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string? MedicalHistory { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? ControlledMedications { get; set; }
        public string? Notes { get; set; }
    }

}
