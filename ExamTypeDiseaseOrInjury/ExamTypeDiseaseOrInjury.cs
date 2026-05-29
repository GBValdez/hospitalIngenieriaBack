using fletesProyect.catalogues;
using project.utils;

namespace fletesProyect.ExamTypeDiseaseOrInjury
{
    public class ExamTypeDiseaseOrInjury : CommonsModel<long>
    {
        public long examTypeId { get; set; }
        public ExamType examType { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public DiseaseOrInjury diseaseOrInjury { get; set; }
        public string? notes { get; set; }
    }
}
