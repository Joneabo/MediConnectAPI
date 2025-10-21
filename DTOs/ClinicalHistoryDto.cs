public class ClinicalHistoryDto
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
    }