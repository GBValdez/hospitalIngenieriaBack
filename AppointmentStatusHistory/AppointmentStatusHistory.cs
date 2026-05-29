using fletesProyect.catalogues;
using project.users;
using project.utils;
using AppointmentModel = fletesProyect.Appointment.Appointment;

namespace fletesProyect.AppointmentStatusHistory
{
    public class AppointmentStatusHistory : CommonsModel<long>
    {
        public long appointmentId { get; set; }
        public AppointmentModel appointment { get; set; }
        public long? previousStatusId { get; set; }
        public AppointmentStatus? previousStatus { get; set; }
        public long statusId { get; set; }
        public AppointmentStatus status { get; set; }
        public string? comment { get; set; }
        public DateTime changedAt { get; set; }
        public string? changedByUserId { get; set; }
        public userEntity? changedByUser { get; set; }
    }
}
