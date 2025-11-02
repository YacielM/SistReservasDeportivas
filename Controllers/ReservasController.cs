using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    public class ReservasController : Controller
    {
        private readonly DataContext _context;

        public ReservasController(DataContext context)
        {
            _context = context;
        }

        // GET: Reservas
        public async Task<IActionResult> Index()
        {
            var reservas = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Cancha);
            return View(await reservas.ToListAsync());
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
            ViewData["IdCliente"] = new SelectList(
                _context.Clientes
                    .Select(c => new { c.IdCliente, NombreCompleto = c.Nombre + " " + c.Apellido })
                    .ToList(),
                "IdCliente", "NombreCompleto", idClienteSeleccionado);

            ViewData["IdCancha"] = new SelectList(
                _context.Canchas
                    .Select(c => new { c.IdCancha, NombreTipo = c.Nombre + " (" + c.Tipo + ")" })
                    .ToList(),
                "IdCancha", "NombreTipo", idCanchaSeleccionada);
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
            // Evitar que las referencias de navegación invaliden el modelo
            ModelState.Remove(nameof(Reserva.Cliente));
            ModelState.Remove(nameof(Reserva.Cancha));

            if (ModelState.IsValid)
            {
                _context.Add(reserva);
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

            var reserva = await _context.Reservas.FindAsync(id);
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

            // Evitar que las referencias de navegación invaliden el modelo
            ModelState.Remove(nameof(Reserva.Cliente));
            ModelState.Remove(nameof(Reserva.Cancha));

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
                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
