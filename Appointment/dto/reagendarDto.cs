using System;

namespace project.Appointment.dto
{
    public class reagendarDto
    {
        public long? citaId { get; set; }
        public DateTime newStartDate { get; set; }
        public DateTime? newEndDate { get; set; }
    }
}
