using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    [Authorize(Roles = "Administrador, Empleado")]
    public class CanchasController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _environment;

        public CanchasController(DataContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Canchas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("IdCancha,Nombre,Tipo,PrecioHora,Estado")] Cancha cancha, IFormFile? FotoFile)
        {
            // Validar nombre duplicado
            if (await _context.Canchas.AnyAsync(c => c.Nombre == cancha.Nombre))
            {
                ModelState.AddModelError("Nombre", "Ya existe una cancha registrada con ese nombre.");
            }

            if (ModelState.IsValid)
            {
                if (FotoFile != null && FotoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "canchas");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(FotoFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
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
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id, string? returnUrl = null)
        {
            if (id == null) return NotFound();

            var cancha = await _context.Canchas.FindAsync(id);
            if (cancha == null) return NotFound();

            ViewData["ReturnUrl"] = returnUrl;
            return View(cancha);
        }

        // POST: Canchas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, Cancha cancha, IFormFile? FotoFile, bool eliminarFoto = false, string? returnUrl = null)
        {
            if (id != cancha.IdCancha) return NotFound();

            var dbCancha = await _context.Canchas.FindAsync(id);
            if (dbCancha == null) return NotFound();

            // Validar nombre duplicado en otra cancha
            if (await _context.Canchas.AnyAsync(c => c.Nombre == cancha.Nombre && c.IdCancha != id))
            {
                ModelState.AddModelError("Nombre", "Ya existe otra cancha con ese nombre.");
            }

            // Ignorar la validación de Foto en el ModelState si viene nula
            ModelState.Remove("Foto");
            ModelState.Remove("Reservas");

            if (ModelState.IsValid)
            {
                try
                {
                    dbCancha.Nombre = cancha.Nombre;
                    dbCancha.Tipo = cancha.Tipo;
                    dbCancha.PrecioHora = cancha.PrecioHora;
                    dbCancha.Estado = cancha.Estado;

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "canchas");

                    // Si se marcó el checkbox para eliminar la foto
                    if (eliminarFoto)
                    {
                        if (!string.IsNullOrEmpty(dbCancha.Foto))
                        {
                            var oldFilePath = Path.Combine(uploadsFolder, dbCancha.Foto);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        dbCancha.Foto = null;
                    }
                    // Si se subió una nueva foto
                    else if (FotoFile != null && FotoFile.Length > 0)
                    {
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        // Borrar foto anterior si existía
                        if (!string.IsNullOrEmpty(dbCancha.Foto))
                        {
                            var oldFilePath = Path.Combine(uploadsFolder, dbCancha.Foto);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(FotoFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await FotoFile.CopyToAsync(stream);
                        }
                        dbCancha.Foto = fileName;
                    }

                    _context.Update(dbCancha);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Canchas.Any(e => e.IdCancha == id))
                        return NotFound();
                    else
                        throw;
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(cancha);
        }

        // GET: Canchas/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cancha = await _context.Canchas
                .Include(c => c.Reservas)
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
            var cancha = await _context.Canchas
                .Include(c => c.Reservas)
                .FirstOrDefaultAsync(c => c.IdCancha == id);

            if (cancha != null)
            {
                // Validación: No permitir borrar si tiene reservas asociadas
                if (cancha.Reservas != null && cancha.Reservas.Any())
                {
                    ModelState.AddModelError(string.Empty, "No se puede eliminar la cancha porque posee reservas asociadas.");
                    return View("Delete", cancha);
                }

                _context.Canchas.Remove(cancha);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}