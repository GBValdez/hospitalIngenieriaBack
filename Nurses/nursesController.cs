using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.Nurses.dto;
using project.roles;
using project.users;
using project.utils.dto;

namespace project.Nurses
{
    [ApiController]
    [Route("api/enfermeras")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
    public class nursesController : ControllerBase
    {
        private const string NurseRole = "NURSE";
        private readonly DBProyContext context;
        private readonly UserManager<userEntity> userManager;
        private readonly RoleManager<rolEntity> roleManager;

        public nursesController(
            DBProyContext context,
            UserManager<userEntity> userManager,
            RoleManager<rolEntity> roleManager)
        {
            this.context = context;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        [HttpGet]
        public async Task<ActionResult<resPag<nurseDto>>> Get(
            [FromQuery] pagQueryDto page,
            [FromQuery] nurseQueryDto query)
        {
            List<string> nurseUserIds = await GetNurseUserIds();
            IQueryable<Worker> nurses = context.Workers
                .Include(x => x.user)
                .Include(x => x.sex)
                .Include(x => x.nationality)
                .Where(x => nurseUserIds.Contains(x.userId));

            if (!string.IsNullOrWhiteSpace(query?.name))
            {
                string name = query.name.Trim().ToLower();
                nurses = nurses.Where(x => x.name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(query?.email))
            {
                string email = query.email.Trim().ToLower();
                nurses = nurses.Where(x => x.user.Email.ToLower().Contains(email));
            }

            if (query?.isActive != null)
                nurses = query.isActive.Value
                    ? nurses.Where(x => x.deleteAt == null && x.user.deleteAt == null)
                    : nurses.Where(x => x.deleteAt != null || x.user.deleteAt != null);

            int total = await nurses.CountAsync();
            int pageSize = page.pageSize <= 0 ? 10 : page.pageSize;
            int pageNumber = page.pageNumber <= 0 ? 1 : page.pageNumber;
            int totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);

            if (page.all != true)
                nurses = nurses.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<nurseDto> items = await nurses
                .OrderBy(x => x.Id)
                .Select(x => MapNurse(x))
                .ToListAsync();

            return new resPag<nurseDto>
            {
                items = items,
                total = total,
                index = pageNumber,
                totalPages = totalPages
            };
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<nurseDto>> GetById(long id)
        {
            Worker nurse = await context.Workers
                .Include(x => x.user)
                .Include(x => x.sex)
                .Include(x => x.nationality)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (nurse == null || !await HasNurseRole(nurse.userId))
                return NotFound();

            return MapNurse(nurse);
        }

        [HttpPost]
        public async Task<ActionResult<nurseDto>> Post([FromBody] nurseCreationDto dto)
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

            await EnsureNurseRole();
            IdentityResult roleResult = await userManager.AddToRoleAsync(user, NurseRole);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            Worker nurse = new Worker
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

            context.Workers.Add(nurse);
            await context.SaveChangesAsync();
            return MapNurse(nurse);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(long id, [FromBody] nurseUpdateDto dto)
        {
            Worker nurse = await context.Workers
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (nurse == null || !await HasNurseRole(nurse.userId))
                return NotFound();

            errorMessageDto dateError = ValidateWorkerDates(dto.birthday, dto.hiringDate);
            if (dateError != null)
                return BadRequest(dateError);

            errorMessageDto catalogueError = await ValidateCatalogues(dto.sexId, dto.nationalityId);
            if (catalogueError != null)
                return BadRequest(catalogueError);

            userEntity emailUser = await userManager.FindByEmailAsync(dto.email);
            if (emailUser != null && emailUser.Id != nurse.userId)
                return BadRequest(new errorMessageDto("El correo ya esta en uso."));

            nurse.name = dto.name.Trim();
            nurse.dpi = dto.dpi.Trim();
            nurse.direction = dto.direction.Trim();
            nurse.birthday = dto.birthday;
            nurse.sexId = dto.sexId;
            nurse.nationalityId = dto.nationalityId;
            nurse.hiringDate = ToUtc(dto.hiringDate);
            nurse.deleteAt = dto.isActive ? null : DateTime.UtcNow;

            nurse.user.Email = dto.email;
            nurse.user.PhoneNumber = dto.phoneNumber;
            nurse.user.deleteAt = dto.isActive ? null : DateTime.UtcNow;
            await userManager.UpdateAsync(nurse.user);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            Worker nurse = await context.Workers
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (nurse == null || !await HasNurseRole(nurse.userId))
                return NotFound();

            DateTime deletedAt = DateTime.UtcNow;
            nurse.deleteAt = deletedAt;
            if (nurse.user != null)
            {
                nurse.user.deleteAt = deletedAt;
                await userManager.UpdateAsync(nurse.user);
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        private static nurseDto MapNurse(Worker nurse)
        {
            return new nurseDto
            {
                id = nurse.Id,
                name = nurse.name,
                dpi = nurse.dpi,
                direction = nurse.direction,
                birthday = nurse.birthday,
                sexId = nurse.sexId,
                sexName = nurse.sex?.name,
                nationalityId = nurse.nationalityId,
                nationalityName = nurse.nationality?.name,
                hiringDate = nurse.hiringDate,
                userId = nurse.userId,
                userName = nurse.user?.UserName,
                email = nurse.user?.Email,
                phoneNumber = nurse.user?.PhoneNumber,
                isActive = nurse.deleteAt == null && nurse.user?.deleteAt == null
            };
        }

        private async Task<List<string>> GetNurseUserIds()
        {
            rolEntity role = await roleManager.FindByNameAsync(NurseRole);
            if (role == null)
                return new List<string>();

            return await context.UserRoles
                .Where(x => x.RoleId == role.Id)
                .Select(x => x.UserId)
                .ToListAsync();
        }

        private async Task<bool> HasNurseRole(string userId)
        {
            userEntity user = await userManager.FindByIdAsync(userId);
            return user != null && await userManager.IsInRoleAsync(user, NurseRole);
        }

        private async Task<errorMessageDto> ValidateCatalogues(long sexId, long nationalityId)
        {
            if (!await context.Sexs.AnyAsync(x => x.Id == sexId && x.deleteAt == null))
                return new errorMessageDto("El sexo seleccionado no existe.");

            if (!await context.Nationalities.AnyAsync(x => x.Id == nationalityId && x.deleteAt == null))
                return new errorMessageDto("La nacionalidad seleccionada no existe.");

            return null;
        }

        private errorMessageDto ValidateWorkerDates(DateOnly birthday, DateTime hiringDate)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly minimumAdultDate = birthday.AddYears(18);
            DateOnly hiringDateOnly = DateOnly.FromDateTime(ToUtc(hiringDate));

            if (minimumAdultDate > today)
                return new errorMessageDto("La enfermera debe ser mayor de edad.");

            if (hiringDateOnly > today)
                return new errorMessageDto("La fecha de contratacion no puede ser mayor a la fecha actual.");

            if (hiringDateOnly < minimumAdultDate)
                return new errorMessageDto("La fecha de contratacion debe ser al menos 18 anios despues de la fecha de nacimiento.");

            return null;
        }

        private async Task EnsureNurseRole()
        {
            if (!await roleManager.RoleExistsAsync(NurseRole))
                await roleManager.CreateAsync(new rolEntity { Name = NurseRole });
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
