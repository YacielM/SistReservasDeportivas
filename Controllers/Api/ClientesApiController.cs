using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistReservasDeportivas.Data;
using SistReservasDeportivas.Models;

namespace SistReservasDeportivas.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}")]
    public class ClientesApiController : ControllerBase
    {
        private readonly DataContext _context;

        public ClientesApiController(DataContext context)
        {
            _context = context;
        }

        // GET: api/ClientesApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetClientes()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .Select(c => new {
                    c.IdCliente,
                    c.Dni,
                    c.Nombre,
                    c.Apellido,
                    c.FechaNacimiento,
                    c.Telefono,
                    c.Email,
                    saldoDeudor = c.SaldoDeudor
                })
                .ToListAsync();

            return Ok(clientes);
        }

        // GET: api/ClientesApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .FirstOrDefaultAsync(c => c.IdCliente == id);

            if (cliente == null) return NotFound();

            return new {
                cliente.IdCliente,
                cliente.Dni,
                cliente.Nombre,
                cliente.Apellido,
                cliente.FechaNacimiento,
                cliente.Telefono,
                cliente.Email,
                saldoDeudor = cliente.SaldoDeudor
            };
        }

        // POST: api/ClientesApi
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCliente), new { id = cliente.IdCliente }, cliente);
        }

        // PUT: api/ClientesApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.IdCliente) return BadRequest();
            _context.Entry(cliente).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/ClientesApi/5
        [Authorize(Roles = "Administrador")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/ClientesApi/search?q=123
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchClientes(string? q = "")
        {
            q ??= "";
            var clientes = await _context.Clientes
                .Include(c => c.Reservas)
                    .ThenInclude(r => r.Pagos)
                .Where(c => string.IsNullOrEmpty(q) || 
                            c.Dni.Contains(q) || 
                            c.Nombre.Contains(q) || 
                            c.Apellido.Contains(q) || 
                            (c.Nombre + " " + c.Apellido).Contains(q))
                .Select(c => new {
                    idCliente = c.IdCliente,
                    dni = c.Dni,
                    nombre = c.Nombre,
                    apellido = c.Apellido,
                    saldoDeudor = c.SaldoDeudor
                })
                .Take(10)
                .ToListAsync();

            return Ok(clientes);
        }
    }
}