namespace project.Appointment.dto
{
    public class citaQueryDto
    {
        public int? doctorId { get; set; }
        public int? patientId { get; set; }
        public string? estado { get; set; }
        public string? reason { get; set; }
        public DateTime? startDateFrom { get; set; }
        public DateTime? startDateTo { get; set; }
    }
}
