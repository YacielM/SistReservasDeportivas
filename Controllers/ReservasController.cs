using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class ReservasController : Controller
    {
        private readonly DataContext _context;

        public ReservasController(DataContext context)
        {
            _context = context;
        }

        private void CargarCombos(int? idCliente = null, int? idCancha = null)
        {
            ViewData["IdCliente"] = new SelectList(_context.Clientes, "IdCliente", "Nombre", idCliente);
            ViewData["IdCancha"] = new SelectList(_context.Canchas, "IdCancha", "Nombre", idCancha);
        }

        // GET: Reservas
        public async Task<IActionResult> Index(string search, string estado, string sortOrder, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .AsQueryable();

            // 1. Buscador
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(r =>
                    (r.Cliente != null && (r.Cliente.Nombre + " " + r.Cliente.Apellido).ToLower().Contains(term)) ||
                    (r.Cancha != null && r.Cancha.Nombre.ToLower().Contains(term))
                );
            }

            // 2. Filtro Estado traducible a SQL
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

            // 3. Ordenamiento
            switch (sortOrder)
            {
                case "fecha_asc":
                    query = query.OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio);
                    break;
                case "fecha_desc":
                default:
                    query = query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraInicio);
                    break;
            }

            int totalRegistros = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var reservas = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentEstado"] = estado;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;

            return View(reservas);
        }

        // GET: Reservas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
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
        public async Task<IActionResult> Create([Bind("IdReserva,IdCancha,IdCliente,Fecha,HoraInicio,HoraFin,Monto,CreadoPor")] Reserva reserva)
        {
            ModelState.Remove(nameof(Reserva.Cliente));
            ModelState.Remove(nameof(Reserva.Cancha));

            // 1. Validar bloqueo por Saldo Deudor
            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .FirstOrDefaultAsync(c => c.IdCliente == reserva.IdCliente);

            if (cliente != null && cliente.SaldoDeudor > 0)
            {
                ModelState.AddModelError("", $"El cliente {cliente.Nombre} {cliente.Apellido} registra una deuda de {cliente.SaldoDeudor:C} por turnos vencidos no abonados. Debe regularizar su situación antes de registrar un nuevo turno.");
            }

            // 2. Validar duración mínima
            if (reserva.HoraFin <= reserva.HoraInicio.Add(TimeSpan.FromHours(1)))
            {
                ModelState.AddModelError("HoraFin", "La reserva debe tener al menos 1 hora de duración.");
            }

            // 3. Validar horarios solapados
            var solapada = await _context.Reservas
                .Where(r => r.IdCancha == reserva.IdCancha && r.Fecha.Date == reserva.Fecha.Date && !r.Cancelada)
                .AnyAsync(r => (reserva.HoraInicio < r.HoraFin) && (reserva.HoraFin > r.HoraInicio));

            if (solapada)
            {
                ModelState.AddModelError("", "Ya existe una reserva en ese horario para la misma cancha.");
            }

            // 4. Calcular Monto
            var cancha = await _context.Canchas.FindAsync(reserva.IdCancha);
            if (cancha != null)
            {
                var horas = (reserva.HoraFin - reserva.HoraInicio).TotalHours;
                if (horas > 0)
                {
                    reserva.Monto = cancha.PrecioHora * (decimal)horas;
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(reserva);
                await _context.SaveChangesAsync();

                var usuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
                var usuarioApellido = HttpContext.Session.GetString("UsuarioApellido");

                var pago = new Pago
                {
                    IdReserva = reserva.IdReserva,
                    Importe = reserva.Monto,
                    Fecha = DateTime.Now,
                    EstadoPago = "Pendiente",
                    MetodoPago = null,
                    CreadoPor = (usuarioNombre != null && usuarioApellido != null)
                        ? $"{usuarioNombre} {usuarioApellido}"
                        : "Sistema"
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
                .FirstOrDefaultAsync(m => m.IdReserva == id);

            if (reserva == null) return NotFound();

            CargarCombos(reserva.IdCliente, reserva.IdCancha);
            return View(reserva);
        }

        // POST: Reservas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdReserva,IdCancha,IdCliente,Fecha,HoraInicio,HoraFin,Monto,CreadoPor")] Reserva reserva)
        {
            if (id != reserva.IdReserva) return NotFound();

            ModelState.Remove(nameof(Reserva.Cliente));
            ModelState.Remove(nameof(Reserva.Cancha));

            if (reserva.HoraFin <= reserva.HoraInicio.Add(TimeSpan.FromHours(1)))
            {
                ModelState.AddModelError("HoraFin", "La reserva debe tener al menos 1 hora de duración.");
            }

            var solapada = await _context.Reservas
                .Where(r => r.IdCancha == reserva.IdCancha && r.Fecha.Date == reserva.Fecha.Date && r.IdReserva != reserva.IdReserva && !r.Cancelada)
                .AnyAsync(r => (reserva.HoraInicio < r.HoraFin) && (reserva.HoraFin > r.HoraInicio));

            if (solapada)
            {
                ModelState.AddModelError("", "Ya existe una reserva en ese horario para la misma cancha.");
            }

            var cancha = await _context.Canchas.FindAsync(reserva.IdCancha);
            if (cancha != null)
            {
                var horas = (reserva.HoraFin - reserva.HoraInicio).TotalHours;
                if (horas > 0)
                {
                    reserva.Monto = cancha.PrecioHora * (decimal)horas;
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reserva);

                    // Sincronizar el importe del pago si aún está en estado Pendiente
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

        // POST: Reservas/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva != null)
            {
                reserva.Cancelada = true;
                _context.Update(reserva);

                var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.IdReserva == id);
                if (pago != null)
                {
                    pago.EstadoPago = "Anulado";
                    _context.Update(pago);
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Reservas/Delete/5
        [Authorize(Roles = "Administrador")]
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva != null)
            {
                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}