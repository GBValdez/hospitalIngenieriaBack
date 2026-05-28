using System.ComponentModel.DataAnnotations;
namespace project.users.dto
{
    public class clientDtoBase
{
        [Required(ErrorMessage = "El campo Nombre es requerido")]
        [StringLength(100, ErrorMessage = "El Nombre no puede exceder los 100 caracteres")]
        public string name { get; set; }

        [Required(ErrorMessage = "El campo DPI es requerido")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "El DPI debe tener 13 caracteres")]
        public string dpi { get; set; }

        [Required(ErrorMessage = "El campo Dirección es requerido")]
        [StringLength(200, ErrorMessage = "La Dirección no puede exceder los 200 caracteres")]
        public string direction { get; set; }

        [Required(ErrorMessage = "El campo Fecha de nacimiento es requerido")]
        public DateOnly birthday { get; set; }

        [Required(ErrorMessage = "El campo Correo es requerido")]
        public string email { get; set; } = null!;

        [Required(ErrorMessage = "El campo Teléfono es requerido")]
        [StringLength(10, ErrorMessage = "El campo Teléfono no puede tener más de 10 caracteres")]
        public string phoneNumber { get; set; } = null!;
    }
}