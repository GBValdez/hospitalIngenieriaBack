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

            if (from.Date > DateTime.UtcNow.Date || to.Date.AddDays(-1) > DateTime.UtcNow.Date)
            {
                return BadRequest(new { error = "Solo se pueden consultar fechas desde la fecha actual hacia atras." });
            }

            if (from >= to)
            {
                return BadRequest(new { error = "La fecha desde debe ser menor o igual a la fecha hasta." });
            }

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
            doctorReportsDto doctorReports = await GetDoctorReports(appointmentIds, from, to, appointmentStatuses);

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
                lowStockMedicines = lowStockMedicines,
                doctorReports = doctorReports
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

        private async Task<doctorReportsDto> GetDoctorReports(
            List<long> appointmentIds,
            DateTime from,
            DateTime to,
            Dictionary<long, string> appointmentStatuses)
        {
            List<long> finalizedAppointmentIds = appointmentStatuses
                .Where(x => x.Value == StatusFinalizada)
                .Select(x => x.Key)
                .ToList();

            return new doctorReportsDto
            {
                patientsAttendedByDoctor = await GetPatientsAttendedByDoctor(appointmentIds),
                patientAttendanceDetails = await GetPatientAttendanceDetails(appointmentIds),
                appointmentsByDoctor = await GetAppointmentsByDoctor(appointmentIds),
                finalizedAppointmentsByDoctor = await GetFinalizedAppointmentsByDoctor(finalizedAppointmentIds),
                emergencyAppointmentsByDoctor = await GetEmergencyAppointmentsByDoctor(appointmentIds),
                averageAttentionMinutesByDoctor = await GetAverageAttentionMinutesByDoctor(appointmentIds),
                recipesByDoctor = await GetRecipesByDoctor(from, to),
                prescribedMedicinesByDoctor = await GetPrescribedMedicinesByDoctor(from, to),
                dispatchedMedicinesByDoctor = await GetDispatchedMedicinesByDoctor(from, to),
                diagnosesByDoctor = await GetDiagnosesByDoctor(appointmentIds),
                appointmentsBySpecialty = await GetAppointmentsBySpecialty(appointmentIds)
            };
        }

        private async Task<List<reportCountDto>> GetPatientsAttendedByDoctor(List<long> appointmentIds)
        {
            return await context.Appointments
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id) && x.doctor != null)
                .GroupBy(x => x.doctor.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Select(item => item.patientId).Distinct().Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<doctorPatientAttendanceDetailDto>> GetPatientAttendanceDetails(List<long> appointmentIds)
        {
            List<patientAttendanceReportItem> items = await context.Appointments
                .Include(x => x.patient)
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id)
                    && x.patient != null
                    && x.doctor != null)
                .Select(x => new patientAttendanceReportItem
                {
                    patientId = x.patient.Id,
                    patientName = x.patient.name,
                    patientDpi = x.patient.dpi,
                    doctorName = x.doctor.name,
                    startDate = x.startDate
                })
                .ToListAsync();

            return items
                .GroupBy(x => new { x.patientId, x.patientName, x.patientDpi })
                .Select(x =>
                {
                    List<string> doctorNames = x
                        .Select(item => item.doctorName)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList();

                    return new doctorPatientAttendanceDetailDto
                    {
                        patientId = x.Key.patientId,
                        patientName = x.Key.patientName,
                        patientDpi = x.Key.patientDpi,
                        appointmentCount = x.Count(),
                        doctorCount = doctorNames.Count,
                        attendedByMultipleDoctors = doctorNames.Count > 1,
                        doctorNames = doctorNames
                    };
                })
                .OrderByDescending(x => x.attendedByMultipleDoctors)
                .ThenByDescending(x => x.doctorCount)
                .ThenBy(x => x.patientName)
                .ToList();
        }

        private async Task<List<reportCountDto>> GetAppointmentsByDoctor(List<long> appointmentIds)
        {
            return await context.Appointments
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id) && x.doctor != null)
                .GroupBy(x => x.doctor.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetFinalizedAppointmentsByDoctor(List<long> finalizedAppointmentIds)
        {
            return await context.Appointments
                .Include(x => x.doctor)
                .Where(x => finalizedAppointmentIds.Contains(x.Id) && x.doctor != null)
                .GroupBy(x => x.doctor.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetEmergencyAppointmentsByDoctor(List<long> appointmentIds)
        {
            return await context.Appointments
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id) && x.doctor != null && x.isEmergency)
                .GroupBy(x => x.doctor.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetAverageAttentionMinutesByDoctor(List<long> appointmentIds)
        {
            List<appointmentDatesReportItem> appointmentDates = await context.Appointments
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id) && x.doctor != null && x.endDate != null)
                .Select(x => new appointmentDatesReportItem
                {
                    doctorName = x.doctor.name,
                    startDate = x.startDate,
                    endDate = x.endDate.Value
                })
                .ToListAsync();

            List<appointmentDurationReportItem> items = appointmentDates
                .Select(x => new appointmentDurationReportItem
                {
                    doctorName = x.doctorName,
                    minutes = (decimal)(x.endDate - x.startDate).TotalMinutes
                })
                .ToList();

            return items
                .GroupBy(x => x.doctorName)
                .Select(x => new reportCountDto
                {
                    name = x.Key,
                    count = x.Count(),
                    amount = Math.Round(x.Average(item => item.minutes), 2)
                })
                .OrderByDescending(x => x.amount)
                .ThenBy(x => x.name)
                .Take(10)
                .ToList();
        }

        private async Task<List<reportCountDto>> GetRecipesByDoctor(DateTime from, DateTime to)
        {
            return await context.Recipes
                .Include(x => x.appointment)
                    .ThenInclude(x => x.doctor)
                .Where(x => x.deleteAt == null
                    && x.appointment.deleteAt == null
                    && x.appointment.doctor != null
                    && x.appointment.startDate >= from
                    && x.appointment.startDate < to)
                .GroupBy(x => x.appointment.doctor.name)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetPrescribedMedicinesByDoctor(DateTime from, DateTime to)
        {
            return await context.Recipes
                .Include(x => x.medicine)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.doctor)
                .Where(x => x.deleteAt == null
                    && x.medicine.deleteAt == null
                    && x.appointment.deleteAt == null
                    && x.appointment.doctor != null
                    && x.appointment.startDate >= from
                    && x.appointment.startDate < to)
                .GroupBy(x => new { DoctorName = x.appointment.doctor.name, MedicineName = x.medicine.name })
                .Select(x => new reportCountDto { name = x.Key.DoctorName + " - " + x.Key.MedicineName, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetDispatchedMedicinesByDoctor(DateTime from, DateTime to)
        {
            List<doctorDispatchReportItem> items = await context.Dispatchs
                .Include(x => x.recipe)
                    .ThenInclude(x => x.medicine)
                .Include(x => x.recipe)
                    .ThenInclude(x => x.appointment)
                        .ThenInclude(x => x.doctor)
                .Where(x => x.deleteAt == null
                    && x.createAt >= from
                    && x.createAt < to
                    && x.recipe.deleteAt == null
                    && x.recipe.medicine.deleteAt == null
                    && x.recipe.appointment.doctor != null)
                .Select(x => new doctorDispatchReportItem
                {
                    doctorName = x.recipe.appointment.doctor.name,
                    medicineName = x.recipe.medicine.name,
                    amount = x.amount,
                    unitPrice = x.recipe.medicine.price
                })
                .ToListAsync();

            return items
                .GroupBy(x => new { x.doctorName, x.medicineName })
                .Select(x => new reportCountDto
                {
                    name = x.Key.doctorName + " - " + x.Key.medicineName,
                    count = x.Sum(item => item.amount),
                    amount = x.Sum(item => (decimal)item.amount * (decimal)item.unitPrice)
                })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToList();
        }

        private async Task<List<reportCountDto>> GetDiagnosesByDoctor(List<long> appointmentIds)
        {
            return await context.AppointmentDiseaseOrInjuries
                .Include(x => x.appointment)
                    .ThenInclude(x => x.doctor)
                .Include(x => x.diseaseOrInjury)
                .Where(x => appointmentIds.Contains(x.appointmentId)
                    && x.deleteAt == null
                    && x.appointment.doctor != null
                    && x.diseaseOrInjury.deleteAt == null)
                .GroupBy(x => new { DoctorName = x.appointment.doctor.name, DiagnosisName = x.diseaseOrInjury.name })
                .Select(x => new reportCountDto { name = x.Key.DoctorName + " - " + x.Key.DiagnosisName, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
                .ToListAsync();
        }

        private async Task<List<reportCountDto>> GetAppointmentsBySpecialty(List<long> appointmentIds)
        {
            return await context.Appointments
                .Include(x => x.doctor)
                .Where(x => appointmentIds.Contains(x.Id) && x.doctor != null)
                .Join(
                    context.doctorSpecialties.Include(x => x.specialty).Where(x => x.deleteAt == null && x.specialty.deleteAt == null),
                    appointment => appointment.doctor.Id,
                    specialty => specialty.doctorId,
                    (appointment, specialty) => specialty.specialty.name)
                .GroupBy(x => x)
                .Select(x => new reportCountDto { name = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count)
                .ThenBy(x => x.name)
                .Take(10)
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

        private class appointmentDurationReportItem
        {
            public string doctorName { get; set; }
            public decimal minutes { get; set; }
        }

        private class appointmentDatesReportItem
        {
            public string doctorName { get; set; }
            public DateTime startDate { get; set; }
            public DateTime endDate { get; set; }
        }

        private class patientAttendanceReportItem
        {
            public long patientId { get; set; }
            public string patientName { get; set; }
            public string patientDpi { get; set; }
            public string doctorName { get; set; }
            public DateTime startDate { get; set; }
        }

        private class doctorDispatchReportItem : dispatchMedicineReportItem
        {
            public string doctorName { get; set; }
        }
    }
}
