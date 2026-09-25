using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;

namespace SistReservasDeportivas.Controllers.Api
{
    [Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}")]
    [Route("api/[controller]")]
    [ApiController]
    public class CanchasApiController : ControllerBase
    {
        private readonly DataContext _context;

        public CanchasApiController(DataContext context)
        {
            _context = context;
        }

        // GET: api/CanchasApi/search?q=futbol
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchCanchas(string? q = "")
        {
            q ??= "";
            var canchas = await _context.Canchas
                .Where(c => string.IsNullOrEmpty(q) || c.Nombre.Contains(q) || c.Tipo.Contains(q))
                .Select(c => new {
                    idCancha = c.IdCancha,
                    nombre = c.Nombre,
                    tipo = c.Tipo,
                    precioHora = c.PrecioHora
                })
                .Take(10)
                .ToListAsync();

            return Ok(canchas);
        }
    }
}