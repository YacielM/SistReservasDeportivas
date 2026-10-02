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
        [Column(TypeName = "decimal(18,2)")]
        public decimal Importe { get; set; }

        public string CreadoPor { get; set; } = "Sistema";

        
        [Required]
        [Display(Name = "Estado del Pago")]
        public string EstadoPago { get; set; } = "Pendiente"; // "Pendiente", "Pagado", "Anulado"

        [Display(Name = "Método de Pago")]
        public string? MetodoPago { get; set; } // "Efectivo", "Mercado Pago", "Transferencia", "Tarjeta"

        // Navegación
        [ForeignKey("IdReserva")]
        public Reserva? Reserva { get; set; }
    }
}