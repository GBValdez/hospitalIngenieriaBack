using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fletesProyect.LaboratoryAttendantExamType;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.LaboratoryAttendants.dto;
using project.Models;
using project.roles;
using project.users;
using project.utils.dto;

namespace project.LaboratoryAttendants
{
    [ApiController]
    [Route("api/encargados-laboratorio")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
    public class laboratoryAttendantsController : ControllerBase
    {
        private const string LaboratoryAttendantRole = "LAB_ATTENDANT";
        private readonly DBProyContext context;
        private readonly UserManager<userEntity> userManager;
        private readonly RoleManager<rolEntity> roleManager;

        public laboratoryAttendantsController(
            DBProyContext context,
            UserManager<userEntity> userManager,
            RoleManager<rolEntity> roleManager)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult<resPag<laboratoryAttendantDto>>> Get(
            [FromQuery] pagQueryDto page,
            [FromQuery] laboratoryAttendantQueryDto query)
        {
            List<string> attendantUserIds = await GetLaboratoryAttendantUserIds();
            IQueryable<Worker> attendants = context.Workers
                .Include(x => x.user)
                .Include(x => x.sex)
                .Include(x => x.nationality)
                .Where(x => attendantUserIds.Contains(x.userId));

            if (!string.IsNullOrWhiteSpace(query?.name))
            {
                string name = query.name.Trim().ToLower();
                attendants = attendants.Where(x => x.name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(query?.email))
            {
                string email = query.email.Trim().ToLower();
                attendants = attendants.Where(x => x.user.Email.ToLower().Contains(email));
            }

            if (query?.examTypeId != null)
                attendants = attendants.Where(x => context.LaboratoryAttendantExamTypes.Any(et => et.attendantId == x.Id && et.examTypeId == query.examTypeId.Value && et.deleteAt == null));

            if (query?.isActive != null)
                attendants = query.isActive.Value
                    ? attendants.Where(x => x.deleteAt == null && x.user.deleteAt == null)
                    : attendants.Where(x => x.deleteAt != null || x.user.deleteAt != null);

            int total = await attendants.CountAsync();
            int pageSize = page.pageSize <= 0 ? 10 : page.pageSize;
            int pageNumber = page.pageNumber <= 0 ? 1 : page.pageNumber;
            int totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);

            if (page.all != true)
                attendants = attendants.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<Worker> list = await attendants
                .OrderBy(x => x.Id)
                .ToListAsync();
            List<laboratoryAttendantDto> items = new List<laboratoryAttendantDto>();
            foreach (Worker attendant in list)
            {
                items.Add(await MapAttendant(attendant));
            }

            return new resPag<laboratoryAttendantDto>
            {
                items = items,
                total = total,
                index = pageNumber,
                totalPages = totalPages
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<laboratoryAttendantDto>> GetById(long id)
        {
            Worker attendant = await context.Workers
                .Include(x => x.user)
                .Include(x => x.sex)
                .Include(x => x.nationality)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (attendant == null || !await HasLaboratoryAttendantRole(attendant.userId))
                return NotFound();

            return await MapAttendant(attendant);
        }

        [HttpPost]
        public async Task<ActionResult<laboratoryAttendantDto>> Post([FromBody] laboratoryAttendantCreationDto dto)
        {
            if (await userManager.FindByEmailAsync(dto.email) != null)
                return BadRequest(new errorMessageDto("El correo ya esta en uso."));

            if (await userManager.FindByNameAsync(dto.userName) != null)
                return BadRequest(new errorMessageDto("El nombre de usuario ya esta en uso."));

            errorMessageDto dateError = ValidateWorkerDates(dto.birthday, dto.hiringDate);
            if (dateError != null)
                return BadRequest(dateError);

            errorMessageDto catalogueError = await ValidateCatalogues(dto.sexId, dto.nationalityId);
            if (catalogueError != null)
                return BadRequest(catalogueError);

            List<long> examTypeIds = GetExamTypeIds(dto.examTypeIds);
            errorMessageDto examTypeError = await ValidateExamTypes(examTypeIds);
            if (examTypeError != null)
                return BadRequest(examTypeError);

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

            await EnsureLaboratoryAttendantRole();
            IdentityResult roleResult = await userManager.AddToRoleAsync(user, LaboratoryAttendantRole);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            Worker attendant = new Worker
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

            context.Workers.Add(attendant);
            await context.SaveChangesAsync();

            foreach (long examTypeId in examTypeIds)
            {
                context.LaboratoryAttendantExamTypes.Add(new LaboratoryAttendantExamType
                {
                    attendantId = attendant.Id,
                    examTypeId = examTypeId
                });
            }
            await context.SaveChangesAsync();

            return await MapAttendant(attendant);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(long id, [FromBody] laboratoryAttendantUpdateDto dto)
        {
            Worker attendant = await context.Workers
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (attendant == null || !await HasLaboratoryAttendantRole(attendant.userId))
                return NotFound();

            errorMessageDto dateError = ValidateWorkerDates(dto.birthday, dto.hiringDate);
            if (dateError != null)
                return BadRequest(dateError);

            errorMessageDto catalogueError = await ValidateCatalogues(dto.sexId, dto.nationalityId);
            if (catalogueError != null)
                return BadRequest(catalogueError);

            List<long> examTypeIds = GetExamTypeIds(dto.examTypeIds);
            errorMessageDto examTypeError = await ValidateExamTypes(examTypeIds);
            if (examTypeError != null)
                return BadRequest(examTypeError);

            userEntity emailUser = await userManager.FindByEmailAsync(dto.email);
            if (emailUser != null && emailUser.Id != attendant.userId)
                return BadRequest(new errorMessageDto("El correo ya esta en uso."));

            attendant.name = dto.name.Trim();
            attendant.dpi = dto.dpi.Trim();
            attendant.direction = dto.direction.Trim();
            attendant.birthday = dto.birthday;
            attendant.sexId = dto.sexId;
            attendant.nationalityId = dto.nationalityId;
            attendant.hiringDate = ToUtc(dto.hiringDate);
            attendant.deleteAt = dto.isActive ? null : DateTime.UtcNow;

            attendant.user.Email = dto.email;
            attendant.user.PhoneNumber = dto.phoneNumber;
            attendant.user.deleteAt = dto.isActive ? null : DateTime.UtcNow;
            await userManager.UpdateAsync(attendant.user);

            List<LaboratoryAttendantExamType> currentExamTypes = await context.LaboratoryAttendantExamTypes
                .Where(x => x.attendantId == id && x.deleteAt == null)
                .ToListAsync();

            foreach (LaboratoryAttendantExamType currentExamType in currentExamTypes)
            {
                currentExamType.deleteAt = DateTime.UtcNow;
            }

            foreach (long examTypeId in examTypeIds)
            {
                context.LaboratoryAttendantExamTypes.Add(new LaboratoryAttendantExamType
                {
                    attendantId = id,
                    examTypeId = examTypeId
                });
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            Worker attendant = await context.Workers
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (attendant == null || !await HasLaboratoryAttendantRole(attendant.userId))
                return NotFound();

            DateTime deletedAt = DateTime.UtcNow;
            attendant.deleteAt = deletedAt;
            if (attendant.user != null)
            {
                attendant.user.deleteAt = deletedAt;
                await userManager.UpdateAsync(attendant.user);
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<laboratoryAttendantDto> MapAttendant(Worker attendant)
        {
            List<LaboratoryAttendantExamType> examTypes = await context.LaboratoryAttendantExamTypes
                .Include(x => x.examType)
                .Where(x => x.attendantId == attendant.Id && x.deleteAt == null)
                .OrderBy(x => x.Id)
                .ToListAsync();

            return new laboratoryAttendantDto
            {
                id = attendant.Id,
                name = attendant.name,
                dpi = attendant.dpi,
                direction = attendant.direction,
                birthday = attendant.birthday,
                sexId = attendant.sexId,
                sexName = attendant.sex?.name,
                nationalityId = attendant.nationalityId,
                nationalityName = attendant.nationality?.name,
                hiringDate = attendant.hiringDate,
                examTypeIds = examTypes.Select(x => x.examTypeId).ToList(),
                examTypeNames = examTypes
                    .Where(x => x.examType != null)
                    .Select(x => x.examType.name)
                    .ToList(),
                userId = attendant.userId,
                userName = attendant.user?.UserName,
                email = attendant.user?.Email,
                phoneNumber = attendant.user?.PhoneNumber,
                isActive = attendant.deleteAt == null && attendant.user?.deleteAt == null
            };
        }

        private async Task<List<string>> GetLaboratoryAttendantUserIds()
        {
            rolEntity role = await roleManager.FindByNameAsync(LaboratoryAttendantRole);
            if (role == null)
                return new List<string>();

            return await context.UserRoles
                .Where(x => x.RoleId == role.Id)
                .Select(x => x.UserId)
                .ToListAsync();
        }

        private async Task<bool> HasLaboratoryAttendantRole(string userId)
        {
            userEntity user = await userManager.FindByIdAsync(userId);
            return user != null && await userManager.IsInRoleAsync(user, LaboratoryAttendantRole);
        }

        private async Task<errorMessageDto> ValidateCatalogues(long sexId, long nationalityId)
        {
            if (!await context.Sexs.AnyAsync(x => x.Id == sexId && x.deleteAt == null))
                return new errorMessageDto("El sexo seleccionado no existe.");

            if (!await context.Nationalities.AnyAsync(x => x.Id == nationalityId && x.deleteAt == null))
                return new errorMessageDto("La nacionalidad seleccionada no existe.");

            return null;
        }

        private async Task<errorMessageDto> ValidateExamTypes(List<long> examTypeIds)
        {
            if (examTypeIds == null || examTypeIds.Count == 0)
                return new errorMessageDto("Debe seleccionar al menos un tipo de examen.");

            int validExamTypes = await context.ExamTypes
                .CountAsync(x => examTypeIds.Contains(x.Id) && x.deleteAt == null);
            if (validExamTypes != examTypeIds.Count)
                return new errorMessageDto("Uno o mas tipos de examen seleccionados no existen.");

            return null;
        }

        private static List<long> GetExamTypeIds(List<long> examTypeIds)
        {
            return (examTypeIds ?? new List<long>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }

        private errorMessageDto ValidateWorkerDates(DateOnly birthday, DateTime hiringDate)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly minimumAdultDate = birthday.AddYears(18);
            DateOnly hiringDateOnly = DateOnly.FromDateTime(ToUtc(hiringDate));

            if (minimumAdultDate > today)
                return new errorMessageDto("El encargado de laboratorio debe ser mayor de edad.");

            if (hiringDateOnly > today)
                return new errorMessageDto("La fecha de contratacion no puede ser mayor a la fecha actual.");

            if (hiringDateOnly < minimumAdultDate)
                return new errorMessageDto("La fecha de contratacion debe ser al menos 18 anios despues de la fecha de nacimiento.");

            return null;
        }

        private async Task EnsureLaboratoryAttendantRole()
        {
            if (!await roleManager.RoleExistsAsync(LaboratoryAttendantRole))
                await roleManager.CreateAsync(new rolEntity { Name = LaboratoryAttendantRole });
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
