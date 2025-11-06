using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace SistReservasDeportivas.Controllers
{
    [Authorize] // por defecto requiere login
    public class UsuariosController : Controller
    {
        private readonly DataContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public UsuariosController(DataContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        // =====================
        // LOGIN (JWT)
        // =====================
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (usuario == null)
                return Unauthorized("Usuario o clave incorrectos");

            var result = _passwordHasher.VerifyHashedPassword(usuario, usuario.Clave, dto.Clave);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Usuario o clave incorrectos");

            // Generar token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.IdUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuario = new { usuario.IdUsuario, usuario.Nombre, usuario.Apellido, usuario.Email, usuario.Rol }
            });
        }

        // =====================
        // LISTADO (solo Admin)
        // =====================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Usuarios.ToListAsync());
        }

        // =====================
        // CREAR USUARIO
        // =====================
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario usuario, IFormFile? AvatarFile)
        {
            if (ModelState.IsValid)
            {
                // Hash de clave
                usuario.Clave = _passwordHasher.HashPassword(usuario, usuario.Clave);

                // Guardar avatar si se subió
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(AvatarFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatars", fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
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

        // =====================
        // EDITAR USUARIO
        // =====================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Usuario usuario, IFormFile? AvatarFile)
        {
            if (id != usuario.IdUsuario) return NotFound();

            var dbUsuario = await _context.Usuarios.FindAsync(id);
            if (dbUsuario == null) return NotFound();

            if (ModelState.IsValid)
            {
                dbUsuario.Nombre = usuario.Nombre;
                dbUsuario.Apellido = usuario.Apellido;
                dbUsuario.Email = usuario.Email;
                dbUsuario.Rol = usuario.Rol;

                // Si se cambió la clave, re-hashear
                if (!string.IsNullOrEmpty(usuario.Clave))
                {
                    dbUsuario.Clave = _passwordHasher.HashPassword(dbUsuario, usuario.Clave);
                }

                // Avatar nuevo
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(AvatarFile.FileName);
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatars", fileName);
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await AvatarFile.CopyToAsync(stream);
                    }
                    dbUsuario.Avatar = fileName;
                }

                _context.Update(dbUsuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(usuario);
        }

        // =====================
        // DETALLES
        // =====================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

        // =====================
        // ELIMINAR
        // =====================
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario == null) return NotFound();
            return View(usuario);
        }

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
    }
}
