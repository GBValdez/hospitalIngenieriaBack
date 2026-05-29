using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace fletesProyect.Medicine.dto
{
    public class despachoDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "La cita es obligatoria.")]
        public long appointmentId { get; set; }

        [Required(ErrorMessage = "El DPI del paciente es obligatorio.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "El DPI debe tener 13 caracteres.")]
        public string dpi { get; set; }

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
