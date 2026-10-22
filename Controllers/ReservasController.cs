using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly DataContext _context;

        public ReservasController(DataContext context)
        {
            _context = context;
        }

        // GET: Reservas
        public async Task<IActionResult> Index(string search, string estado, string sortOrder, int page = 1)
        {
            int pageSize = 10;
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentEstado"] = estado;
            ViewData["CurrentSort"] = sortOrder;

            var query = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .Include(r => r.Pagos)
                .AsQueryable();

            // 1. Buscador por cliente o cancha
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower().Trim();
                query = query.Where(r =>
                    (r.Cliente != null && (r.Cliente.Nombre + " " + r.Cliente.Apellido).ToLower().Contains(term)) ||
                    (r.Cliente != null && r.Cliente.Dni.Contains(term)) ||
                    (r.Cancha != null && r.Cancha.Nombre.ToLower().Contains(term))
                );
            }

            // 2. Filtro por Estado (compatible con SQL)
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var ahora = DateTime.Now;
                var fechaHoy = ahora.Date;
                var horaActual = ahora.TimeOfDay;

                if (estado == "Cancelada")
                {
                    query = query.Where(r => r.Cancelada);
                }
                else if (estado == "Activa")
                {
                    query = query.Where(r => !r.Cancelada && 
                        (r.Fecha > fechaHoy || (r.Fecha == fechaHoy && r.HoraFin >= horaActual)));
                }
                else if (estado == "Finalizada")
                {
                    query = query.Where(r => !r.Cancelada && 
                        (r.Fecha < fechaHoy || (r.Fecha == fechaHoy && r.HoraFin < horaActual)));
                }
            }

            // 3. Orden
            query = sortOrder switch
            {
                "fecha_asc" => query.OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio),
                _ => query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraInicio)
            };

            int totalRegistros = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalRegistros / pageSize);

            var reservas = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;

            return View(reservas);
        }

        // GET: Reservas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .Include(r => r.Pagos)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        // GET: Reservas/Create
        public IActionResult Create()
        {
            CargarCombos();
            return View();
        }

        // POST: Reservas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdReserva,IdCancha,IdCliente,Fecha,HoraInicio,HoraFin,Monto")] Reserva reserva)
        {
            // 1. Asignar usuario logueado
            reserva.CreadoPor = User.Identity?.Name ?? "Sistema";

            // 2. Control estricto de deuda del cliente
            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .FirstOrDefaultAsync(c => c.IdCliente == reserva.IdCliente);

            if (cliente != null && cliente.SaldoDeudor > 0)
            {
                ModelState.AddModelError("", $"El cliente {cliente.Nombre} {cliente.Apellido} posee una deuda acumulada de {cliente.SaldoDeudor:C}. Debe regularizar su situación antes de realizar una nueva reserva.");
            }

            // 3. Control de solapamiento de horarios en la misma cancha
            bool solapado = await _context.Reservas.AnyAsync(r =>
                r.IdCancha == reserva.IdCancha &&
                r.Fecha.Date == reserva.Fecha.Date &&
                !r.Cancelada &&
                ((reserva.HoraInicio >= r.HoraInicio && reserva.HoraInicio < r.HoraFin) ||
                 (reserva.HoraFin > r.HoraInicio && reserva.HoraFin <= r.HoraFin) ||
                 (reserva.HoraInicio <= r.HoraInicio && reserva.HoraFin >= r.HoraFin)));

            if (solapado)
            {
                ModelState.AddModelError("", "La cancha ya se encuentra reservada en el rango horario seleccionado.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(reserva);
                await _context.SaveChangesAsync();

                // Generar automáticamente el registro de Pago en estado Pendiente
                var pago = new Pago
                {
                    IdReserva = reserva.IdReserva,
                    Importe = reserva.Monto,
                    Fecha = DateTime.Now,
                    MetodoPago = "Sin registrar",
                    EstadoPago = "Pendiente",
                    CreadoPor = User.Identity?.Name ?? "Sistema"
                };
                _context.Pagos.Add(pago);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            CargarCombos(reserva.IdCliente, reserva.IdCancha);
            return View(reserva);
        }

        // GET: Reservas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .FirstOrDefaultAsync(r => r.IdReserva == id);

            if (reserva == null) return NotFound();

            CargarCombos(reserva.IdCliente, reserva.IdCancha);
            return View(reserva);
        }

        // POST: Reservas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdReserva,IdCancha,IdCliente,Fecha,HoraInicio,HoraFin,Monto,Cancelada,CreadoPor")] Reserva reserva)
        {
            if (id != reserva.IdReserva) return NotFound();

            // Validar solapamiento excluyendo la propia reserva
            bool solapado = await _context.Reservas.AnyAsync(r =>
                r.IdReserva != reserva.IdReserva &&
                r.IdCancha == reserva.IdCancha &&
                r.Fecha.Date == reserva.Fecha.Date &&
                !r.Cancelada &&
                ((reserva.HoraInicio >= r.HoraInicio && reserva.HoraInicio < r.HoraFin) ||
                 (reserva.HoraFin > r.HoraInicio && reserva.HoraFin <= r.HoraFin) ||
                 (reserva.HoraInicio <= r.HoraInicio && reserva.HoraFin >= r.HoraFin)));

            if (solapado)
            {
                ModelState.AddModelError("", "La cancha ya posee otro turno reservado en el horario seleccionado.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reserva);

                    // Sincronizar el importe en la tabla de Pagos si sigue Pendiente
                    var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.IdReserva == reserva.IdReserva);
                    if (pago != null && pago.EstadoPago == "Pendiente")
                    {
                        pago.Importe = reserva.Monto;
                        _context.Update(pago);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Reservas.Any(e => e.IdReserva == reserva.IdReserva))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            CargarCombos(reserva.IdCliente, reserva.IdCancha);
            return View(reserva);
        }

        // GET: Reservas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        // POST: Reservas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva != null)
            {
                reserva.Cancelada = true;
                _context.Reservas.Update(reserva);

                // Anular el pago asociado si estaba pendiente
                var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.IdReserva == id);
                if (pago != null && pago.EstadoPago == "Pendiente")
                {
                    pago.EstadoPago = "Anulado";
                    _context.Pagos.Update(pago);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private void CargarCombos(int? idCliente = null, int? idCancha = null)
        {
            ViewData["IdCliente"] = new SelectList(_context.Clientes.Select(c => new {
                c.IdCliente,
                NombreCompleto = c.Nombre + " " + c.Apellido + " (DNI: " + c.Dni + ")"
            }), "IdCliente", "NombreCompleto", idCliente);

            ViewData["IdCancha"] = new SelectList(_context.Canchas.Select(c => new {
                c.IdCancha,
                NombreCompleto = c.Nombre + " (" + c.Tipo + " - " + c.PrecioHora.ToString("C") + "/hs)"
            }), "IdCancha", "NombreCompleto", idCancha);
        }
    }
}