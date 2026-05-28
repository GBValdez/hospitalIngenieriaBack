using fletesProyect.catalogues;
using project.utils;

namespace fletesProyect.DoctorSpecialty
{
    public class DoctorSpecialty:CommonsModel<long>
    {
        public long doctorId { get; set; }
        public Worker.Worker doctor {  get; set; }
        public long specialtyId {  get; set; }
        public Specialty specialty { get; set; }
        public string licenseNumber {  get; set; }
    }
}
