using System.ComponentModel.DataAnnotations;

namespace SistReservasDeportivas.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        [Required, MaxLength(50)]
        public string Apellido { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string Clave { get; set; }

        [Required, MaxLength(50)]
        public string Rol { get; set; } // Administrador / Empleado

        public string? Avatar { get; set; }
    }
}
