using System.ComponentModel.DataAnnotations;

namespace SistReservasDeportivas.Models
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required, MaxLength(10)]
        public string Dni { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        public string? Telefono { get; set; }
        public string? Email { get; set; }

        public List<Reserva> Reservas { get; set; } = new();
    }

}
