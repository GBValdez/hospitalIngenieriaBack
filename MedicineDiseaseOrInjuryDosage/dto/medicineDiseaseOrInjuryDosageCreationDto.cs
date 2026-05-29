namespace fletesProyect.MedicineDiseaseOrInjuryDosage.dto
{
    public class medicineDiseaseOrInjuryDosageCreationDto
    {
        public long medicineId { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public int recommendedAmount { get; set; }
        public int maximumAmount { get; set; }
        public string? notes { get; set; }
    }
}
