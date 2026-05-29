using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace fletesProyect.Medicine.dto
{
    public class despachoDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "La cita es obligatoria.")]
        public long appointmentId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos un medicamento.")]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un medicamento.")]
        public List<despachoItemDto> items { get; set; } = new List<despachoItemDto>();
    }

    public class despachoItemDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "La receta es obligatoria.")]
        public long recipeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int amount { get; set; }
    }

    public class inventarioEntradaDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "El medicamento es obligatorio.")]
        public long medicineId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int amount { get; set; }

        public float? unitPrice { get; set; }
        public string reason { get; set; }
    }
}
