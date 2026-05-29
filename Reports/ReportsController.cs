using fletesProyect.Reports.dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;

namespace fletesProyect.Reports
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
    public class ReportsController : ControllerBase
    {
        private const string StatusActivo = "ACTIVO";
        private const string StatusFinalizada = "FINALIZADA";
        private readonly DBProyContext context;

        public ReportsController(DBProyContext context)
        {
            this.context = context;
        }

        [HttpGet("resumen")]
        public async Task<ActionResult<reportSummaryDto>> GetSummary([FromQuery] reportQueryDto query)
        {
            DateTime from = query.startDateFrom.HasValue
                ? ToUtc(query.startDateFrom.Value)
                : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime to = query.startDateTo.HasValue
                ? ToUtc(query.startDateTo.Value).Date.AddDays(1)
                : DateTime.UtcNow.Date.AddDays(1);
            int lowStockThreshold = query.lowStockThreshold <= 0 ? 10 : query.lowStockThreshold;

            List<long> appointmentIds = await context.Appointments
                .Where(x => x.deleteAt == null && x.startDate >= from && x.startDate < to)
                .Select(x => x.Id)
                .ToListAsync();

            List<long> examIds = await context.Exams
                .Where(x => x.deleteAt == null && x.startDate >= from && x.startDate < to)
                .Select(x => x.Id)
                .ToListAsync();

            Dictionary<long, string> appointmentStatuses = await GetAppointmentStatuses(appointmentIds);
            Dictionary<long, string> examStatuses = await GetExamStatuses(examIds);
            List<lowStockMedicineDto> lowStockMedicines = await GetLowStockMedicines(lowStockThreshold);

            List<reportCountDto> appointmentStatusReport = appointmentIds
                .Select(id => appointmentStatuses.TryGetValue(id, out string status) ? status : StatusActivo)
                .GroupBy(status => status)
                .Select(group => new reportCountDto { name = group.Key, count = group.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            List<reportCountDto> examStatusReport = examIds
                .Select(id => examStatuses.TryGetValue(id, out string status) ? status : StatusActivo)
                .GroupBy(status => status)
                .Select(group => new reportCountDto { name = group.Key, count = group.Count() })
                .OrderByDescending(x => x.count)
                .ToList();

            List<reportCountDto> appointmentDoctorReport = await context.Appointments
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id))
                .GroupBy(x => x.doctor.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            List<reportCountDto> examTypeReport = await context.Exams
                .Include(x => x.examType)
                .Where(x => examIds.Contains(x.Id))
                .GroupBy(x => x.examType.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            List<reportCountDto> topDiagnoses = await GetTopDiagnoses(appointmentIds, examIds);
            List<reportCountDto> topPrescribedMedicines = await GetTopPrescribedMedicines(from, to);
            List<reportCountDto> topDispatchedMedicines = await GetTopDispatchedMedicines(from, to);

            int recipes = await context.Recipes
                .Where(x => x.deleteAt == null
                    && x.appointment.startDate >= from
                    && x.appointment.startDate < to)
                .CountAsync();

            List<dispatchReportItem> dispatchItems = await context.Dispatchs
                .Include(x => x.recipe)
                    .ThenInclude(x => x.medicine)
                .Where(x => x.deleteAt == null && x.createAt >= from && x.createAt < to)
                .Select(x => new dispatchReportItem
                {
                    amount = x.amount,
                    unitPrice = x.recipe.medicine.price
                })
                .ToListAsync();

            return new reportSummaryDto
            {
                startDateFrom = from,
                startDateTo = to.AddDays(-1),
                totals = new reportTotalsDto
                {
                    appointments = appointmentIds.Count,
                    finalizedAppointments = appointmentStatusReport
                        .Where(x => x.name == StatusFinalizada)
                        .Select(x => x.count)
                        .FirstOrDefault(),
                    exams = examIds.Count,
                    finalizedExams = examStatusReport
                        .Where(x => x.name == StatusFinalizada)
                        .Select(x => x.count)
                        .FirstOrDefault(),
                    recipes = recipes,
                    dispatchedUnits = dispatchItems.Sum(x => x.amount),
                    dispatchRevenue = dispatchItems.Sum(x => (decimal)x.amount * (decimal)x.unitPrice),
                    lowStockMedicines = lowStockMedicines.Count
                },
                appointmentsByStatus = appointmentStatusReport,
                appointmentsByDoctor = appointmentDoctorReport,
                examsByStatus = examStatusReport,
                examsByType = examTypeReport,
                topDiagnoses = topDiagnoses,
                topPrescribedMedicines = topPrescribedMedicines,
                topDispatchedMedicines = topDispatchedMedicines,
                lowStockMedicines = lowStockMedicines
            };
        }

        private async Task<Dictionary<long, string>> GetAppointmentStatuses(List<long> appointmentIds)
        {
            if (appointmentIds.Count == 0)
                return new Dictionary<long, string>();

            return await context.AppointmentStatusHistories
                .Include(x => x.status)
                .Where(x => appointmentIds.Contains(x.appointmentId) && x.deleteAt == null)
                .OrderByDescending(x => x.changedAt)
                .ToListAsync()
                .ContinueWith(task => task.Result
                    .GroupBy(x => x.appointmentId)
                    .ToDictionary(x => x.Key, x => x.First().status.name));
        }

        private async Task<Dictionary<long, string>> GetExamStatuses(List<long> examIds)
        {
            if (examIds.Count == 0)
                return new Dictionary<long, string>();

            return await context.ExamStatusHistories
                .Include(x => x.status)
                .Where(x => examIds.Contains(x.examId) && x.deleteAt == null)
                .OrderByDescending(x => x.changedAt)
                .ToListAsync()
                .ContinueWith(task => task.Result
                    .GroupBy(x => x.examId)
                    .ToDictionary(x => x.Key, x => x.First().status.name));
        }

        private async Task<List<reportCountDto>> GetTopDiagnoses(List<long> appointmentIds, List<long> examIds)
        {
            List<string> appointmentDiagnoses = await context.AppointmentDiseaseOrInjuries
                .Include(x => x.diseaseOrInjury)
                .Where(x => appointmentIds.Contains(x.appointmentId)
                    && x.deleteAt == null
                    && x.diseaseOrInjury.deleteAt == null)
                .Select(x => x.diseaseOrInjury.name)
                .ToListAsync();

            List<string> examDiagnoses = await context.ExamDiseaseOrInjuries
                .Include(x => x.diseaseOrInjury)
                .Where(x => examIds.Contains(x.examId)
                    && x.deleteAt == null
                    && x.diseaseOrInjury.deleteAt == null)
                .Select(x => x.diseaseOrInjury.name)
                .ToListAsync();

            return appointmentDiagnoses
                .Concat(examDiagnoses)
                .GroupBy(x => x)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();
        }

        private async Task<List<reportCountDto>> GetTopPrescribedMedicines(DateTime from, DateTime to)
        {
            return await context.Recipes
                .Include(x => x.medicine)
                .Where(x => x.deleteAt == null
                    && x.appointment.startDate >= from
                    && x.appointment.startDate < to
                    && x.medicine.deleteAt == null)
                .GroupBy(x => x.medicine.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetTopDispatchedMedicines(DateTime from, DateTime to)
        {
            List<dispatchMedicineReportItem> items = await context.Dispatchs
                .Include(x => x.recipe)
                    .ThenInclude(x => x.medicine)
                .Where(x => x.deleteAt == null
                    && x.createAt >= from
                    && x.createAt < to
                    && x.recipe.medicine.deleteAt == null)
                .Select(x => new dispatchMedicineReportItem
                {
                    medicineName = x.recipe.medicine.name,
                    amount = x.amount,
                    unitPrice = x.recipe.medicine.price
                })
                .ToListAsync();

            return items
                .GroupBy(x => x.medicineName)
                .Select(x => new reportCountDto
                {
                    name = x.Key,
                    count = x.Sum(item => item.amount),
                    amount = x.Sum(item => (decimal)item.amount * (decimal)item.unitPrice)
                })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToList();
        }

        private async Task<List<lowStockMedicineDto>> GetLowStockMedicines(int threshold)
        {
            return await context.Medicines
                .Where(x => x.deleteAt == null)
                .GroupJoin(
                    context.MedicineInventories,
                    medicine => medicine.Id,
                    inventory => inventory.medicineId,
                    (medicine, inventory) => new lowStockMedicineDto
                    {
                        medicineId = medicine.Id,
                        medicineName = medicine.name,
                        price = medicine.price,
                        stock = inventory.Select(x => x.stock).FirstOrDefault()
                    })
                .Where(x => x.stock <= threshold)
                .OrderBy(x => x.stock)
                .ThenBy(x => x.medicineName)
                .Take(20)
                .ToListAsync();
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }

        private class dispatchReportItem
        {
            public int amount { get; set; }
            public float unitPrice { get; set; }
        }

        private class dispatchMedicineReportItem : dispatchReportItem
        {
            public string medicineName { get; set; }
        }
    }
}
