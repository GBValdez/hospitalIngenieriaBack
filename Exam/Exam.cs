using fletesProyect.catalogues;
using fletesProyect.Worker;
using project.utils;

namespace fletesProyect.Exam
{
    public class Exam:CommonsModel<long>
    {
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string results { get; set; }
        public string observations { get; set; }
        public long examTypeId {  get; set; }
        public ExamType examType { get; set; }
        public long appointmentId {  get; set; }
        public Appointment.Appointment appointment { get; set; }
        public long attendantId {  get; set; }
        public Worker.Worker attendant { get; set; }
    }
}
