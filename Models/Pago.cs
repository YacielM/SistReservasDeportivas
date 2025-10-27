using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistReservasDeportivas.Models
{
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }

        [Required]
        public int IdReserva { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public decimal Importe { get; set; }

        public string CreadoPor { get; set; } = "Sistema";

        // Navegación
        [ForeignKey("IdReserva")]
        public Reserva Reserva { get; set; } = null!;
    }
}
