using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace project.LaboratoryAttendants.dto
{
    public class laboratoryAttendantUpdateDto
    {
        [Required(ErrorMessage = "El campo Nombre es requerido")]
        [StringLength(100, ErrorMessage = "El Nombre no puede exceder los 100 caracteres")]
        public string name { get; set; }

        [Required(ErrorMessage = "El campo DPI es requerido")]
        [StringLength(13, MinimumLength = 13, ErrorMessage = "El DPI debe tener 13 caracteres")]
        public string dpi { get; set; }

        [Required(ErrorMessage = "El campo Direccion es requerido")]
        [StringLength(200, ErrorMessage = "La Direccion no puede exceder los 200 caracteres")]
        public string direction { get; set; }

        [Required(ErrorMessage = "El campo Fecha de nacimiento es requerido")]
        public DateOnly birthday { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "El campo Sexo debe ser valido")]
        public long sexId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "El campo Nacionalidad debe ser valido")]
        public long nationalityId { get; set; }

        [Required(ErrorMessage = "El campo Fecha de contratacion es requerido")]
        public DateTime hiringDate { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos un tipo de examen")]
        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un tipo de examen")]
        public List<long> examTypeIds { get; set; } = new List<long>();

        [Required(ErrorMessage = "El campo Correo es requerido")]
        [EmailAddress(ErrorMessage = "El correo no es valido")]
        public string email { get; set; }

        [Required(ErrorMessage = "El campo Telefono es requerido")]
        [StringLength(10, ErrorMessage = "El Telefono no puede exceder los 10 caracteres")]
        public string phoneNumber { get; set; }

        public bool isActive { get; set; } = true;
    }
}
