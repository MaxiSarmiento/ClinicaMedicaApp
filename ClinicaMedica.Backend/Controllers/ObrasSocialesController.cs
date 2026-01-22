using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ObrasSocialesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ObrasSocialesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/ObrasSociales
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q = null)
        {
            var query = _context.ObraSocial.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o => o.Nombre.Contains(q));
            }

            var list = await query
                .OrderBy(o => o.Nombre)
                .Select(o => new
                {
                    o.OSID,
                    o.Nombre
                })
                .ToListAsync();

            return Ok(list);
        }
      
       

        // GET api/obras/buscar?nombre=X
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string nombre)
        {
            var list = await _context.ObraSocial
                .Where(o => o.Nombre.Contains(nombre))
                .ToListAsync();

            return Ok(list);
        }

        // POST api/obras
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Crear([FromBody] ObraSocial obra)
        {
            if (string.IsNullOrWhiteSpace(obra.Nombre))
                return BadRequest("El nombre es obligatorio.");

            _context.ObraSocial.Add(obra);
            await _context.SaveChangesAsync();

            return Ok(obra);
        }

        // PUT api/obras/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [FromBody] ObraSocial edit)
        {
            var obra = await _context.ObraSocial.FindAsync(id);
            if (obra == null) return NotFound("No existe la obra social.");

            obra.Nombre = edit.Nombre;

            await _context.SaveChangesAsync();
            return Ok(obra);
        }

        // DELETE api/obras/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var obra = await _context.ObraSocial.FindAsync(id);
            if (obra == null) return NotFound();

            bool usadaPorPacientes = await _context.Usuarios.AnyAsync(u => u.OSID == id);
            if (usadaPorPacientes)
                return BadRequest("No se puede eliminar: hay pacientes asociados.");

            bool usadaPorDoctores = await _context.DoctoresObrasSociales.AnyAsync(d => d.OSID == id);
            if (usadaPorDoctores)
                return BadRequest("No se puede eliminar: hay doctores asociados.");

            _context.ObraSocial.Remove(obra);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Obra social eliminada." });
        }
    }
}
