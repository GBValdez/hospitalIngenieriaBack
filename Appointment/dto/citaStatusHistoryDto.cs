using System;

namespace project.Appointment.dto
{
    public class citaStatusHistoryDto
    {
        public long id { get; set; }
        public long appointmentId { get; set; }
        public long? previousStatusId { get; set; }
        public string? previousStatus { get; set; }
        public long statusId { get; set; }
        public string status { get; set; }
        public string? comment { get; set; }
        public DateTime changedAt { get; set; }
        public string? changedByUserId { get; set; }
        public string? changedByUserName { get; set; }
    }
}
