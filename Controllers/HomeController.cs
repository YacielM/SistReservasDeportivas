using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;
using System.Diagnostics;

namespace SistReservasDeportivas.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly DataContext _context;

        public HomeController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ahora = DateTime.Now;
            var hoy = ahora.Date;
            var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

            // 1. Reservas para el día de hoy
            var reservasHoy = await _context.Reservas
                .Where(r => r.Fecha.Date == hoy && !r.Cancelada)
                .CountAsync();

            // 2. Recaudación del mes actual (Pagos confirmados)
            var recaudacionMes = await _context.Pagos
                .Where(p => p.EstadoPago == "Pagado" && p.Fecha >= inicioMes)
                .SumAsync(p => p.Importe);

            // 3. Cantidad de Clientes Deudores
            var clientes = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .ToListAsync();

            var totalDeudores = clientes.Count(c => c.SaldoDeudor > 0);

            // 4. Próximas 5 Reservas
            var proximasReservas = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .Where(r => !r.Cancelada && r.Fecha.Date >= hoy)
                .OrderBy(r => r.Fecha)
                .ThenBy(r => r.HoraInicio)
                .Take(5)
                .ToListAsync();

            ViewData["ReservasHoy"] = reservasHoy;
            ViewData["RecaudacionMes"] = recaudacionMes;
            ViewData["TotalDeudores"] = totalDeudores;

            return View(proximasReservas);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}