using System.ComponentModel.DataAnnotations;

namespace project.Exams.dto
{
    public class finalizarExamDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "El examen es obligatorio.")]
        public long examId { get; set; }

        [Required(ErrorMessage = "El resultado es obligatorio.")]
        [StringLength(1000, ErrorMessage = "El resultado no puede superar 1000 caracteres.")]
        public string results { get; set; }
    }
}
