using System.ComponentModel.DataAnnotations;

namespace project.Appointment.dto
{
    public class inicioCitaDto
    {
        [Required]
        public long appointmentId { get; set; }

        [Required]
        public float bloodPressure { get; set; }

        [Required]
        public float temperature { get; set; }

        [Required]
        public float heartRate { get; set; }

        [Required]
        public float respiratoryRate { get; set; }

        [Required]
        public float oxygenSaturation { get; set; }

        [Required]
        public float weight { get; set; }

        [Required]
        public float height { get; set; }
    }
}
