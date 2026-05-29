namespace fletesProyect.ExamTypeDiseaseOrInjury.dto
{
    public class examTypeDiseaseOrInjuryDto
    {
        public long id { get; set; }
        public long examTypeId { get; set; }
        public string examTypeName { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public string diseaseOrInjuryName { get; set; }
        public string? notes { get; set; }
    }

    public class examTypeDiseaseOrInjuryCreationDto
    {
        public long examTypeId { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public string? notes { get; set; }
    }

    public class examTypeDiseaseOrInjuryQueryDto
    {
        public long? examTypeId { get; set; }
        public long? diseaseOrInjuryId { get; set; }
    }
}
