using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    [Authorize(Roles = "Administrador,Empleado")]
    public class PagosController : Controller
    {
        private readonly DataContext _context;

        public PagosController(DataContext context)
        {
            _context = context;
        }

        private void CargarReservas(int? idReservaSeleccionada = null)
        {
            ViewData["IdReserva"] = new SelectList(
                _context.Reservas
                    .Include(r => r.Cliente)
                    .Include(r => r.Cancha)
                    .Select(r => new
                    {
                        r.IdReserva,
                        Descripcion = "#" + r.IdReserva + " - " + r.Cliente!.Nombre + " " + r.Cliente.Apellido +
                                      " - " + r.Cancha!.Nombre + " (" + r.Fecha.ToShortDateString() + ")"
                    })
                    .ToList(),
                "IdReserva", "Descripcion", idReservaSeleccionada
            );
        }

        // GET: Pagos
        public async Task<IActionResult> Index(string search, string estadoPago, string metodoPago, string sortOrder, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Pagos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cliente)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cancha)
                .AsQueryable();

            // 1. Buscador por Cliente o Cancha
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(p =>
                    (p.Reserva != null && p.Reserva.Cliente != null && 
                     (p.Reserva.Cliente.Nombre + " " + p.Reserva.Cliente.Apellido).ToLower().Contains(term)) ||
                    (p.Reserva != null && p.Reserva.Cancha != null && 
                     p.Reserva.Cancha.Nombre.ToLower().Contains(term))
                );
            }

            // 2. Filtro por Estado de Pago
            if (!string.IsNullOrWhiteSpace(estadoPago))
            {
                if (estadoPago == "Pendiente")
                {
                    // Incluye los que dicen "Pendiente" y también los registros viejos con NULL o vacío
                    query = query.Where(p => p.EstadoPago == "Pendiente" || string.IsNullOrEmpty(p.EstadoPago));
                }
                else
                {
                    query = query.Where(p => p.EstadoPago == estadoPago);
                }
            }

            // 3. Filtro por Método de Pago
            if (!string.IsNullOrWhiteSpace(metodoPago))
            {
                query = query.Where(p => p.MetodoPago == metodoPago);
            }

            // 4. Ordenamiento
            switch (sortOrder)
            {
                case "fecha_asc":
                    query = query.OrderBy(p => p.Fecha);
                    break;
                case "importe_desc":
                    query = query.OrderByDescending(p => p.Importe);
                    break;
                case "importe_asc":
                    query = query.OrderBy(p => p.Importe);
                    break;
                case "fecha_desc":
                default:
                    query = query.OrderByDescending(p => p.Fecha);
                    break;
            }

            int totalRegistros = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize);

            var pagos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentSearch"] = search;
            ViewData["CurrentEstado"] = estadoPago;
            ViewData["CurrentMetodo"] = metodoPago;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;

            return View(pagos);
        }

        // GET: Pagos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pago = await _context.Pagos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cliente)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cancha)
                .FirstOrDefaultAsync(m => m.IdPago == id);

            if (pago == null) return NotFound();

            return View(pago);
        }

        // GET: Pagos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pago = await _context.Pagos.FindAsync(id);
            if (pago == null) return NotFound();

            CargarReservas(pago.IdReserva);
            return View(pago);
        }

        // POST: Pagos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdPago,IdReserva,Importe,Fecha,EstadoPago,MetodoPago,CreadoPor")] Pago pago)
        {
            if (id != pago.IdPago) return NotFound();

            ModelState.Remove(nameof(Pago.Reserva));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pago);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Pagos.Any(e => e.IdPago == pago.IdPago))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarReservas(pago.IdReserva);
            return View(pago);
        }

        // GET: Pagos/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pago = await _context.Pagos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cliente)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cancha)
                .FirstOrDefaultAsync(m => m.IdPago == id);

            if (pago == null) return NotFound();

            return View(pago);
        }

        // POST: Pagos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pago = await _context.Pagos.FindAsync(id);
            if (pago != null)
            {
                _context.Pagos.Remove(pago);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}