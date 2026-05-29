using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
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

namespace project.Appointment
{
    [ApiController]
    [Route("api/citas")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "userNormal,DOCTOR,ADMINISTRATOR")]
    public class citasController : controllerCommons<AppointmentModel, citaCreationDto, citaDto, citaQueryDto, object, long>
    {
        protected override bool showDeleted { get; set; } = true;

        public citasController(DBProyContext context, IMapper mapper) : base(context, mapper)
        {
        }

        protected override async Task<IQueryable<AppointmentModel>> modifyGet(IQueryable<AppointmentModel> query, citaQueryDto queryParams)
        {
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

        protected override async Task<errorMessageDto> validPost(AppointmentModel entity, citaCreationDto newRegister, object queryParams)
        {
            if (newRegister == null)
                return new errorMessageDto("Debe enviar la informacion de la cita.");

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

        [HttpGet("disponibilidad")]
        public async Task<ActionResult<object>> ValidarDisponibilidad([FromQuery] DateTime fechaHora)
        {
            DateTime fechaInicio = ToUtc(fechaHora);

            if (fechaInicio <= DateTime.UtcNow)
                return BadRequest(new errorMessageDto("La fecha y hora no pueden ser anteriores a la fecha actual."));

            DateTime fechaFin = fechaInicio.AddMinutes(30);
            long doctorId = await ObtenerDoctorDisponible(fechaInicio, fechaFin);

            return Ok(new
            {
                disponible = doctorId != 0,
                doctorId = doctorId == 0 ? (long?)null : doctorId
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

            appointment.deleteAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("reagendar")]
        public async Task<ActionResult> ReagendarCita([FromBody] reagendarDto dto)
        {
            if (dto == null || !dto.citaId.HasValue)
                return BadRequest(new errorMessageDto("Debe enviar el id de la cita a reagendar."));

            DateTime newStartDate = ToUtc(dto.newStartDate);
            DateTime newEndDate = ToUtc(dto.newEndDate);

            if (newEndDate <= newStartDate)
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

            bool conflict = await context.Set<AppointmentModel>()
                .AnyAsync(x => x.Id != appointment.Id && x.doctorId == appointment.doctorId && x.deleteAt == null
                    && x.startDate < newEndDate && x.endDate > newStartDate);

            if (conflict)
                return BadRequest(new errorMessageDto("El médico ya tiene otra cita en el horario seleccionado."));

            appointment.startDate = newStartDate;
            appointment.endDate = newEndDate;
            appointment.scheduledDate = newStartDate;
            await context.SaveChangesAsync();
            return Ok();
        }

        private async Task<long> ObtenerDoctorDisponible(DateTime fechaInicio, DateTime fechaFin, int doctorSolicitado = 0)
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
                    .Any(cita => cita.doctorId == doctor.Id && cita.deleteAt == null
                        && cita.startDate < fechaFin && cita.endDate > fechaInicio))
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

        private int GetClaimInt(string claimType)
        {
            Claim claim = User.Claims.FirstOrDefault(x => x.Type == claimType);
            return int.TryParse(claim?.Value, out int value) ? value : 0;
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
