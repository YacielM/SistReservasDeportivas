using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;
using Microsoft.AspNetCore.Authorization;

namespace SistReservasDeportivas.Controllers
{
    [Authorize]
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
        public async Task<IActionResult> Create(Cancha cancha, IFormFile? FotoFile)
        {
            if (ModelState.IsValid)
            {
                if (FotoFile != null && FotoFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(FotoFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/canchas", fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await FotoFile.CopyToAsync(stream);
                    }
                    cancha.Foto = fileName;
                }

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
        public async Task<IActionResult> Edit(int id, Cancha cancha, IFormFile? FotoFile)
        {
            if (id != cancha.IdCancha) return NotFound();

            var dbCancha = await _context.Canchas.FindAsync(id);
            if (dbCancha == null) return NotFound();

            if (ModelState.IsValid)
            {
                dbCancha.Nombre = cancha.Nombre;
                dbCancha.Tipo = cancha.Tipo;
                dbCancha.PrecioHora = cancha.PrecioHora;
                dbCancha.Estado = cancha.Estado;

                if (FotoFile != null && FotoFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(FotoFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/canchas", fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await FotoFile.CopyToAsync(stream);
                    }
                    dbCancha.Foto = fileName;
                }

                _context.Update(dbCancha);
                await _context.SaveChangesAsync();
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
        [Authorize(Roles = "Administrador")]
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
