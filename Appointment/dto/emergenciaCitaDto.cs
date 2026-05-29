using System.ComponentModel.DataAnnotations;

namespace project.Appointment.dto
{
    public class emergenciaCitaDto
    {
        [Required(ErrorMessage = "El DPI es obligatorio.")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "El DPI debe tener 13 caracteres.")]
        public string dpi { get; set; }

        [Required(ErrorMessage = "El motivo de emergencia es obligatorio.")]
        [StringLength(250, ErrorMessage = "El motivo no puede superar 250 caracteres.")]
        public string reason { get; set; }

        public emergenciaPacienteDto? patient { get; set; }
    }

    public class emergenciaPacienteDto
    {
        [Required(ErrorMessage = "El nombre del paciente es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string name { get; set; }

        [Required(ErrorMessage = "La direccion es obligatoria.")]
        [StringLength(200, ErrorMessage = "La direccion no puede exceder los 200 caracteres.")]
        public string direction { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        public DateOnly birthday { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "El sexo es obligatorio.")]
        public long sexId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "La nacionalidad es obligatoria.")]
        public long nationalityId { get; set; }
    }

    public class emergenciaPacienteResultadoDto
    {
        public long id { get; set; }
        public string name { get; set; }
        public string dpi { get; set; }
        public string direction { get; set; }
        public DateOnly birthday { get; set; }
        public long sexId { get; set; }
        public long nationalityId { get; set; }
    }
}
