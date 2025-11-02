using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistReservasDeportivas.Models
{
    public class Reserva
    {
        [Key]
        public int IdReserva { get; set; }

        [Required]
        public int IdCancha { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        [Required]
        public decimal Monto { get; set; }

        public string CreadoPor { get; set; } = "Sistema";

        // Navegación

        [ForeignKey("IdCancha")]
        
        public Cancha? Cancha { get; set; } = null!;

        [ForeignKey("IdCliente")]
        public Cliente? Cliente { get; set; } = null!;

        public List<Pago> Pagos { get; set; } = new();
    }
}
