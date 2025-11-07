using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;

namespace SistReservasDeportivas.Controllers.Api
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class CanchasApiController : ControllerBase
    {
        private readonly DataContext _context;

        public CanchasApiController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchCanchas(string q)
        {
            var canchas = await _context.Canchas
                .Where(c => c.Nombre.Contains(q) || c.Tipo.Contains(q))
                .Select(c => new {
                    id = c.IdCancha,
                    nombre = c.Nombre + " (" + c.Tipo + ")"
                })
                .Take(10)
                .ToListAsync();

            return Ok(canchas);
        }
    }
}
