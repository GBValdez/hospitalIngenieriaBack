using System.ComponentModel.DataAnnotations;

namespace project.users.dto
{
    public class credentialsDto
    {
        [Required]
        [EmailAddress]
        public string email { get; set; } = null!;
        [Required]
        public string password { get; set; } = null!;
    }
}