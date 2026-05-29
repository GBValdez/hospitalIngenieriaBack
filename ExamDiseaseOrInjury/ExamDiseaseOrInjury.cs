using fletesProyect.catalogues;
using project.utils;
using ExamModel = fletesProyect.Exam.Exam;

namespace fletesProyect.ExamDiseaseOrInjury
{
    public class ExamDiseaseOrInjury : CommonsModel<long>
    {
        public long examId { get; set; }
        public ExamModel exam { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public DiseaseOrInjury diseaseOrInjury { get; set; }
    }
}
