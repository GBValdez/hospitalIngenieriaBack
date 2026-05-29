using fletesProyect.catalogues;
using project.utils;
using AppointmentModel = fletesProyect.Appointment.Appointment;

namespace fletesProyect.AppointmentDiseaseOrInjury
{
    public class AppointmentDiseaseOrInjury : CommonsModel<long>
    {
        public long appointmentId { get; set; }
        public AppointmentModel appointment { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public DiseaseOrInjury diseaseOrInjury { get; set; }
    }
}
