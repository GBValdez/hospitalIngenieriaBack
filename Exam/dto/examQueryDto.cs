namespace project.Exams.dto
{
    public class examQueryDto
    {
        public long? appointmentId { get; set; }
        public long? examTypeId { get; set; }
        public long? attendantId { get; set; }
        public int? patientId { get; set; }
        public string? dpi { get; set; }
        public int? doctorId { get; set; }
    }
}
