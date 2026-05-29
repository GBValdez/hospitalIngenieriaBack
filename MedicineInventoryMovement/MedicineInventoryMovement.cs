using fletesProyect.Medicine;
using project.users;
using project.utils;
using DispatchModel = fletesProyect.Dispatch.Dispatch;

namespace fletesProyect.MedicineInventoryMovement
{
    public class MedicineInventoryMovement : CommonsModel<long>
    {
        public long medicineId { get; set; }
        public Medicine.Medicine medicine { get; set; }
        public string movementType { get; set; }
        public int amount { get; set; }
        public int previousStock { get; set; }
        public int newStock { get; set; }
        public float unitPrice { get; set; }
        public string reason { get; set; }
        public long? dispatchId { get; set; }
        public DispatchModel? dispatch { get; set; }
        public string? registeredByUserId { get; set; }
        public userEntity? registeredByUser { get; set; }
    }
}
