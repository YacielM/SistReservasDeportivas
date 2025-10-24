using System.ComponentModel.DataAnnotations;

namespace SistReservasDeportivas.Models
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required, MaxLength(10)]
        public string Dni { get; set; }

        [Required, MaxLength(150)]
        public string NombreCompleto { get; set; }

        public string? Telefono { get; set; }
        public string? Email { get; set; }

        // Relación: un cliente puede tener muchas reservas
        public List<Reserva> Reservas { get; set; }
    }
}
