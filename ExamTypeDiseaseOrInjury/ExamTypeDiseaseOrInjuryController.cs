using fletesProyect.ExamTypeDiseaseOrInjury.dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.dto;

namespace fletesProyect.ExamTypeDiseaseOrInjury
{
    [ApiController]
    [Route("exam-type-disease-or-injuries")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR,DOCTOR,LAB_ATTENDANT")]
    public class ExamTypeDiseaseOrInjuryController : ControllerBase
    {
        private readonly DBProyContext context;

        public ExamTypeDiseaseOrInjuryController(DBProyContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<examTypeDiseaseOrInjuryDto>>> Get([FromQuery] examTypeDiseaseOrInjuryQueryDto query)
        {
            IQueryable<ExamTypeDiseaseOrInjury> relations = context.ExamTypeDiseaseOrInjuries
                .Include(x => x.examType)
                .Include(x => x.diseaseOrInjury)
                .Where(x => x.deleteAt == null && x.examType.deleteAt == null && x.diseaseOrInjury.deleteAt == null);

            if (query.examTypeId.HasValue)
                relations = relations.Where(x => x.examTypeId == query.examTypeId.Value);

            if (query.diseaseOrInjuryId.HasValue)
                relations = relations.Where(x => x.diseaseOrInjuryId == query.diseaseOrInjuryId.Value);

            return await relations
                .OrderBy(x => x.examType.name)
                .ThenBy(x => x.diseaseOrInjury.name)
                .Select(x => new examTypeDiseaseOrInjuryDto
                {
                    id = x.Id,
                    examTypeId = x.examTypeId,
                    examTypeName = x.examType.name,
                    diseaseOrInjuryId = x.diseaseOrInjuryId,
                    diseaseOrInjuryName = x.diseaseOrInjury.name,
                    notes = x.notes
                })
                .ToListAsync();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public async Task<ActionResult<examTypeDiseaseOrInjuryDto>> Post([FromBody] examTypeDiseaseOrInjuryCreationDto dto)
        {
            errorMessageDto error = await ValidateDto(dto);
            if (error != null)
                return BadRequest(error);

            bool exists = await context.ExamTypeDiseaseOrInjuries.AnyAsync(x => x.examTypeId == dto.examTypeId
                && x.diseaseOrInjuryId == dto.diseaseOrInjuryId
                && x.deleteAt == null);
            if (exists)
                return BadRequest(new errorMessageDto("Ya existe una relacion entre este tipo de examen y enfermedad o lesion."));

            ExamTypeDiseaseOrInjury relation = new ExamTypeDiseaseOrInjury
            {
                examTypeId = dto.examTypeId,
                diseaseOrInjuryId = dto.diseaseOrInjuryId,
                notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim(),
                createAt = DateTime.UtcNow
            };

            context.ExamTypeDiseaseOrInjuries.Add(relation);
            await context.SaveChangesAsync();

            return await GetById(relation.Id);
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public async Task<ActionResult> Put(long id, [FromBody] examTypeDiseaseOrInjuryCreationDto dto)
        {
            ExamTypeDiseaseOrInjury relation = await context.ExamTypeDiseaseOrInjuries
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (relation == null)
                return NotFound();

            errorMessageDto error = await ValidateDto(dto);
            if (error != null)
                return BadRequest(error);

            bool exists = await context.ExamTypeDiseaseOrInjuries.AnyAsync(x => x.Id != id
                && x.examTypeId == dto.examTypeId
                && x.diseaseOrInjuryId == dto.diseaseOrInjuryId
                && x.deleteAt == null);
            if (exists)
                return BadRequest(new errorMessageDto("Ya existe una relacion entre este tipo de examen y enfermedad o lesion."));

            relation.examTypeId = dto.examTypeId;
            relation.diseaseOrInjuryId = dto.diseaseOrInjuryId;
            relation.notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim();
            relation.updateAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public async Task<ActionResult> Delete(long id)
        {
            ExamTypeDiseaseOrInjury relation = await context.ExamTypeDiseaseOrInjuries
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (relation == null)
                return NotFound();

            relation.deleteAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Ok();
        }

        private async Task<ActionResult<examTypeDiseaseOrInjuryDto>> GetById(long id)
        {
            examTypeDiseaseOrInjuryDto dto = await context.ExamTypeDiseaseOrInjuries
                .Include(x => x.examType)
                .Include(x => x.diseaseOrInjury)
                .Where(x => x.Id == id)
                .Select(x => new examTypeDiseaseOrInjuryDto
                {
                    id = x.Id,
                    examTypeId = x.examTypeId,
                    examTypeName = x.examType.name,
                    diseaseOrInjuryId = x.diseaseOrInjuryId,
                    diseaseOrInjuryName = x.diseaseOrInjury.name,
                    notes = x.notes
                })
                .FirstOrDefaultAsync();

            return dto;
        }

        private async Task<errorMessageDto> ValidateDto(examTypeDiseaseOrInjuryCreationDto dto)
        {
            if (dto == null)
                return new errorMessageDto("Debe enviar la informacion de la relacion.");

            bool examTypeExists = await context.ExamTypes.AnyAsync(x => x.Id == dto.examTypeId && x.deleteAt == null);
            if (!examTypeExists)
                return new errorMessageDto("El tipo de examen seleccionado no existe.");

            bool diseaseOrInjuryExists = await context.DiseaseOrInjuries.AnyAsync(x => x.Id == dto.diseaseOrInjuryId && x.deleteAt == null);
            if (!diseaseOrInjuryExists)
                return new errorMessageDto("La enfermedad o lesion seleccionada no existe.");

            if (!string.IsNullOrWhiteSpace(dto.notes) && dto.notes.Trim().Length > 500)
                return new errorMessageDto("Las notas no pueden exceder 500 caracteres.");

            return null;
        }
    }
}
