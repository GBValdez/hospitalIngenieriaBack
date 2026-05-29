using System.ComponentModel.DataAnnotations;

namespace project.Exams.dto
{
    public class inicioExamDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "El examen es obligatorio.")]
        public long examId { get; set; }
    }
}
