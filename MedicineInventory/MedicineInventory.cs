using fletesProyect.Medicine;
using project.utils;

namespace fletesProyect.MedicineInventory
{
    public class MedicineInventory : CommonsModel<long>
    {
        public long medicineId { get; set; }
        public Medicine.Medicine medicine { get; set; }
        public int stock { get; set; }
    }
}
