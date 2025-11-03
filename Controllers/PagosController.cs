using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
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
                        Descripcion = r.Cliente!.Nombre + " " + r.Cliente.Apellido +
                                      " - " + r.Cancha!.Nombre + " (" + r.Cancha.Tipo + ")" +
                                      " - " + r.Fecha.ToShortDateString()
                    })
                    .ToList(),
                "IdReserva", "Descripcion", idReservaSeleccionada
            );
        }

        // GET: Pagos
        public async Task<IActionResult> Index()
        {
            var pagos = _context.Pagos
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cliente)
                .Include(p => p.Reserva)
                    .ThenInclude(r => r!.Cancha);

            return View(await pagos.ToListAsync());
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

        // GET: Pagos/Create (si lo mantenés activo)
        public IActionResult Create()
        {
            CargarReservas();
            return View();
        }

        // POST: Pagos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdPago,IdReserva,Importe,Fecha")] Pago pago)
        {
            ModelState.Remove(nameof(Pago.Reserva));

            if (ModelState.IsValid)
            {
                pago.CreadoPor = "Sistema"; // set automático
                _context.Add(pago);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            CargarReservas(pago.IdReserva);
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
        public async Task<IActionResult> Edit(int id, [Bind("IdPago,IdReserva,Importe,Fecha")] Pago pago)
        {
            if (id != pago.IdPago) return NotFound();

            ModelState.Remove(nameof(Pago.Reserva));

            if (ModelState.IsValid)
            {
                try
                {
                    pago.CreadoPor = "Sistema"; // mantener auditoría
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
