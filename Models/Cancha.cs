using System.ComponentModel.DataAnnotations;

namespace SistReservasDeportivas.Models
{
    public class Cancha
    {
        [Key]  
        public int IdCancha { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Tipo { get; set; } = string.Empty; // Fútbol 5, Tenis, etc.

        [Required]
        public decimal PrecioHora { get; set; }

        public string Estado { get; set; } = "Disponible";

        public string? Foto { get; set; }

        // Relación: una cancha puede tener muchas reservas
        public List<Reserva> Reservas { get; set; } = new();
    }
}
