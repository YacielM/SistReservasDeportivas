using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;
using System.Security.Claims;

namespace SistReservasDeportivas.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext _context;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AccountController(DataContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
                return View(dto);

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o clave incorrectos");
                return View(dto);
            }

            var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.Clave, dto.Clave);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Usuario o clave incorrectos");
                return View(dto);
            }

            // Crear claims para roles
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Email),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("FullName", $"{usuario.Nombre} {usuario.Apellido}")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

            // Opcional: datos para el layout vía sesión
            HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
            HttpContext.Session.SetString("UsuarioApellido", usuario.Apellido);
            HttpContext.Session.SetString("UsuarioRol", usuario.Rol);
            HttpContext.Session.SetString("UsuarioEmail", usuario.Email);

            // Redirigir según rol
            if (usuario.Rol == "Administrador")
                return RedirectToAction("Index", "Usuarios");
            else
                return RedirectToAction("Index", "Reservas");
        }

        // GET: Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
