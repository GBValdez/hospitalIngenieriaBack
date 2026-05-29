using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using fletesProyect.ExamStatusHistory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Exams.dto;
using project.Models;
using project.utils.dto;
using ExamModel = fletesProyect.Exam.Exam;

namespace project.Exams
{
    [ApiController]
    [Route("api/examenes")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "userNormal,DOCTOR,NURSE,ADMINISTRATOR,LAB_ATTENDANT")]
    public class examsController : ControllerBase
    {
        private const string StatusActivo = "ACTIVO";
        private const string StatusEnCurso = "EN_CURSO";
        private const string StatusFinalizada = "FINALIZADA";
        private readonly DBProyContext context;

        public examsController(DBProyContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<resPag<examDto>>> Get(
            [FromQuery] pagQueryDto page,
            [FromQuery] examQueryDto query)
        {
            IQueryable<ExamModel> exams = context.Exams
                .Include(x => x.examType)
                .Include(x => x.attendant)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.patient)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.doctor)
                .Where(x => x.deleteAt == null);

            if (!User.IsInRole("ADMINISTRATOR") && !User.IsInRole("NURSE"))
            {
                if (User.IsInRole("userNormal"))
                {
                    int patientId = GetClaimInt("patientId");
                    exams = exams.Where(x => x.appointment.patientId == patientId);
                }
                else if (User.IsInRole("DOCTOR"))
                {
                    int doctorId = GetClaimInt("workerId");
                    exams = exams.Where(x => x.appointment.doctorId == doctorId);
                }
                else if (User.IsInRole("LAB_ATTENDANT"))
                {
                    int attendantId = GetClaimInt("workerId");
                    exams = exams.Where(x => x.attendantId == attendantId);
                }
            }

            if (query?.appointmentId != null)
                exams = exams.Where(x => x.appointmentId == query.appointmentId.Value);

            if (query?.examTypeId != null)
                exams = exams.Where(x => x.examTypeId == query.examTypeId.Value);

            if (query?.attendantId != null)
                exams = exams.Where(x => x.attendantId == query.attendantId.Value);

            if (query?.patientId != null)
                exams = exams.Where(x => x.appointment.patientId == query.patientId.Value);

            if (query?.doctorId != null)
                exams = exams.Where(x => x.appointment.doctorId == query.doctorId.Value);

            int total = await exams.CountAsync();
            int pageSize = page.pageSize <= 0 ? 10 : page.pageSize;
            int pageNumber = page.pageNumber <= 0 ? 1 : page.pageNumber;
            int totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);

            if (page.all != true)
                exams = exams.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<examDto> items = await exams
                .OrderByDescending(x => x.startDate)
                .Select(x => new examDto
                {
                    id = x.Id,
                    startDate = x.startDate,
                    endDate = x.endDate,
                    results = x.results,
                    observations = x.observations,
                    examTypeId = x.examTypeId,
                    examTypeName = x.examType.name,
                    appointmentId = x.appointmentId,
                    appointmentReason = x.appointment.reason,
                    attendantId = x.attendantId,
                    attendantName = x.attendant.name,
                    doctorId = x.appointment.doctorId,
                    doctorName = x.appointment.doctor.name,
                    patientId = x.appointment.patientId,
                    patientName = x.appointment.patient.name,
                    status = StatusActivo
                })
                .ToListAsync();

            List<long> examIds = items.Select(x => x.id).ToList();
            Dictionary<long, string> statuses = await context.ExamStatusHistories
                .Include(x => x.status)
                .Where(x => examIds.Contains(x.examId) && x.deleteAt == null)
                .OrderByDescending(x => x.changedAt)
                .GroupBy(x => x.examId)
                .Select(x => new { examId = x.Key, status = x.First().status.name })
                .ToDictionaryAsync(x => x.examId, x => x.status);

            foreach (examDto item in items)
                item.status = statuses.TryGetValue(item.id, out string status)
                    ? status
                    : StatusActivo;

            return new resPag<examDto>
            {
                items = items,
                total = total,
                index = pageNumber,
                totalPages = totalPages
            };
        }

        [HttpPost("inicio")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "LAB_ATTENDANT,ADMINISTRATOR")]
        public async Task<ActionResult<examDto>> IniciarExamen([FromBody] inicioExamDto dto)
        {
            if (dto == null)
                return BadRequest(new errorMessageDto("Debe enviar la informacion de inicio del examen."));

            ExamModel exam = await GetExam(dto.examId);
            if (exam == null)
                return NotFound();

            if (!CanManageExam(exam))
                return Forbid();

            DateTime now = DateTime.UtcNow;
            DateTime startDate = ToUtc(exam.startDate);
            DateTime maxStartDate = startDate.AddMinutes(10);
            if (now < startDate || now > maxStartDate)
                return BadRequest(new errorMessageDto("El examen solo puede iniciarse desde la hora programada hasta 10 minutos despues."));

            string currentStatus = await GetLastExamStatus(exam.Id);
            if (currentStatus != StatusActivo)
                return BadRequest(new errorMessageDto("Solo se pueden iniciar examenes con estado ACTIVO."));

            await AddExamStatusHistory(exam.Id, currentStatus, StatusEnCurso, $"Inicio de examen registrado el {now:yyyy-MM-dd HH:mm} UTC.");
            await context.SaveChangesAsync();

            return MapExam(exam, StatusEnCurso);
        }

        [HttpPost("finalizar")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "LAB_ATTENDANT,ADMINISTRATOR")]
        public async Task<ActionResult<examDto>> FinalizarExamen([FromBody] finalizarExamDto dto)
        {
            if (dto == null)
                return BadRequest(new errorMessageDto("Debe enviar la informacion para finalizar el examen."));

            ExamModel exam = await GetExam(dto.examId);
            if (exam == null)
                return NotFound();

            if (!CanManageExam(exam))
                return Forbid();

            string currentStatus = await GetLastExamStatus(exam.Id);
            if (currentStatus != StatusEnCurso)
                return BadRequest(new errorMessageDto("Solo se pueden finalizar examenes con estado EN_CURSO."));

            if (string.IsNullOrWhiteSpace(dto.results))
                return BadRequest(new errorMessageDto("El resultado del examen es obligatorio."));

            if (dto.results.Trim().Length > 1000)
                return BadRequest(new errorMessageDto("El resultado del examen no puede superar 1000 caracteres."));

            exam.results = dto.results.Trim();
            await AddExamStatusHistory(exam.Id, currentStatus, StatusFinalizada, "Examen finalizado.");
            await context.SaveChangesAsync();

            return MapExam(exam, StatusFinalizada);
        }

        [HttpGet("{id}/historial-estados")]
        public async Task<ActionResult<List<examStatusHistoryDto>>> GetHistorialEstados(long id)
        {
            ExamModel exam = await GetExam(id);
            if (exam == null)
                return NotFound();

            if (!CanSeeExam(exam))
                return Forbid();

            return await context.ExamStatusHistories
                .Include(x => x.previousStatus)
                .Include(x => x.status)
                .Include(x => x.changedByUser)
                .Where(x => x.examId == id && x.deleteAt == null)
                .OrderBy(x => x.changedAt)
                .Select(x => new examStatusHistoryDto
                {
                    id = x.Id,
                    examId = x.examId,
                    previousStatus = x.previousStatus != null ? x.previousStatus.name : null,
                    status = x.status.name,
                    comment = x.comment,
                    changedAt = x.changedAt,
                    changedByUserId = x.changedByUserId,
                    changedByUserName = x.changedByUser != null ? x.changedByUser.UserName : null
                })
                .ToListAsync();
        }

        private int GetClaimInt(string claimType)
        {
            string value = User.FindFirstValue(claimType);
            return int.TryParse(value, out int parsedValue) ? parsedValue : 0;
        }

        private async Task<ExamModel> GetExam(long id)
        {
            return await context.Exams
                .Include(x => x.examType)
                .Include(x => x.attendant)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.patient)
                .Include(x => x.appointment)
                    .ThenInclude(x => x.doctor)
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);
        }

        private bool CanManageExam(ExamModel exam)
        {
            return User.IsInRole("ADMINISTRATOR")
                || (User.IsInRole("LAB_ATTENDANT") && exam.attendantId == GetClaimInt("workerId"));
        }

        private bool CanSeeExam(ExamModel exam)
        {
            if (User.IsInRole("ADMINISTRATOR") || User.IsInRole("NURSE"))
                return true;

            if (User.IsInRole("LAB_ATTENDANT"))
                return exam.attendantId == GetClaimInt("workerId");

            if (User.IsInRole("DOCTOR"))
                return exam.appointment.doctorId == GetClaimInt("workerId");

            if (User.IsInRole("userNormal"))
                return exam.appointment.patientId == GetClaimInt("patientId");

            return false;
        }

        private examDto MapExam(ExamModel exam, string status)
        {
            return new examDto
            {
                id = exam.Id,
                startDate = exam.startDate,
                endDate = exam.endDate,
                results = exam.results,
                observations = exam.observations,
                examTypeId = exam.examTypeId,
                examTypeName = exam.examType?.name,
                appointmentId = exam.appointmentId,
                appointmentReason = exam.appointment?.reason,
                attendantId = exam.attendantId,
                attendantName = exam.attendant?.name,
                doctorId = exam.appointment?.doctorId ?? 0,
                doctorName = exam.appointment?.doctor?.name,
                patientId = exam.appointment?.patientId ?? 0,
                patientName = exam.appointment?.patient?.name,
                status = status
            };
        }

        private async Task<string> GetLastExamStatus(long examId)
        {
            string status = await context.ExamStatusHistories
                .Where(x => x.examId == examId && x.deleteAt == null)
                .OrderByDescending(x => x.changedAt)
                .Select(x => x.status.name)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(status) ? StatusActivo : status;
        }

        private async Task AddExamStatusHistory(long examId, string? previousStatus, string status, string comment)
        {
            DateTime now = DateTime.UtcNow;
            long? previousStatusId = string.IsNullOrWhiteSpace(previousStatus)
                ? null
                : await GetAppointmentStatusId(previousStatus);
            long statusId = await GetAppointmentStatusId(status);

            context.ExamStatusHistories.Add(new ExamStatusHistory
            {
                examId = examId,
                previousStatusId = previousStatusId,
                statusId = statusId,
                comment = comment,
                changedAt = now,
                changedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                createAt = now
            });
        }

        private async Task<long> GetAppointmentStatusId(string status)
        {
            string normalizedStatus = status.Trim().ToUpper();
            long statusId = await context.AppointmentStatuses
                .Where(x => x.name == normalizedStatus && x.deleteAt == null)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (statusId != 0)
                return statusId;

            fletesProyect.catalogues.AppointmentStatus newStatus = new fletesProyect.catalogues.AppointmentStatus
            {
                name = normalizedStatus,
                description = normalizedStatus,
                createAt = DateTime.UtcNow
            };
            context.AppointmentStatuses.Add(newStatus);
            await context.SaveChangesAsync();
            return newStatus.Id;
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }
    }
}
