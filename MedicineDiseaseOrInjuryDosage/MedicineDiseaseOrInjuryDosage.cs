using fletesProyect.catalogues;
using fletesProyect.Medicine;
using project.utils;

namespace fletesProyect.MedicineDiseaseOrInjuryDosage
{
    public class MedicineDiseaseOrInjuryDosage : CommonsModel<long>
    {
        public long medicineId { get; set; }
        public Medicine.Medicine medicine { get; set; }
        public long diseaseOrInjuryId { get; set; }
        public DiseaseOrInjury diseaseOrInjury { get; set; }
        public int recommendedAmount { get; set; }
        public int maximumAmount { get; set; }
        public string? notes { get; set; }
    }
}
