using fletesProyect.Patient;
using fletesProyect.Worker;
using System.ComponentModel.DataAnnotations.Schema;
using project.utils;

namespace fletesProyect.Appointment
{
    public class Appointment : CommonsModel<long>
    {
        public DateTime? scheduledDate { get; set; }
        public DateTime? arrivalDate { get; set; }
        public string reason { get; set; }
        public bool isEmergency {  get; set; }
        public DateTime startDate { get; set; }
        public DateTime? endDate { get; set; }
        public string? bloodPressure { get; set; }
        public string? diagnosis { get; set; }
        public string? observations { get; set; }
        public string? treatment { get; set; }
        public float temperature { get; set; }
        public float heartRate { get; set; }
        public float respiratoryRate { get; set; }
        public float oxygenSaturation { get; set; }
        public float weight { get; set; }
        public float height { get; set; }
        public int doctorId { get; set; }
        public int patientId { get; set; }
        public Worker.Worker doctor { get; set; }
        public Patient.Patient patient { get; set; }

        [NotMapped]
        public string? currentStatus { get; set; }

    }
}
