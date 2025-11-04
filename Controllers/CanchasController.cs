using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    public class CanchasController : Controller
    {
        private readonly DataContext _context;

        public CanchasController(DataContext context)
        {
            _context = context;
        }

        // GET: Canchas
        public async Task<IActionResult> Index()
        {
            var canchas = await _context.Canchas
                .Include(c => c.Reservas)
                .ToListAsync();

            var ahora = DateTime.Now;

            foreach (var cancha in canchas)
            {
                // si está en mantenimiento, no tocar
                if (cancha.Estado == "Mantenimiento") continue;

                var reservaEnCurso = cancha.Reservas.Any(r =>
                    !r.Cancelada &&
                    r.Fecha.Date == ahora.Date &&
                    r.HoraInicio <= ahora.TimeOfDay &&
                    r.HoraFin > ahora.TimeOfDay);

                cancha.Estado = reservaEnCurso ? "Ocupada" : "Disponible";
            }

            await _context.SaveChangesAsync();

            return View(canchas);
        }


        // GET: Canchas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cancha = await _context.Canchas
                .FirstOrDefaultAsync(m => m.IdCancha == id);
            if (cancha == null) return NotFound();

            return View(cancha);
        }

        // GET: Canchas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Canchas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCancha,Nombre,Tipo,PrecioHora,Estado")] Cancha cancha)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cancha);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cancha);
        }

        // GET: Canchas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return NotFound();

            return View(cancha);
        }

        // POST: Canchas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCancha,Nombre,Tipo,PrecioHora,Estado")] Cancha cancha)
        {
            if (id != cancha.IdCancha) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cancha);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Canchas.Any(e => e.IdCancha == cancha.IdCancha))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cancha);
        }

        // GET: Canchas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cancha = await _context.Canchas
                .FirstOrDefaultAsync(m => m.IdCancha == id);
            if (cancha == null) return NotFound();

            return View(cancha);
        }

        // POST: Canchas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha != null)
            {
                _context.Canchas.Remove(cancha);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
