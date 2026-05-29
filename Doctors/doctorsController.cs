using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using back.catalogues;
using fletesProyect.DoctorSpecialty;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Doctors.dto;
using project.Models;
using project.roles;
using project.users;
using project.utils.dto;

namespace project.Doctors
{
    [ApiController]
    [Route("api/doctores")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
    public class doctorsController : ControllerBase
    {
        private const string DoctorRole = "DOCTOR";
        private readonly DBProyContext context;
        private readonly UserManager<userEntity> userManager;
        private readonly RoleManager<rolEntity> roleManager;

        public doctorsController(
            DBProyContext context,
            UserManager<userEntity> userManager,
            RoleManager<rolEntity> roleManager)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult<resPag<doctorDto>>> Get([FromQuery] pagQueryDto page, [FromQuery] doctorQueryDto query)
        {
            IQueryable<Worker> doctors = context.Workers
                .Include(x => x.user)
                .Include(x => x.sex)
                .Include(x => x.nationality)
                .Where(x => context.doctorSpecialties.Any(ds => ds.doctorId == x.Id && ds.deleteAt == null));

            if (!string.IsNullOrWhiteSpace(query?.name))
            {
                string name = query.name.Trim().ToLower();
                doctors = doctors.Where(x => x.name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(query?.email))
            {
                string email = query.email.Trim().ToLower();
                doctors = doctors.Where(x => x.user.Email.ToLower().Contains(email));
            }

            if (query?.specialtyId != null)
                doctors = doctors.Where(x => context.doctorSpecialties.Any(ds => ds.doctorId == x.Id && ds.specialtyId == query.specialtyId.Value && ds.deleteAt == null));

            if (query?.isActive != null)
                doctors = query.isActive.Value
                    ? doctors.Where(x => x.deleteAt == null && x.user.deleteAt == null)
                    : doctors.Where(x => x.deleteAt != null || x.user.deleteAt != null);

            int total = await doctors.CountAsync();
            int pageSize = page.pageSize <= 0 ? 10 : page.pageSize;
            int pageNumber = page.pageNumber <= 0 ? 1 : page.pageNumber;
            int totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);

            if (page.all != true)
                doctors = doctors.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<Worker> list = await doctors.OrderBy(x => x.Id).ToListAsync();
            List<doctorDto> items = new List<doctorDto>();
            foreach (Worker doctor in list)
            {
                items.Add(await MapDoctor(doctor));
            }

            return new resPag<doctorDto>
            {
                items = items,
                total = total,
                index = pageNumber,
                totalPages = totalPages
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<doctorDto>> GetById(long id)
        {
            Worker doctor = await context.Workers
                .Include(x => x.user)
                .Include(x => x.sex)
                .Include(x => x.nationality)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (doctor == null || !await HasDoctorSpecialty(id))
                return NotFound();

            return await MapDoctor(doctor);
        }

        [HttpPost]
        public async Task<ActionResult<doctorDto>> Post([FromBody] doctorCreationDto dto)
        {
            if (await userManager.FindByEmailAsync(dto.email) != null)
                return BadRequest(new errorMessageDto("El correo ya esta en uso."));

            if (await userManager.FindByNameAsync(dto.userName) != null)
                return BadRequest(new errorMessageDto("El nombre de usuario ya esta en uso."));

            errorMessageDto dateError = ValidateDoctorDates(dto.birthday, dto.hiringDate);
            if (dateError != null)
                return BadRequest(dateError);

            List<long> specialtyIds = GetSpecialtyIds(dto.specialtyIds, dto.specialtyId);
            errorMessageDto catalogueError = await ValidateCatalogues(dto.sexId, dto.nationalityId, specialtyIds);
            if (catalogueError != null)
                return BadRequest(catalogueError);

            userEntity user = new userEntity
            {
                UserName = dto.userName,
                Email = dto.email,
                PhoneNumber = dto.phoneNumber,
                EmailConfirmed = true,
                createAt = DateTime.UtcNow
            };

            IdentityResult userResult = await userManager.CreateAsync(user, dto.password);
            if (!userResult.Succeeded)
                return BadRequest(userResult.Errors);

            await EnsureDoctorRole();
            IdentityResult roleResult = await userManager.AddToRoleAsync(user, DoctorRole);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            Worker doctor = new Worker
            {
                name = dto.name.Trim(),
                dpi = dto.dpi.Trim(),
                direction = dto.direction.Trim(),
                birthday = dto.birthday,
                sexId = dto.sexId,
                nationalityId = dto.nationalityId,
                hiringDate = ToUtc(dto.hiringDate),
                userId = user.Id
            };

            context.Workers.Add(doctor);
            await context.SaveChangesAsync();

            foreach (long specialtyId in specialtyIds)
            {
                context.doctorSpecialties.Add(new DoctorSpecialty
                {
                    doctorId = doctor.Id,
                    specialtyId = specialtyId,
                    licenseNumber = dto.licenseNumber.Trim()
                });
            }
            await context.SaveChangesAsync();

            return await MapDoctor(doctor);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(long id, [FromBody] doctorUpdateDto dto)
        {
            Worker doctor = await context.Workers
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (doctor == null || !await HasDoctorSpecialty(id))
                return NotFound();

            errorMessageDto dateError = ValidateDoctorDates(dto.birthday, dto.hiringDate);
            if (dateError != null)
                return BadRequest(dateError);

            List<long> specialtyIds = GetSpecialtyIds(dto.specialtyIds, dto.specialtyId);
            errorMessageDto catalogueError = await ValidateCatalogues(dto.sexId, dto.nationalityId, specialtyIds);
            if (catalogueError != null)
                return BadRequest(catalogueError);

            userEntity emailUser = await userManager.FindByEmailAsync(dto.email);
            if (emailUser != null && emailUser.Id != doctor.userId)
                return BadRequest(new errorMessageDto("El correo ya esta en uso."));

            doctor.name = dto.name.Trim();
            doctor.dpi = dto.dpi.Trim();
            doctor.direction = dto.direction.Trim();
            doctor.birthday = dto.birthday;
            doctor.sexId = dto.sexId;
            doctor.nationalityId = dto.nationalityId;
            doctor.hiringDate = ToUtc(dto.hiringDate);
            doctor.deleteAt = dto.isActive ? null : DateTime.UtcNow;

            doctor.user.Email = dto.email;
            doctor.user.UserName = doctor.user.UserName;
            doctor.user.PhoneNumber = dto.phoneNumber;
            doctor.user.deleteAt = dto.isActive ? null : DateTime.UtcNow;
            await userManager.UpdateAsync(doctor.user);

            List<DoctorSpecialty> currentSpecialties = await context.doctorSpecialties
                .Where(x => x.doctorId == id && x.deleteAt == null)
                .ToListAsync();

            foreach (DoctorSpecialty currentSpecialty in currentSpecialties)
            {
                currentSpecialty.deleteAt = DateTime.UtcNow;
            }

            foreach (long specialtyId in specialtyIds)
            {
                context.doctorSpecialties.Add(new DoctorSpecialty
                {
                    doctorId = id,
                    specialtyId = specialtyId,
                    licenseNumber = dto.licenseNumber.Trim()
                });
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            Worker doctor = await context.Workers
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (doctor == null || !await HasDoctorSpecialty(id))
                return NotFound();

            DateTime deletedAt = DateTime.UtcNow;
            doctor.deleteAt = deletedAt;
            if (doctor.user != null)
            {
                doctor.user.deleteAt = deletedAt;
                await userManager.UpdateAsync(doctor.user);
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<doctorDto> MapDoctor(Worker doctor)
        {
            List<DoctorSpecialty> doctorSpecialties = await context.doctorSpecialties
                .Include(x => x.specialty)
                .Where(x => x.doctorId == doctor.Id && x.deleteAt == null)
                .OrderBy(x => x.Id)
                .ToListAsync();

            DoctorSpecialty doctorSpecialty = doctorSpecialties.FirstOrDefault();

            return new doctorDto
            {
                id = doctor.Id,
                name = doctor.name,
                dpi = doctor.dpi,
                direction = doctor.direction,
                birthday = doctor.birthday,
                sexId = doctor.sexId,
                sexName = doctor.sex?.name,
                nationalityId = doctor.nationalityId,
                nationalityName = doctor.nationality?.name,
                hiringDate = doctor.hiringDate,
                specialtyId = doctorSpecialty?.specialtyId ?? 0,
                specialtyName = doctorSpecialty?.specialty?.name,
                specialtyIds = doctorSpecialties.Select(x => x.specialtyId).ToList(),
                specialtyNames = doctorSpecialties
                    .Where(x => x.specialty != null)
                    .Select(x => x.specialty.name)
                    .ToList(),
                licenseNumber = doctorSpecialty?.licenseNumber,
                userId = doctor.userId,
                userName = doctor.user?.UserName,
                email = doctor.user?.Email,
                phoneNumber = doctor.user?.PhoneNumber,
                isActive = doctor.deleteAt == null && doctor.user?.deleteAt == null
            };
        }

        private async Task<bool> HasDoctorSpecialty(long doctorId)
        {
            return await context.doctorSpecialties.AnyAsync(x => x.doctorId == doctorId && x.deleteAt == null);
        }

        private async Task<errorMessageDto> ValidateCatalogues(long sexId, long nationalityId, List<long> specialtyIds)
        {
            if (!await context.Sexs.AnyAsync(x => x.Id == sexId && x.deleteAt == null))
                return new errorMessageDto("El sexo seleccionado no existe.");

            if (!await context.Nationalities.AnyAsync(x => x.Id == nationalityId && x.deleteAt == null))
                return new errorMessageDto("La nacionalidad seleccionada no existe.");

            if (specialtyIds == null || specialtyIds.Count == 0)
                return new errorMessageDto("Debe seleccionar al menos una especialidad.");

            int validSpecialties = await context.Specialtys
                .CountAsync(x => specialtyIds.Contains(x.Id) && x.deleteAt == null);
            if (validSpecialties != specialtyIds.Count)
                return new errorMessageDto("Una o mas especialidades seleccionadas no existen.");

            return null;
        }

        private static List<long> GetSpecialtyIds(List<long> specialtyIds, long? specialtyId)
        {
            List<long> ids = specialtyIds ?? new List<long>();
            if (ids.Count == 0 && specialtyId.HasValue)
                ids.Add(specialtyId.Value);

            return ids
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        private errorMessageDto ValidateDoctorDates(DateOnly birthday, DateTime hiringDate)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly minimumAdultDate = birthday.AddYears(18);
            DateOnly hiringDateOnly = DateOnly.FromDateTime(ToUtc(hiringDate));

            if (minimumAdultDate > today)
                return new errorMessageDto("El doctor debe ser mayor de edad.");

            if (hiringDateOnly < minimumAdultDate)
                return new errorMessageDto("La fecha de contratacion debe ser al menos 18 anios despues de la fecha de nacimiento.");

            return null;
        }

        private async Task EnsureDoctorRole()
        {
            if (!await roleManager.RoleExistsAsync(DoctorRole))
                await roleManager.CreateAsync(new rolEntity { Name = DoctorRole });
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
