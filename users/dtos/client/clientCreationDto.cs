using System.ComponentModel.DataAnnotations;

namespace project.users.dto
{
    public class clientCreationDto : clientDtoBase
    {
        [Required(ErrorMessage = "El campo Sexo es requerido")]
        [Range(1, long.MaxValue, ErrorMessage = "El campo Sexo debe ser un valor válido")]
        public long sexId { get; set; }

        [Required(ErrorMessage = "El campo Nacionalidad es requerido")]
        [Range(1, long.MaxValue, ErrorMessage = "El campo Nacionalidad debe ser un valor válido")]
        public long nationalityId { get; set; }

        [Required(ErrorMessage = "El campo Nombre de usuario es requerido")]
        [StringLength(50, ErrorMessage = "El Nombre de usuario no puede exceder los 50 caracteres")]
        public string userName { get; set; }

        [Required(ErrorMessage = "El campo Contraseña es requerido")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La Contraseña debe tener al menos 6 caracteres")]
        public string password { get; set; }
    }
}