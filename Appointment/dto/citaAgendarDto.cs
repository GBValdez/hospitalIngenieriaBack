using System;

namespace project.Appointment.dto
{
    public class citaAgendarDto
    {
        public DateTime? scheduledDate { get; set; }
        public string reason { get; set; }
        public bool isEmergency { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public int doctorId { get; set; }
        public int patientId { get; set; }
    }
}
