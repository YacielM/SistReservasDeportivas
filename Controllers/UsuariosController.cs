using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;
using System.Security.Claims;

namespace SistReservasDeportivas.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _environment;

        public UsuariosController(DataContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Usuarios
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return View(usuarios);
        }

        // GET: Usuarios/Details/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // GET: Usuarios/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create([Bind("IdUsuario,Nombre,Apellido,Email,Clave,Rol")] Usuario usuario, IFormFile? AvatarFile)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico ya se encuentra registrado.");
            }

            if (ModelState.IsValid)
            {
                // Hashear contraseña
                var passwordHasher = new PasswordHasher<Usuario>();
                usuario.Clave = passwordHasher.HashPassword(usuario, usuario.Clave);

                // Guardar Avatar
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "avatars");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await AvatarFile.CopyToAsync(stream);
                    }
                    usuario.Avatar = fileName;
                }

                _context.Add(usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // GET: Usuarios/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id, string? returnUrl = null)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            ViewData["ReturnUrl"] = returnUrl;
            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Nombre,Apellido,Email,Rol,Avatar")] Usuario usuario, string? nuevaClave, IFormFile? AvatarFile, string? returnUrl = null)
        {
            if (id != usuario.IdUsuario) return NotFound();

            var dbUsuario = await _context.Usuarios.FindAsync(id);
            if (dbUsuario == null) return NotFound();

            // Ignorar la validación de la clave en la edición para que no falle el ModelState
            ModelState.Remove("Clave");

            if (ModelState.IsValid)
            {
                try
                {
                    dbUsuario.Nombre = usuario.Nombre;
                    dbUsuario.Apellido = usuario.Apellido;
                    dbUsuario.Email = usuario.Email;
                    dbUsuario.Rol = usuario.Rol;

                    // Si se ingresó una nueva contraseña, hashearla
                    if (!string.IsNullOrWhiteSpace(nuevaClave))
                    {
                        var passwordHasher = new PasswordHasher<Usuario>();
                        dbUsuario.Clave = passwordHasher.HashPassword(dbUsuario, nuevaClave);
                    }

                    // Reemplazar avatar si se subió uno nuevo
                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "avatars");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(AvatarFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await AvatarFile.CopyToAsync(stream);
                        }
                        dbUsuario.Avatar = fileName;
                    }

                    _context.Update(dbUsuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Usuarios.Any(e => e.IdUsuario == usuario.IdUsuario))
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
            return View(usuario);
        }

        // GET: Usuarios/Delete/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (usuario == null) return NotFound();

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- AUTENTICACIÓN / LOGOUT ---

        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}