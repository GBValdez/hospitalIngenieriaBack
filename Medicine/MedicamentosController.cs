using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using fletesProyect.Medicine.dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.utils.dto;
using DispatchModel = fletesProyect.Dispatch.Dispatch;
using MedicineInventoryModel = fletesProyect.MedicineInventory.MedicineInventory;
using MedicineInventoryMovementModel = fletesProyect.MedicineInventoryMovement.MedicineInventoryMovement;

namespace fletesProyect.Medicine
{
    [ApiController]
    [Route("api/medicamentos")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "PHARMACY_ATTENDANT,ADMINISTRATOR")]
    public class MedicamentosController : ControllerBase
    {
        private const string MovementIn = "ENTRADA";
        private const string MovementOut = "SALIDA";
        private readonly DBProyContext context;

        public MedicamentosController(DBProyContext context)
        {
            this.context = context;
        }

        [HttpGet("receta/{id}")]
        public async Task<ActionResult<recetaDespachoDto>> ObtenerReceta(long id, [FromQuery] string dpi)
        {
            if (string.IsNullOrWhiteSpace(dpi))
                return BadRequest(new errorMessageDto("Debe ingresar el DPI del paciente."));

            var appointment = await context.Appointments
                .Include(x => x.patient)
                .Include(x => x.doctor)
                .FirstOrDefaultAsync(x => x.Id == id && x.deleteAt == null);

            if (appointment == null)
                return NotFound(new errorMessageDto("No se encontro la receta solicitada."));

            if (appointment.patient?.dpi != dpi.Trim())
                return NotFound(new errorMessageDto("No se encontro una receta para la cita y DPI ingresados."));

            List<recetaDespachoItemDto> medicines = await BuildRecipeItems(id);
            if (medicines.Count == 0)
                return NotFound(new errorMessageDto("La cita no tiene medicamentos recetados."));

            return new recetaDespachoDto
            {
                appointmentId = appointment.Id,
                appointmentReason = appointment.reason,
                patientId = appointment.patientId,
                patientName = appointment.patient?.name,
                patientDpi = appointment.patient?.dpi,
                doctorId = appointment.doctorId,
                doctorName = appointment.doctor?.name,
                appointmentDate = appointment.startDate,
                medicines = medicines
            };
        }

        [HttpPost("despachar")]
        public async Task<ActionResult> DespacharMedicamento([FromBody] despachoDto dto)
        {
            if (dto == null)
                return BadRequest(new errorMessageDto("Debe enviar la informacion del despacho."));

            if (string.IsNullOrWhiteSpace(dto.dpi))
                return BadRequest(new errorMessageDto("Debe ingresar el DPI del paciente."));

            if (dto.items == null || dto.items.Count == 0)
                return BadRequest(new errorMessageDto("Debe seleccionar al menos un medicamento."));

            bool appointmentMatchesPatient = await context.Appointments
                .Include(x => x.patient)
                .AnyAsync(x => x.Id == dto.appointmentId
                    && x.deleteAt == null
                    && x.patient.dpi == dto.dpi.Trim());
            if (!appointmentMatchesPatient)
                return BadRequest(new errorMessageDto("La cita y el DPI del paciente no coinciden."));

            List<long> recipeIds = dto.items.Select(x => x.recipeId).Distinct().ToList();
            if (recipeIds.Count != dto.items.Count)
                return BadRequest(new errorMessageDto("No puede repetir medicamentos de la misma receta en el despacho."));

            var recipes = await context.Recipes
                .Include(x => x.medicine)
                .Where(x => recipeIds.Contains(x.Id)
                    && x.appointmentId == dto.appointmentId
                    && x.deleteAt == null
                    && x.medicine.deleteAt == null)
                .ToListAsync();

            if (recipes.Count != recipeIds.Count)
                return BadRequest(new errorMessageDto("Uno o mas medicamentos no pertenecen a la receta seleccionada."));

            foreach (despachoItemDto item in dto.items)
            {
                if (item.amount <= 0)
                    return BadRequest(new errorMessageDto("La cantidad a despachar debe ser mayor a cero."));

                var recipe = recipes.First(x => x.Id == item.recipeId);
                int prescribedAmount = CalculatePrescribedAmount(recipe.days, recipe.timeLimit);
                int alreadyDispatched = await context.Dispatchs
                    .Where(x => x.recipeId == recipe.Id && x.deleteAt == null)
                    .SumAsync(x => x.amount);
                int pendingAmount = prescribedAmount - alreadyDispatched;
                if (item.amount > pendingAmount)
                    return BadRequest(new errorMessageDto($"La cantidad de {recipe.medicine.name} excede lo pendiente de la receta."));

                int stock = await GetCurrentStock(recipe.medicineId);
                if (item.amount > stock)
                    return BadRequest(new errorMessageDto($"Inventario insuficiente para {recipe.medicine.name}. Disponible: {stock}."));
            }

            DateTime now = DateTime.UtcNow;
            foreach (despachoItemDto item in dto.items)
            {
                var recipe = recipes.First(x => x.Id == item.recipeId);
                MedicineInventoryModel inventory = await GetOrCreateInventory(recipe.medicineId);
                int previousStock = inventory.stock;
                inventory.stock -= item.amount;

                DispatchModel dispatch = new DispatchModel
                {
                    recipeId = recipe.Id,
                    amount = item.amount,
                    createAt = now
                };
                context.Dispatchs.Add(dispatch);
                await context.SaveChangesAsync();

                context.MedicineInventoryMovements.Add(new MedicineInventoryMovementModel
                {
                    medicineId = recipe.medicineId,
                    movementType = MovementOut,
                    amount = item.amount,
                    previousStock = previousStock,
                    newStock = inventory.stock,
                    unitPrice = recipe.medicine.price,
                    reason = $"Despacho de receta de cita #{dto.appointmentId}",
                    dispatchId = dispatch.Id,
                    registeredByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    createAt = now
                });
            }

            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("inventario")]
        public async Task<ActionResult<List<inventarioMedicinaDto>>> GetInventario()
        {
            return await context.Medicines
                .Where(x => x.deleteAt == null)
                .GroupJoin(
                    context.MedicineInventories,
                    medicine => medicine.Id,
                    inventory => inventory.medicineId,
                    (medicine, inventory) => new inventarioMedicinaDto
                    {
                        medicineId = medicine.Id,
                        medicineName = medicine.name,
                        price = medicine.price,
                        stock = inventory.Select(x => x.stock).FirstOrDefault()
                    })
                .OrderBy(x => x.medicineName)
                .ToListAsync();
        }

        [HttpPost("inventario/entrada")]
        public async Task<ActionResult> RegistrarEntrada([FromBody] inventarioEntradaDto dto)
        {
            if (dto == null)
                return BadRequest(new errorMessageDto("Debe enviar la informacion de la entrada."));

            if (dto.amount <= 0)
                return BadRequest(new errorMessageDto("La cantidad debe ser mayor a cero."));

            var medicine = await context.Medicines.FirstOrDefaultAsync(x => x.Id == dto.medicineId && x.deleteAt == null);
            if (medicine == null)
                return BadRequest(new errorMessageDto("El medicamento seleccionado no existe."));

            MedicineInventoryModel inventory = await GetOrCreateInventory(dto.medicineId);
            int previousStock = inventory.stock;
            inventory.stock += dto.amount;
            DateTime now = DateTime.UtcNow;

            context.MedicineInventoryMovements.Add(new MedicineInventoryMovementModel
            {
                medicineId = dto.medicineId,
                movementType = MovementIn,
                amount = dto.amount,
                previousStock = previousStock,
                newStock = inventory.stock,
                unitPrice = dto.unitPrice ?? medicine.price,
                reason = string.IsNullOrWhiteSpace(dto.reason) ? "Entrada de inventario" : dto.reason.Trim(),
                registeredByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                createAt = now
            });

            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("movimientos")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMINISTRATOR")]
        public async Task<ActionResult<List<inventarioMovimientoDto>>> GetMovimientos([FromQuery] long? medicineId)
        {
            var query = context.MedicineInventoryMovements
                .Include(x => x.medicine)
                .Include(x => x.registeredByUser)
                .Where(x => x.deleteAt == null);

            if (medicineId.HasValue)
                query = query.Where(x => x.medicineId == medicineId.Value);

            return await query
                .OrderByDescending(x => x.Id)
                .Select(x => new inventarioMovimientoDto
                {
                    id = x.Id,
                    medicineId = x.medicineId,
                    medicineName = x.medicine.name,
                    movementType = x.movementType,
                    amount = x.amount,
                    previousStock = x.previousStock,
                    newStock = x.newStock,
                    unitPrice = x.unitPrice,
                    reason = x.reason,
                    dispatchId = x.dispatchId,
                    createAt = x.createAt,
                    registeredByUserName = x.registeredByUser != null ? x.registeredByUser.UserName : null
                })
                .Take(200)
                .ToListAsync();
        }

        private async Task<List<recetaDespachoItemDto>> BuildRecipeItems(long appointmentId)
        {
            List<long> recipeIds = await context.Recipes
                .Where(x => x.appointmentId == appointmentId && x.deleteAt == null)
                .Select(x => x.Id)
                .ToListAsync();

            Dictionary<long, int> dispatchedByRecipe = await context.Dispatchs
                .Where(x => recipeIds.Contains(x.recipeId) && x.deleteAt == null)
                .GroupBy(x => x.recipeId)
                .Select(x => new { recipeId = x.Key, amount = x.Sum(y => y.amount) })
                .ToDictionaryAsync(x => x.recipeId, x => x.amount);

            Dictionary<long, int> stockByMedicine = await context.MedicineInventories
                .ToDictionaryAsync(x => x.medicineId, x => x.stock);

            var recipes = await context.Recipes
                .Include(x => x.medicine)
                .Where(x => x.appointmentId == appointmentId && x.deleteAt == null && x.medicine.deleteAt == null)
                .OrderBy(x => x.Id)
                .ToListAsync();

            return recipes
                .Select(x =>
                {
                    int prescribedAmount = CalculatePrescribedAmount(x.days, x.timeLimit);
                    int alreadyDispatched = dispatchedByRecipe.ContainsKey(x.Id) ? dispatchedByRecipe[x.Id] : 0;
                    return new recetaDespachoItemDto
                    {
                        recipeId = x.Id,
                        medicineId = x.medicineId,
                        medicineName = x.medicine.name,
                        days = x.days,
                        timeLimit = x.timeLimit,
                        prescribedAmount = prescribedAmount,
                        alreadyDispatched = alreadyDispatched,
                        pendingAmount = prescribedAmount - alreadyDispatched,
                        availableStock = stockByMedicine.ContainsKey(x.medicineId) ? stockByMedicine[x.medicineId] : 0,
                        price = x.medicine.price
                    };
                })
                .ToList();
        }

        private static int CalculatePrescribedAmount(int days, int timeLimit)
        {
            if (days <= 0 || timeLimit <= 0)
                return 0;

            return days * (int)Math.Ceiling(24m / timeLimit);
        }

        private async Task<int> GetCurrentStock(long medicineId)
        {
            MedicineInventoryModel inventory = await GetOrCreateInventory(medicineId);
            return inventory.stock;
        }

        private async Task<MedicineInventoryModel> GetOrCreateInventory(long medicineId)
        {
            MedicineInventoryModel inventory = await context.MedicineInventories
                .FirstOrDefaultAsync(x => x.medicineId == medicineId);

            if (inventory != null)
                return inventory;

            inventory = new MedicineInventoryModel
            {
                medicineId = medicineId,
                stock = 0,
                createAt = DateTime.UtcNow
            };
            context.MedicineInventories.Add(inventory);
            await context.SaveChangesAsync();
            return inventory;
        }
    }
}
