using System;

namespace project.Appointment.dto
{
    public class citaCreationDto
    {
        public DateTime? scheduledDate { get; set; }
        public string reason { get; set; }
        public bool isEmergency { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string? bloodPressure { get; set; }
        public string? observations { get; set; }
        public float temperature { get; set; }
        public float heartRate { get; set; }
        public float respiratoryRate { get; set; }
        public float oxygenSaturation { get; set; }
        public float weight { get; set; }
        public float height { get; set; }
        public int doctorId { get; set; }
        public int patientId { get; set; }
    }
}
