using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using fletesProyect.AppointmentStatusHistory;
using fletesProyect.ExamStatusHistory;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils;
using project.utils.dto;
using project.Appointment.dto;
using AppointmentModel = fletesProyect.Appointment.Appointment;
using ExamModel = fletesProyect.Exam.Exam;
using RecipeModel = fletesProyect.Recipe.Recipe;

namespace project.Appointment
{
    [ApiController]
    [Route("api/citas")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "userNormal,DOCTOR,NURSE,ADMINISTRATOR")]
    public class citasController : controllerCommons<AppointmentModel, citaAgendarDto, citaDto, citaQueryDto, object, long>
    {
        private const string StatusActivo = "ACTIVO";
        private const string StatusReagendar = "REAGENDAR";
        private const string StatusCancelar = "CANCELAR";
        private const string StatusEnCurso = "EN_CURSO";
        private const string StatusFinalizada = "FINALIZADA";
        protected override bool showDeleted { get; set; } = true;

        public citasController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override async Task<IQueryable<AppointmentModel>> modifyGet(IQueryable<AppointmentModel> query, citaQueryDto queryParams)
        {
            query = query
                .Include(x => x.doctor)
                .Include(x => x.patient);

            if (queryParams == null)
                return query;

            if (!User.IsInRole("ADMINISTRATOR"))
            {
                if (User.IsInRole("userNormal"))
                {
                    int patientId = GetClaimInt("patientId");
                    query = query.Where(x => x.patientId == patientId);
                }
                else if (User.IsInRole("DOCTOR"))
                {
                    int doctorId = GetClaimInt("workerId");
                    query = query.Where(x => x.doctorId == doctorId);
                }
            }

            if (queryParams.doctorId.HasValue)
                query = query.Where(x => x.doctorId == queryParams.doctorId.Value);

            if (queryParams.patientId.HasValue)
                query = query.Where(x => x.patientId == queryParams.patientId.Value);

            if (!string.IsNullOrWhiteSpace(queryParams.estado))
            {
                string estado = queryParams.estado.Trim().ToLower();
                if (estado == "activo")
                    query = query.Where(x => x.deleteAt == null);
                else if (estado == "cancelado")
                    query = query.Where(x => x.deleteAt != null);
            }

            return query;
        }

        protected override void modifyGetResult(List<AppointmentModel> list)
        {
            if (list == null || list.Count == 0)
                return;

            List<long> appointmentIds = list.Select(x => x.Id).ToList();
            Dictionary<long, string> statuses = context.AppointmentStatusHistories
                .Include(x => x.status)
                .Where(x => appointmentIds.Contains(x.appointmentId) && x.deleteAt == null)
                .OrderByDescending(x => x.changedAt)
                .ToList()
                .GroupBy(x => x.appointmentId)
                .ToDictionary(x => x.Key, x => x.First().status.name);

            foreach (AppointmentModel appointment in list)
                appointment.currentStatus = statuses.TryGetValue(appointment.Id, out string status)
                    ? status
                    : appointment.deleteAt == null ? StatusActivo : StatusCancelar;
        }

        protected override async Task<errorMessageDto> validPost(AppointmentModel entity, citaAgendarDto newRegister, object queryParams)
        {
            if (newRegister == null)
                return new errorMessageDto("Debe enviar la informacion de la cita.");

            if (User.IsInRole("NURSE") && !User.IsInRole("ADMINISTRATOR"))
                return new errorMessageDto("La enfermera solo puede registrar el inicio de la cita.");

            if (string.IsNullOrWhiteSpace(newRegister.reason))
                return new errorMessageDto("El motivo de la cita es obligatorio.");

            if (newRegister.reason.Trim().Length > 250)
                return new errorMessageDto("El motivo de la cita no puede superar 250 caracteres.");

            DateTime startDate = ToUtc(newRegister.startDate);
            DateTime endDate = ToUtc(newRegister.endDate);

            if (startDate <= DateTime.UtcNow)
                return new errorMessageDto("La fecha y hora de la cita no pueden ser anteriores a la fecha actual.");

            if (endDate <= startDate)
                return new errorMessageDto("La fecha final debe ser posterior a la fecha de inicio.");

            bool patientExists = await context.Patients
                .AnyAsync(x => x.Id == newRegister.patientId && x.deleteAt == null);
            if (!patientExists)
                return new errorMessageDto("No se encontro el paciente asociado a la cita.");

            if (User.IsInRole("userNormal") && GetClaimInt("patientId") != newRegister.patientId)
                return new errorMessageDto("No puedes agendar citas para otro paciente.");

            if (await HasPatientAppointmentConflict(newRegister.patientId, startDate, endDate))
                return new errorMessageDto("El paciente ya tiene una cita en el horario seleccionado.");

            long doctorId = await ObtenerDoctorDisponible(startDate, endDate, newRegister.doctorId);
            if (doctorId == 0)
                return new errorMessageDto("No hay medicos disponibles para el horario seleccionado.");

            entity.reason = newRegister.reason.Trim();
            entity.doctorId = (int)doctorId;
            entity.startDate = startDate;
            entity.endDate = endDate;
            entity.scheduledDate = startDate;
            context.Entry(entity).Property("doctorId1").CurrentValue = doctorId;
            context.Entry(entity).Property("patientId1").CurrentValue = (long)newRegister.patientId;

            return null;
        }

        protected override async Task finallyPost(AppointmentModel entity, citaAgendarDto dtoCreation, object queryParams)
        {
            await AddStatusHistory(entity.Id, null, StatusActivo, "Cita agendada.");
            await context.SaveChangesAsync();
        }

        [HttpGet("{id}/historial-estados")]
        public async Task<ActionResult<List<citaStatusHistoryDto>>> GetHistorialEstados(long id)
        {
            AppointmentModel appointment = await context.Set<AppointmentModel>()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (appointment == null)
                return NotFound();

            if (!CanManageAppointment(appointment))
                return Forbid();

            return await context.AppointmentStatusHistories
                .Include(x => x.changedByUser)
                .Include(x => x.previousStatus)
                .Include(x => x.status)
                .Where(x => x.appointmentId == id && x.deleteAt == null)
                .OrderBy(x => x.changedAt)
                .Select(x => new citaStatusHistoryDto
                {
                    id = x.Id,
                    appointmentId = x.appointmentId,
                    previousStatusId = x.previousStatusId,
                    previousStatus = x.previousStatus == null ? null : x.previousStatus.name,
                    statusId = x.statusId,
                    status = x.status.name,
                    comment = x.comment,
                    changedAt = x.changedAt,
                    changedByUserId = x.changedByUserId,
                    changedByUserName = x.changedByUser == null ? null : x.changedByUser.UserName
                })
                .ToListAsync();
        }

        [HttpGet("disponibilidad")]
        public async Task<ActionResult<object>> ValidarDisponibilidad(
            [FromQuery] DateTime fechaHora,
            [FromQuery] int doctorId = 0,
            [FromQuery] int patientId = 0,
            [FromQuery] long? excludeAppointmentId = null)
        {
            DateTime fechaInicio = ToUtc(fechaHora);

            if (fechaInicio <= DateTime.UtcNow)
                return BadRequest(new errorMessageDto("La fecha y hora no pueden ser anteriores a la fecha actual."));

            DateTime fechaFin = fechaInicio.AddMinutes(30);
            if (patientId == 0 && User.IsInRole("userNormal"))
                patientId = GetClaimInt("patientId");

            bool patientAvailable = patientId == 0
                || !await HasPatientAppointmentConflict(patientId, fechaInicio, fechaFin, excludeAppointmentId);
            long availableDoctorId = patientAvailable
                ? await ObtenerDoctorDisponible(fechaInicio, fechaFin, doctorId, excludeAppointmentId)
                : 0;
            List<AppointmentAvailabilitySuggestion> suggestions = availableDoctorId == 0
                ? await GetAppointmentAvailabilitySuggestions(fechaInicio, doctorId, patientId, excludeAppointmentId)
                : new List<AppointmentAvailabilitySuggestion>();

            return Ok(new
            {
                disponible = availableDoctorId != 0,
                doctorId = availableDoctorId == 0 ? (long?)null : availableDoctorId,
                recomendaciones = suggestions
            });
        }

        [HttpPost("cancelar")]
        public async Task<ActionResult> CancelarCita([FromBody] idDto dto)
        {
            if (dto == null || !dto.Id.HasValue)
                return BadRequest(new errorMessageDto("Debe enviar el id de la cita a cancelar."));

            AppointmentModel appointment = await context.Set<AppointmentModel>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.deleteAt == null);

            if (appointment == null)
                return NotFound();

            if (!CanManageAppointment(appointment))
                return Forbid();

            string previousStatus = await GetLastAppointmentStatus(appointment.Id);
            if (!CanCancelStatus(previousStatus))
                return BadRequest(new errorMessageDto("Solo se pueden cancelar citas con estado ACTIVO o REAGENDAR."));

            appointment.deleteAt = DateTime.UtcNow;
            await AddStatusHistory(appointment.Id, previousStatus, StatusCancelar, "Cita cancelada.");
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("reagendar")]
        public async Task<ActionResult> ReagendarCita([FromBody] reagendarDto dto)
        {
            if (dto == null || !dto.citaId.HasValue)
                return BadRequest(new errorMessageDto("Debe enviar el id de la cita a reagendar."));

            DateTime newStartDate = ToUtc(dto.newStartDate);
            DateTime validationEndDate = dto.newEndDate.HasValue
                ? ToUtc(dto.newEndDate.Value)
                : newStartDate.AddMinutes(30);

            if (validationEndDate <= newStartDate)
                return BadRequest(new errorMessageDto("La fecha final debe ser posterior a la fecha de inicio."));

            AppointmentModel appointment = await context.Set<AppointmentModel>()
                .FirstOrDefaultAsync(x => x.Id == dto.citaId.Value);

            if (appointment == null)
                return NotFound();

            if (!CanManageAppointment(appointment))
                return Forbid();

            if (appointment.deleteAt != null)
                return BadRequest(new errorMessageDto("No se puede reagendar una cita cancelada."));

            if (newStartDate <= appointment.startDate)
                return BadRequest(new errorMessageDto("La nueva fecha debe ser mayor a la fecha de la cita actual."));

            if (await HasPatientAppointmentConflict(appointment.patientId, newStartDate, validationEndDate, appointment.Id))
                return BadRequest(new errorMessageDto("El paciente ya tiene una cita en el horario seleccionado."));

            bool conflict = await ObtenerDoctorDisponible(
                newStartDate,
                validationEndDate,
                appointment.doctorId,
                appointment.Id) == 0;

            if (conflict)
                return BadRequest(new errorMessageDto("El médico ya tiene otra cita en el horario seleccionado."));

            appointment.startDate = newStartDate;
            appointment.endDate = null;
            appointment.scheduledDate = newStartDate;
            await AddStatusHistory(appointment.Id, await GetLastAppointmentStatus(appointment.Id), StatusReagendar, "Cita reagendada.");
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("inicio")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "NURSE,ADMINISTRATOR")]
        public async Task<ActionResult<citaDto>> IniciarCita([FromBody] inicioCitaDto dto)
        {
            if (dto == null)
                return BadRequest(new errorMessageDto("Debe enviar la informacion de inicio de cita."));

            AppointmentModel appointment = await context.Set<AppointmentModel>()
                .Include(x => x.doctor)
                .Include(x => x.patient)
                .FirstOrDefaultAsync(x => x.Id == dto.appointmentId && x.deleteAt == null);

            if (appointment == null)
                return NotFound();

            errorMessageDto error = await ValidateInicioCita(dto, appointment);
            if (error != null)
                return BadRequest(error);

            appointment.arrivalDate = DateTime.UtcNow;
            appointment.bloodPressure = dto.bloodPressure.ToString(System.Globalization.CultureInfo.InvariantCulture);
            appointment.temperature = dto.temperature;
            appointment.heartRate = dto.heartRate;
            appointment.respiratoryRate = dto.respiratoryRate;
            appointment.oxygenSaturation = dto.oxygenSaturation;
            appointment.weight = dto.weight;
            appointment.height = dto.height;

            await AddStatusHistory(
                appointment.Id,
                await GetLastAppointmentStatus(appointment.Id),
                StatusEnCurso,
                $"Inicio de cita registrado el {appointment.arrivalDate:yyyy-MM-dd HH:mm} UTC.");
            await context.SaveChangesAsync();

            return mapper.Map<citaDto>(appointment);
        }

        [HttpPost("finalizar")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "DOCTOR,ADMINISTRATOR")]
        public async Task<ActionResult<citaDto>> FinalizarCita([FromBody] finalizarCitaDto dto)
        {
            if (dto == null)
                return BadRequest(new errorMessageDto("Debe enviar la informacion para finalizar la cita."));

            AppointmentModel appointment = await context.Set<AppointmentModel>()
                .Include(x => x.doctor)
                .Include(x => x.patient)
                .FirstOrDefaultAsync(x => x.Id == dto.appointmentId && x.deleteAt == null);

            if (appointment == null)
                return NotFound();

            if (!CanManageAppointment(appointment))
                return Forbid();

            errorMessageDto error = await ValidateFinalizarCita(dto, appointment);
            if (error != null)
                return BadRequest(error);

            DateTime now = DateTime.UtcNow;
            string previousStatus = await GetLastAppointmentStatus(appointment.Id);
            appointment.diagnosis = dto.diagnosis.Trim();
            appointment.observations = string.IsNullOrWhiteSpace(dto.observations) ? null : dto.observations.Trim();
            appointment.treatment = dto.treatment.Trim();
            appointment.endDate = now;

            if (dto.requiresRecipe)
            {
                foreach (finalizarCitaRecipeDto recipe in dto.recipes)
                {
                    context.Recipes.Add(new RecipeModel
                    {
                        appointmentId = appointment.Id,
                        medicineId = recipe.medicineId,
                        days = recipe.days,
                        timeLimit = recipe.timeLimit,
                        createAt = now
                    });
                }
            }

            if (dto.requiresLabExams)
            {
                List<LabExamScheduleSlot> labExamSchedule = await BuildLabExamSchedule(dto.labExams, now);
                List<ExamModel> createdExams = new List<ExamModel>();
                foreach (LabExamScheduleSlot scheduledExam in labExamSchedule)
                {
                    ExamModel exam = new ExamModel
                    {
                        appointmentId = appointment.Id,
                        examTypeId = scheduledExam.Exam.examTypeId,
                        attendantId = scheduledExam.AttendantId,
                        startDate = scheduledExam.StartDate,
                        endDate = scheduledExam.EndDate,
                        results = string.Empty,
                        observations = scheduledExam.Exam.indications.Trim(),
                        createAt = now
                    };
                    context.Exams.Add(exam);
                    createdExams.Add(exam);
                }

                await context.SaveChangesAsync();
                foreach (ExamModel createdExam in createdExams)
                {
                    await AddExamStatusHistory(
                        createdExam.Id,
                        null,
                        StatusActivo,
                        "Examen programado al finalizar consulta.");
                }
            }

            if (dto.requiresReschedule && dto.newStartDate.HasValue)
            {
                DateTime newStartDate = ToUtc(dto.newStartDate.Value);
                AppointmentModel newAppointment = new AppointmentModel
                {
                    reason = dto.rescheduleReason.Trim(),
                    isEmergency = false,
                    scheduledDate = newStartDate,
                    startDate = newStartDate,
                    endDate = null,
                    doctorId = appointment.doctorId,
                    patientId = appointment.patientId,
                    createAt = now
                };

                context.Set<AppointmentModel>().Add(newAppointment);
                context.Entry(newAppointment).Property("doctorId1").CurrentValue = (long)appointment.doctorId;
                context.Entry(newAppointment).Property("patientId1").CurrentValue = (long)appointment.patientId;
                await context.SaveChangesAsync();
                await AddStatusHistory(newAppointment.Id, null, StatusActivo, "Cita de seguimiento agendada al finalizar consulta.");
            }

            await AddStatusHistory(appointment.Id, previousStatus, StatusFinalizada, "Cita finalizada.");
            await context.SaveChangesAsync();
            appointment.currentStatus = StatusFinalizada;
            return mapper.Map<citaDto>(appointment);
        }

        [HttpDelete("{id}")]
        public override async Task<ActionResult> delete(long id)
        {
            AppointmentModel appointment = await context.Set<AppointmentModel>()
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (appointment == null)
                return NotFound();

            if (!CanManageAppointment(appointment))
                return Forbid();

            string previousStatus = await GetLastAppointmentStatus(appointment.Id);
            if (!CanCancelStatus(previousStatus))
                return BadRequest(new errorMessageDto("Solo se pueden cancelar citas con estado ACTIVO o REAGENDAR."));

            appointment.deleteAt = DateTime.UtcNow;
            await AddStatusHistory(appointment.Id, previousStatus, StatusCancelar, "Cita cancelada.");
            await context.SaveChangesAsync();
            return Ok();
        }

        private async Task<long> ObtenerDoctorDisponible(
            DateTime fechaInicio,
            DateTime fechaFin,
            int doctorSolicitado = 0,
            long? excludeAppointmentId = null)
        {
            fechaInicio = ToUtc(fechaInicio);
            fechaFin = ToUtc(fechaFin);

            IQueryable<Worker> doctors = context.Set<Worker>()
                .Where(x => x.deleteAt == null
                    && context.doctorSpecialties.Any(ds => ds.doctorId == x.Id && ds.deleteAt == null));

            if (doctorSolicitado > 0)
                doctors = doctors.Where(x => x.Id == doctorSolicitado);

            return await doctors
                .Where(doctor => !context.Set<AppointmentModel>()
                    .Any(cita => (!excludeAppointmentId.HasValue || cita.Id != excludeAppointmentId.Value)
                        && cita.doctorId == doctor.Id && cita.deleteAt == null
                        && cita.startDate < fechaFin && (cita.endDate ?? cita.startDate.AddMinutes(30)) > fechaInicio))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
        }

        private bool CanManageAppointment(AppointmentModel appointment)
        {
            if (User.IsInRole("ADMINISTRATOR"))
                return true;

            if (User.IsInRole("userNormal"))
                return appointment.patientId == GetClaimInt("patientId");

            if (User.IsInRole("DOCTOR"))
                return appointment.doctorId == GetClaimInt("workerId");

            return false;
        }

        private async Task<List<AppointmentAvailabilitySuggestion>> GetAppointmentAvailabilitySuggestions(
            DateTime requestedStartDate,
            int doctorSolicitado,
            int patientId,
            long? excludeAppointmentId)
        {
            const int appointmentDurationMinutes = 30;
            const int slotStepMinutes = 30;
            const int maxSuggestions = 5;
            const int searchHorizonDays = 14;

            List<AppointmentAvailabilitySuggestion> suggestions = new List<AppointmentAvailabilitySuggestion>();
            DateTime firstSlotStart = RoundUpToSlot(ToUtc(requestedStartDate), slotStepMinutes);
            DateTime searchLimit = firstSlotStart.AddDays(searchHorizonDays);

            for (DateTime slotStart = firstSlotStart; slotStart < searchLimit && suggestions.Count < maxSuggestions; slotStart = slotStart.AddMinutes(slotStepMinutes))
            {
                DateTime slotEnd = slotStart.AddMinutes(appointmentDurationMinutes);
                if (patientId > 0 && await HasPatientAppointmentConflict(patientId, slotStart, slotEnd, excludeAppointmentId))
                    continue;

                long doctorId = await ObtenerDoctorDisponible(slotStart, slotEnd, doctorSolicitado, excludeAppointmentId);
                if (doctorId == 0)
                    continue;

                suggestions.Add(new AppointmentAvailabilitySuggestion
                {
                    startDate = slotStart,
                    endDate = slotEnd,
                    doctorId = doctorId
                });
            }

            return suggestions;
        }

        private async Task<bool> HasPatientAppointmentConflict(
            int patientId,
            DateTime startDate,
            DateTime endDate,
            long? excludeAppointmentId = null)
        {
            startDate = ToUtc(startDate);
            endDate = ToUtc(endDate);

            return await context.Set<AppointmentModel>()
                .AnyAsync(x => (!excludeAppointmentId.HasValue || x.Id != excludeAppointmentId.Value)
                    && x.patientId == patientId
                    && x.deleteAt == null
                    && x.startDate < endDate
                    && (x.endDate ?? x.startDate.AddMinutes(30)) > startDate);
        }

        private async Task<errorMessageDto> ValidateFinalizarCita(finalizarCitaDto dto, AppointmentModel appointment)
        {
            string currentStatus = await GetLastAppointmentStatus(appointment.Id);
            if (currentStatus != StatusEnCurso)
                return new errorMessageDto("Solo se pueden finalizar citas con estado EN_CURSO.");

            if (string.IsNullOrWhiteSpace(dto.diagnosis))
                return new errorMessageDto("El diagnostico es obligatorio.");

            if (dto.diagnosis.Trim().Length > 500)
                return new errorMessageDto("El diagnostico no puede superar 500 caracteres.");

            if (!string.IsNullOrWhiteSpace(dto.observations) && dto.observations.Trim().Length > 500)
                return new errorMessageDto("Las observaciones no pueden superar 500 caracteres.");

            if (string.IsNullOrWhiteSpace(dto.treatment))
                return new errorMessageDto("El tratamiento es obligatorio.");

            if (dto.treatment.Trim().Length > 500)
                return new errorMessageDto("El tratamiento no puede superar 500 caracteres.");

            if (dto.requiresRecipe)
            {
                if (dto.recipes == null || dto.recipes.Count == 0)
                    return new errorMessageDto("Debe agregar al menos un medicamento a la receta.");

                List<long> medicineIds = dto.recipes.Select(x => x.medicineId).Distinct().ToList();
                int validMedicines = await context.Medicines
                    .CountAsync(x => medicineIds.Contains(x.Id) && x.deleteAt == null);
                if (validMedicines != medicineIds.Count)
                    return new errorMessageDto("Uno o mas medicamentos seleccionados no existen.");

                if (dto.recipes.Any(x => x.days <= 0 || x.timeLimit <= 0))
                    return new errorMessageDto("Los dias y el plazo entre dosis deben ser mayores a cero.");
            }

            if (dto.requiresLabExams)
            {
                if (dto.labExams == null || dto.labExams.Count == 0)
                    return new errorMessageDto("Debe agregar al menos un examen de laboratorio.");

                List<long> examTypeIds = dto.labExams.Select(x => x.examTypeId).Distinct().ToList();
                int validExamTypes = await context.ExamTypes
                    .CountAsync(x => examTypeIds.Contains(x.Id) && x.deleteAt == null);
                if (validExamTypes != examTypeIds.Count)
                    return new errorMessageDto("Uno o mas tipos de examen seleccionados no existen.");

                if (dto.labExams.Any(x => string.IsNullOrWhiteSpace(x.indications) || x.indications.Trim().Length > 500))
                    return new errorMessageDto("Las indicaciones de los examenes son obligatorias y no pueden superar 500 caracteres.");

                List<LabExamScheduleSlot> labExamSchedule = await BuildLabExamSchedule(dto.labExams, DateTime.UtcNow);
                if (labExamSchedule.Count != dto.labExams.Count)
                    return new errorMessageDto("No hay horario disponible para uno de los examenes de laboratorio.");
            }

            if (dto.requiresReschedule)
            {
                if (string.IsNullOrWhiteSpace(dto.rescheduleReason))
                    return new errorMessageDto("El motivo de reagendamiento es obligatorio.");

                List<string> allowedReasons = new List<string>
                {
                    "Consulta con especialista",
                    "Revision de resultados de laboratorio"
                };
                if (!allowedReasons.Contains(dto.rescheduleReason.Trim()))
                    return new errorMessageDto("El motivo de reagendamiento seleccionado no es valido.");

                if (!dto.newStartDate.HasValue)
                    return new errorMessageDto("La nueva fecha y hora de cita es obligatoria.");

                DateTime newStartDate = ToUtc(dto.newStartDate.Value);
                if (newStartDate <= DateTime.UtcNow)
                    return new errorMessageDto("La nueva fecha y hora debe ser mayor a la fecha actual.");

                DateTime validationEndDate = newStartDate.AddMinutes(30);
                if (await HasPatientAppointmentConflict(appointment.patientId, newStartDate, validationEndDate, appointment.Id))
                    return new errorMessageDto("El paciente ya tiene una cita en el horario seleccionado.");

                bool conflict = await context.Set<AppointmentModel>()
                    .AnyAsync(x => x.Id != appointment.Id && x.doctorId == appointment.doctorId && x.deleteAt == null
                        && x.startDate < validationEndDate && (x.endDate ?? x.startDate.AddMinutes(30)) > newStartDate);
                if (conflict)
                    return new errorMessageDto("El medico ya tiene otra cita en el horario seleccionado.");
            }

            return null;
        }

        private async Task<errorMessageDto> ValidateInicioCita(inicioCitaDto dto, AppointmentModel appointment)
        {
            DateTime now = DateTime.UtcNow;
            DateTime startDate = ToUtc(appointment.startDate);
            DateTime maxArrivalDate = startDate.AddMinutes(10);

            if (now < startDate || now > maxArrivalDate)
                return new errorMessageDto("La cita solo puede iniciarse desde la hora programada hasta 10 minutos despues.");

            string currentStatus = await GetLastAppointmentStatus(appointment.Id);
            if (!CanStartStatus(currentStatus))
                return new errorMessageDto("Solo se pueden iniciar citas con estado ACTIVO o REAGENDAR.");

            if (dto.bloodPressure <= 0)
                return new errorMessageDto("La presion arterial debe ser un valor numerico mayor a cero.");

            if (dto.temperature <= 0)
                return new errorMessageDto("La temperatura debe ser un valor numerico mayor a cero.");

            if (dto.heartRate <= 0)
                return new errorMessageDto("La frecuencia cardiaca debe ser un valor numerico mayor a cero.");

            if (dto.respiratoryRate <= 0)
                return new errorMessageDto("La frecuencia respiratoria debe ser un valor numerico mayor a cero.");

            if (dto.oxygenSaturation <= 0)
                return new errorMessageDto("La saturacion de oxigeno debe ser un valor numerico mayor a cero.");

            if (dto.weight <= 0)
                return new errorMessageDto("El peso debe ser un valor numerico mayor a cero.");

            if (dto.height <= 0)
                return new errorMessageDto("La talla debe ser un valor numerico mayor a cero.");

            return null;
        }

        private int GetClaimInt(string claimType)
        {
            Claim claim = User.Claims.FirstOrDefault(x => x.Type == claimType);
            return int.TryParse(claim?.Value, out int value) ? value : 0;
        }

        private async Task<string> GetLastAppointmentStatus(long appointmentId)
        {
            string status = await context.AppointmentStatusHistories
                .Where(x => x.appointmentId == appointmentId && x.deleteAt == null)
                .OrderByDescending(x => x.changedAt)
                .Select(x => x.status.name)
                .FirstOrDefaultAsync();

            return string.IsNullOrWhiteSpace(status) ? StatusActivo : status;
        }

        private static bool CanCancelStatus(string status)
        {
            return status == StatusActivo || status == StatusReagendar;
        }

        private static bool CanStartStatus(string status)
        {
            return status == StatusActivo || status == StatusReagendar;
        }

        private async Task<List<LabExamScheduleSlot>> BuildLabExamSchedule(
            List<finalizarCitaExamDto> labExams,
            DateTime requestedStartDate)
        {
            const int examDurationMinutes = 30;
            const int slotStepMinutes = 30;
            const int searchHorizonDays = 14;

            List<LabExamScheduleSlot> schedule = new List<LabExamScheduleSlot>();
            DateTime firstSlotStart = RoundUpToSlot(ToUtc(requestedStartDate), slotStepMinutes);
            DateTime searchLimit = firstSlotStart.AddDays(searchHorizonDays);

            foreach (finalizarCitaExamDto labExam in labExams)
            {
                List<long> compatibleAttendantIds = await context.LaboratoryAttendantExamTypes
                    .Where(x => x.examTypeId == labExam.examTypeId
                        && x.deleteAt == null
                        && x.attendant.deleteAt == null)
                    .OrderBy(x => x.attendantId)
                    .Select(x => x.attendantId)
                    .Distinct()
                    .ToListAsync();

                if (compatibleAttendantIds.Count == 0)
                    return new List<LabExamScheduleSlot>();

                LabExamScheduleSlot? selectedSlot = null;
                for (DateTime slotStart = firstSlotStart; slotStart < searchLimit; slotStart = slotStart.AddMinutes(slotStepMinutes))
                {
                    DateTime slotEnd = slotStart.AddMinutes(examDurationMinutes);
                    foreach (long attendantId in compatibleAttendantIds)
                    {
                        bool alreadyReserved = schedule.Any(x => x.AttendantId == attendantId
                            && x.StartDate < slotEnd
                            && x.EndDate > slotStart);
                        if (alreadyReserved)
                            continue;

                        bool occupied = await context.Exams.AnyAsync(exam => exam.attendantId == attendantId
                            && exam.deleteAt == null
                            && exam.startDate < slotEnd
                            && exam.endDate > slotStart);
                        if (occupied)
                            continue;

                        selectedSlot = new LabExamScheduleSlot
                        {
                            Exam = labExam,
                            AttendantId = attendantId,
                            StartDate = slotStart,
                            EndDate = slotEnd
                        };
                        break;
                    }

                    if (selectedSlot != null)
                        break;
                }

                if (selectedSlot == null)
                    return new List<LabExamScheduleSlot>();

                schedule.Add(selectedSlot);
            }

            return schedule;
        }

        private static DateTime RoundUpToSlot(DateTime dateTime, int slotMinutes)
        {
            DateTime utcDate = dateTime.Kind == DateTimeKind.Utc
                ? dateTime
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            long slotTicks = TimeSpan.FromMinutes(slotMinutes).Ticks;
            long roundedTicks = ((utcDate.Ticks + slotTicks - 1) / slotTicks) * slotTicks;
            return new DateTime(roundedTicks, DateTimeKind.Utc);
        }

        private async Task AddStatusHistory(long appointmentId, string? previousStatus, string status, string comment)
        {
            DateTime now = DateTime.UtcNow;
            long? previousStatusId = string.IsNullOrWhiteSpace(previousStatus)
                ? null
                : await GetAppointmentStatusId(previousStatus);
            long statusId = await GetAppointmentStatusId(status);

            context.AppointmentStatusHistories.Add(new AppointmentStatusHistory
            {
                appointmentId = appointmentId,
                previousStatusId = previousStatusId,
                statusId = statusId,
                comment = comment,
                changedAt = now,
                changedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                createAt = now
            });
            await Task.CompletedTask;
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

        private static DateTime ToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }

        private class LabExamScheduleSlot
        {
            public finalizarCitaExamDto Exam { get; set; }
            public long AttendantId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        private class AppointmentAvailabilitySuggestion
        {
            public DateTime startDate { get; set; }
            public DateTime endDate { get; set; }
            public long doctorId { get; set; }
        }
    }
}
