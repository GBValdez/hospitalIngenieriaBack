using fletesProyect.catalogues;
using project.utils;

namespace fletesProyect.LaboratoryAttendantExamType
{
    public class LaboratoryAttendantExamType : CommonsModel<long>
    {
        public long attendantId { get; set; }
        public Worker.Worker attendant { get; set; }
        public long examTypeId { get; set; }
        public ExamType examType { get; set; }
    }
}
