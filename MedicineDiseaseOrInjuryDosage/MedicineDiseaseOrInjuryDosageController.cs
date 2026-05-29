using fletesProyect.MedicineDiseaseOrInjuryDosage.dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.dto;

namespace fletesProyect.MedicineDiseaseOrInjuryDosage
{
    [ApiController]
    [Route("medicine-disease-or-injury-dosages")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR,DOCTOR")]
    public class MedicineDiseaseOrInjuryDosageController : ControllerBase
    {
        private readonly DBProyContext context;

        public MedicineDiseaseOrInjuryDosageController(DBProyContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<medicineDiseaseOrInjuryDosageDto>>> Get([FromQuery] medicineDiseaseOrInjuryDosageQueryDto query)
        {
            IQueryable<MedicineDiseaseOrInjuryDosage> dosages = context.MedicineDiseaseOrInjuryDosages
                .Include(x => x.medicine)
                .Include(x => x.diseaseOrInjury)
                .Where(x => x.deleteAt == null && x.medicine.deleteAt == null && x.diseaseOrInjury.deleteAt == null);

            if (query.medicineId.HasValue)
                dosages = dosages.Where(x => x.medicineId == query.medicineId.Value);

            if (query.diseaseOrInjuryId.HasValue)
                dosages = dosages.Where(x => x.diseaseOrInjuryId == query.diseaseOrInjuryId.Value);

            List<long> diseaseOrInjuryIds = ParseIds(query.diseaseOrInjuryIds);
            if (diseaseOrInjuryIds.Count > 0)
                dosages = dosages.Where(x => diseaseOrInjuryIds.Contains(x.diseaseOrInjuryId));

            return await dosages
                .OrderBy(x => x.diseaseOrInjury.name)
                .ThenBy(x => x.medicine.name)
                .Select(x => new medicineDiseaseOrInjuryDosageDto
                {
                    id = x.Id,
                    medicineId = x.medicineId,
                    medicineName = x.medicine.name,
                    diseaseOrInjuryId = x.diseaseOrInjuryId,
                    diseaseOrInjuryName = x.diseaseOrInjury.name,
                    recommendedAmount = x.recommendedAmount,
                    maximumAmount = x.maximumAmount,
                    notes = x.notes
                })
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<medicineDiseaseOrInjuryDosageDto>> Post([FromBody] medicineDiseaseOrInjuryDosageCreationDto dto)
        {
            errorMessageDto error = await ValidateDto(dto);
            if (error != null)
                return BadRequest(error);

            bool exists = await context.MedicineDiseaseOrInjuryDosages.AnyAsync(x => x.medicineId == dto.medicineId
                && x.diseaseOrInjuryId == dto.diseaseOrInjuryId
                && x.deleteAt == null);
            if (exists)
                return BadRequest(new errorMessageDto("Ya existe una cantidad configurada para este medicamento y enfermedad o lesion."));

            MedicineDiseaseOrInjuryDosage dosage = new MedicineDiseaseOrInjuryDosage
            {
                medicineId = dto.medicineId,
                diseaseOrInjuryId = dto.diseaseOrInjuryId,
                recommendedAmount = dto.recommendedAmount,
                maximumAmount = dto.maximumAmount,
                notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim(),
                createAt = DateTime.UtcNow
            };

            context.MedicineDiseaseOrInjuryDosages.Add(dosage);
            await context.SaveChangesAsync();

            return await GetById(dosage.Id);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Put(long id, [FromBody] medicineDiseaseOrInjuryDosageCreationDto dto)
        {
            MedicineDiseaseOrInjuryDosage dosage = await context.MedicineDiseaseOrInjuryDosages
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (dosage == null)
                return NotFound();

            errorMessageDto error = await ValidateDto(dto);
            if (error != null)
                return BadRequest(error);

            bool exists = await context.MedicineDiseaseOrInjuryDosages.AnyAsync(x => x.Id != id
                && x.medicineId == dto.medicineId
                && x.diseaseOrInjuryId == dto.diseaseOrInjuryId
                && x.deleteAt == null);
            if (exists)
                return BadRequest(new errorMessageDto("Ya existe una cantidad configurada para este medicamento y enfermedad o lesion."));

            dosage.medicineId = dto.medicineId;
            dosage.diseaseOrInjuryId = dto.diseaseOrInjuryId;
            dosage.recommendedAmount = dto.recommendedAmount;
            dosage.maximumAmount = dto.maximumAmount;
            dosage.notes = string.IsNullOrWhiteSpace(dto.notes) ? null : dto.notes.Trim();
            dosage.updateAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public async Task<ActionResult> Delete(long id)
        {
            MedicineDiseaseOrInjuryDosage dosage = await context.MedicineDiseaseOrInjuryDosages
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (dosage == null)
                return NotFound();

            dosage.deleteAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return Ok();
        }

        private async Task<ActionResult<medicineDiseaseOrInjuryDosageDto>> GetById(long id)
        {
            medicineDiseaseOrInjuryDosageDto dto = await context.MedicineDiseaseOrInjuryDosages
                .Include(x => x.medicine)
                .Include(x => x.diseaseOrInjury)
                .Where(x => x.Id == id)
                .Select(x => new medicineDiseaseOrInjuryDosageDto
                {
                    id = x.Id,
                    medicineId = x.medicineId,
                    medicineName = x.medicine.name,
                    diseaseOrInjuryId = x.diseaseOrInjuryId,
                    diseaseOrInjuryName = x.diseaseOrInjury.name,
                    recommendedAmount = x.recommendedAmount,
                    maximumAmount = x.maximumAmount,
                    notes = x.notes
                })
                .FirstOrDefaultAsync();

            return dto;
        }

        private async Task<errorMessageDto> ValidateDto(medicineDiseaseOrInjuryDosageCreationDto dto)
        {
            if (dto == null)
                return new errorMessageDto("Debe enviar la informacion de la cantidad recomendada o maxima.");

            if (dto.recommendedAmount <= 0)
                return new errorMessageDto("La cantidad recomendada debe ser mayor a cero.");

            if (dto.maximumAmount <= 0)
                return new errorMessageDto("La cantidad maxima debe ser mayor a cero.");

            if (dto.recommendedAmount > dto.maximumAmount)
                return new errorMessageDto("La cantidad recomendada no puede ser mayor que la cantidad maxima.");

            bool medicineExists = await context.Medicines.AnyAsync(x => x.Id == dto.medicineId && x.deleteAt == null);
            if (!medicineExists)
                return new errorMessageDto("El medicamento seleccionado no existe.");

            bool diseaseOrInjuryExists = await context.DiseaseOrInjuries.AnyAsync(x => x.Id == dto.diseaseOrInjuryId && x.deleteAt == null);
            if (!diseaseOrInjuryExists)
                return new errorMessageDto("La enfermedad o lesion seleccionada no existe.");

            if (!string.IsNullOrWhiteSpace(dto.notes) && dto.notes.Trim().Length > 500)
                return new errorMessageDto("Las notas no pueden exceder 500 caracteres.");

            return null;
        }

        private static List<long> ParseIds(string? rawIds)
        {
            if (string.IsNullOrWhiteSpace(rawIds))
                return new List<long>();

            return rawIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => long.TryParse(x, out long value) ? value : 0)
                .Where(x => x > 0)
                .Distinct()
                .ToList();
        }
    }
}
