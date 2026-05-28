using fletesProyect.Appointment;
using fletesProyect.Medicine;
using project.utils;

namespace fletesProyect.Recipe
{
    public class Recipe : CommonsModel<long>
    {
        public int days {  get; set; }
        public int timeLimit { get; set; }
        public long medicineId { get; set; }
        public Medicine.Medicine medicine { get; set; }
        public long appointmentId {  get; set; }
        public Appointment.Appointment appointment { get; set; }
    }
}
