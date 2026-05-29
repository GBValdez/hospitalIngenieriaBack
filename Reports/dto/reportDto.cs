namespace fletesProyect.Reports.dto
{
    public class reportQueryDto
    {
        public DateTime? startDateFrom { get; set; }
        public DateTime? startDateTo { get; set; }
        public int lowStockThreshold { get; set; } = 10;
    }

    public class reportSummaryDto
    {
        public DateTime startDateFrom { get; set; }
        public DateTime startDateTo { get; set; }
        public reportTotalsDto totals { get; set; } = new reportTotalsDto();
        public List<reportCountDto> appointmentsByStatus { get; set; } = new List<reportCountDto>();
        public List<reportCountDto> appointmentsByDoctor { get; set; } = new List<reportCountDto>();
        public List<reportCountDto> examsByStatus { get; set; } = new List<reportCountDto>();
        public List<reportCountDto> examsByType { get; set; } = new List<reportCountDto>();
        public List<reportCountDto> topDiagnoses { get; set; } = new List<reportCountDto>();
        public List<reportCountDto> topPrescribedMedicines { get; set; } = new List<reportCountDto>();
        public List<reportCountDto> topDispatchedMedicines { get; set; } = new List<reportCountDto>();
        public List<lowStockMedicineDto> lowStockMedicines { get; set; } = new List<lowStockMedicineDto>();
    }

    public class reportTotalsDto
    {
        public int appointments { get; set; }
        public int finalizedAppointments { get; set; }
        public int exams { get; set; }
        public int finalizedExams { get; set; }
        public int recipes { get; set; }
        public int dispatchedUnits { get; set; }
        public decimal dispatchRevenue { get; set; }
        public int lowStockMedicines { get; set; }
    }

    public class reportCountDto
    {
        public string name { get; set; }
        public int count { get; set; }
        public decimal amount { get; set; }
    }

    public class lowStockMedicineDto
    {
        public long medicineId { get; set; }
        public string medicineName { get; set; }
        public int stock { get; set; }
        public float price { get; set; }
    }
}
