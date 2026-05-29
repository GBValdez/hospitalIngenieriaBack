using System;
using System.Collections.Generic;
using project.utils.dto;

namespace project.Appointment.dto
{
    public class citaDto
    {
        public long Id { get; set; }
        public DateTime? scheduledDate { get; set; }
        public DateTime? arrivalDate { get; set; }
        public string reason { get; set; }
        public bool isEmergency { get; set; }
        public DateTime startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? bloodPressure { get; set; }
        public string? observations { get; set; }
        public List<long> diseaseOrInjuryIds { get; set; } = new List<long>();
        public List<string> diseasesOrInjuries { get; set; } = new List<string>();
        public float temperature { get; set; }
        public float heartRate { get; set; }
        public float respiratoryRate { get; set; }
        public float oxygenSaturation { get; set; }
        public float weight { get; set; }
        public float height { get; set; }
        public int doctorId { get; set; }
        public int patientId { get; set; }
        public string? doctorName { get; set; }
        public string? patientName { get; set; }
        public string status { get; set; }
    }
}
