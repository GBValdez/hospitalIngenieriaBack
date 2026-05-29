namespace fletesProyect.MedicineDiseaseOrInjuryDosage.dto
{
    public class medicineDiseaseOrInjuryDosageDto
    {
        public long id { get; set; }
        public long medicineId { get; set; }
        public string medicineName { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public string diseaseOrInjuryName { get; set; }
        public int recommendedAmount { get; set; }
        public int maximumAmount { get; set; }
        public string? notes { get; set; }
    }
}
