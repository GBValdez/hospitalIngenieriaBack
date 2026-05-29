using System;

namespace project.Exams.dto
{
    public class examDto
    {
        public long id { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string results { get; set; }
        public string observations { get; set; }
        public long examTypeId { get; set; }
        public string examTypeName { get; set; }
        public long appointmentId { get; set; }
        public string appointmentReason { get; set; }
        public long attendantId { get; set; }
        public string attendantName { get; set; }
        public int doctorId { get; set; }
        public string doctorName { get; set; }
        public int patientId { get; set; }
        public string patientName { get; set; }
        public string status { get; set; }
    }
}
