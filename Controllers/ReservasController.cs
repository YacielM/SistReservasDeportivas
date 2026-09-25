using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;
using Microsoft.AspNetCore.Authorization;

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

        // GET: Reservas
        public async Task<IActionResult> Index(string search, string estado, string sortOrder, int page = 1)
        {
            int pageSize = 10;
            var query = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha)
                .AsQueryable();

            // 1. Buscador por Nombre/Apellido de Cliente o Nombre de Cancha
            if (!string.IsNullOrWhiteSpace(search))
            {
                string queryLower = search.Trim().ToLower();
                query = query.Where(r => 
                    (r.Cliente != null && (r.Cliente.Nombre + " " + r.Cliente.Apellido).ToLower().Contains(queryLower)) ||
                    (r.Cancha != null && r.Cancha.Nombre.ToLower().Contains(queryLower)));
            }

            // 2. Filtro por Estado (Activas / Canceladas)
            if (estado == "activas")
            {
                query = query.Where(r => !r.Cancelada);
            }
            else if (estado == "canceladas")
            {
                query = query.Where(r => r.Cancelada);
            }

            // 3. Ordenamiento
            switch (sortOrder)
            {
                case "fecha_asc":
                    query = query.OrderBy(r => r.Fecha).ThenBy(r => r.HoraInicio);
                    break;
                case "monto_desc":
                    query = query.OrderByDescending(r => r.Monto);
                    break;
                case "monto_asc":
                    query = query.OrderBy(r => r.Monto);
                    break;
                case "cliente":
                    query = query.OrderBy(r => r.Cliente.Apellido).ThenBy(r => r.Cliente.Nombre);
                    break;
                case "fecha_desc":
                default:
                    query = query.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraInicio);
                    break;
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

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

        private void CargarCombos(int? idClienteSeleccionado = null, int? idCanchaSeleccionada = null)
        {
            // Si hay un ID seleccionado (ej. en Edit o al volver por error de validación), 
            // solo cargamos ESE elemento específico en el SelectList.
            if (idClienteSeleccionado.HasValue)
            {
                ViewData["IdCliente"] = new SelectList(
                    _context.Clientes.Where(c => c.IdCliente == idClienteSeleccionado)
                        .Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido + " (DNI: " + c.Dni + ")" }),
                    "IdCliente", "NombreCompleto", idClienteSeleccionado);
            }

            if (idCanchaSeleccionada.HasValue)
            {
                ViewData["IdCancha"] = new SelectList(
                    _context.Canchas.Where(c => c.IdCancha == idCanchaSeleccionada)
                        .Select(c => new { c.IdCancha, NombreTipo = c.Nombre + " (" + c.Tipo + ")" }),
                    "IdCancha", "NombreTipo", idCanchaSeleccionada);
            }
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

            if (reserva.HoraFin <= reserva.HoraInicio.Add(TimeSpan.FromHours(1)))
            {
                ModelState.AddModelError("HoraFin", "La reserva debe tener al menos 1 hora de duración.");
            }

            var solapada = await _context.Reservas
                .Where(r => r.IdCancha == reserva.IdCancha && r.Fecha.Date == reserva.Fecha.Date)
                .AnyAsync(r =>
                    (reserva.HoraInicio < r.HoraFin) && (reserva.HoraFin > r.HoraInicio)
                );

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
                _context.Add(reserva);
                await _context.SaveChangesAsync();

                var usuarioNombre = HttpContext.Session.GetString("UsuarioNombre");
                var usuarioApellido = HttpContext.Session.GetString("UsuarioApellido");

                var pago = new Pago
                {
                    IdReserva = reserva.IdReserva,
                    Importe = reserva.Monto,
                    Fecha = DateTime.Now,
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
                .Where(r => r.IdCancha == reserva.IdCancha && r.Fecha.Date == reserva.Fecha.Date && r.IdReserva != reserva.IdReserva)
                .AnyAsync(r =>
                    (reserva.HoraInicio < r.HoraFin) && (reserva.HoraFin > r.HoraInicio)
                );

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Cancha)
                .FirstOrDefaultAsync(r => r.IdReserva == id);

            if (reserva == null) return NotFound();

            reserva.Cancelada = true;

            if (reserva.Cancha != null && reserva.Cancha.Estado == "Ocupada")
            {
                reserva.Cancha.Estado = "Disponible";
            }

            _context.Update(reserva);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}