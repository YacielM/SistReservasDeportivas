using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        public string Telefono { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public List<Reserva> Reservas { get; set; } = new();

        // Calculo dinámico de deuda sin guardar en base de datos
        [NotMapped]
        public decimal SaldoDeudor => Reservas?
            .Where(r => !r.Cancelada && r.Fecha.Date.Add(r.HoraFin) < DateTime.Now)
            .SelectMany(r => r.Pagos)
            .Where(p => p.EstadoPago == "Pendiente")
            .Sum(p => p.Importe) ?? 0;
    }
}