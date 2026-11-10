using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers
{
    [Authorize(Roles = "Administrador, Empleado")]
    public class ClientesController : Controller
    {
        private readonly DataContext _context;

        public ClientesController(DataContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .ToListAsync();
            return View(clientes);
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .FirstOrDefaultAsync(m => m.IdCliente == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // GET: Clientes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCliente,Dni,Nombre,Apellido,FechaNacimiento,Telefono,Email")] Cliente cliente)
        {
            // Validar DNI duplicado
            if (await _context.Clientes.AnyAsync(c => c.Dni == cliente.Dni))
            {
                ModelState.AddModelError("Dni", "Ya existe un cliente registrado con este DNI.");
            }

            // Validar Email duplicado
            if (!string.IsNullOrEmpty(cliente.Email) && await _context.Clientes.AnyAsync(c => c.Email == cliente.Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico ya se encuentra registrado.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(cliente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCliente,Dni,Nombre,Apellido,FechaNacimiento,Telefono,Email,SaldoDeudor")] Cliente cliente)
        {
            if (id != cliente.IdCliente) return NotFound();

            // Validar DNI duplicado en otro registro
            if (await _context.Clientes.AnyAsync(c => c.Dni == cliente.Dni && c.IdCliente != id))
            {
                ModelState.AddModelError("Dni", "Ya existe otro cliente registrado con este DNI.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cliente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clientes.Any(e => e.IdCliente == cliente.IdCliente))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                .FirstOrDefaultAsync(m => m.IdCliente == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente != null)
            {
                // Validación de seguridad para evitar romper integridad si tiene reservas
                if (cliente.Reservas != null && cliente.Reservas.Any())
                {
                    ModelState.AddModelError(string.Empty, "No se puede eliminar el cliente porque tiene reservas registradas.");
                    return View("Delete", cliente);
                }

                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}