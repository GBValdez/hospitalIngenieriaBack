using System;
using System.Collections.Generic;

namespace fletesProyect.Medicine.dto
{
    public class recetaDespachoDto
    {
        public long appointmentId { get; set; }
        public string appointmentReason { get; set; }
        public int patientId { get; set; }
        public string patientName { get; set; }
        public int doctorId { get; set; }
        public string doctorName { get; set; }
        public DateTime appointmentDate { get; set; }
        public List<recetaDespachoItemDto> medicines { get; set; } = new List<recetaDespachoItemDto>();
    }

    public class recetaDespachoItemDto
    {
        public long recipeId { get; set; }
        public long medicineId { get; set; }
        public string medicineName { get; set; }
        public int days { get; set; }
        public int timeLimit { get; set; }
        public int prescribedAmount { get; set; }
        public int alreadyDispatched { get; set; }
        public int pendingAmount { get; set; }
        public int availableStock { get; set; }
        public float price { get; set; }
    }

    public class inventarioMedicinaDto
    {
        public long medicineId { get; set; }
        public string medicineName { get; set; }
        public float price { get; set; }
        public int stock { get; set; }
    }

    public class inventarioMovimientoDto
    {
        public long id { get; set; }
        public long medicineId { get; set; }
        public string medicineName { get; set; }
        public string movementType { get; set; }
        public int amount { get; set; }
        public int previousStock { get; set; }
        public int newStock { get; set; }
        public float unitPrice { get; set; }
        public string reason { get; set; }
        public long? dispatchId { get; set; }
        public DateTime? createAt { get; set; }
        public string registeredByUserName { get; set; }
    }
}
