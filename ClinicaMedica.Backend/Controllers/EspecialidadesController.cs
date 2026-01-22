using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EspecialidadesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/especialidades
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Especialidades
                .AsNoTracking()
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            return Ok(list);
        }

        // GET api/especialidades/buscar?nombre=cardio
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string nombre)
        {
            nombre = (nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return Ok(new List<Especialidad>());

            var lower = nombre.ToLower();

            var list = await _context.Especialidades
                .AsNoTracking()
                .Where(e => e.Nombre.ToLower().Contains(lower))
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            return Ok(list);
        }

        // POST api/especialidades
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Crear([FromBody] Especialidad esp)
        {
            if (esp == null)
                return BadRequest("Body inválido.");

            var nombre = (esp.Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return BadRequest("El nombre es obligatorio.");

            //  Evitar duplicados (case-insensitive)
            var lower = nombre.ToLower();
            var existe = await _context.Especialidades
                .AnyAsync(e => e.Nombre.ToLower() == lower);

            if (existe)
                return Conflict("Ya existe una especialidad con ese nombre.");

            esp.Nombre = nombre;

            _context.Especialidades.Add(esp);
            await _context.SaveChangesAsync();

        
            return CreatedAtAction(nameof(GetAll), new { id = esp.EspID }, esp);
        }

        // PUT api/especialidades/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, [FromBody] Especialidad edit)
        {
            if (edit == null)
                return BadRequest("Body inválido.");

            var nuevoNombre = (edit.Nombre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(nuevoNombre))
                return BadRequest("El nombre es obligatorio.");

            var esp = await _context.Especialidades.FindAsync(id);
            if (esp == null)
                return NotFound("No existe la especialidad.");

            //Si cambia, validar duplicado
            var lower = nuevoNombre.ToLower();
            var duplicado = await _context.Especialidades
                .AnyAsync(e => e.EspID != id && e.Nombre.ToLower() == lower);

            if (duplicado)
                return Conflict("Ya existe otra especialidad con ese nombre.");

            esp.Nombre = nuevoNombre;

            await _context.SaveChangesAsync();
            return Ok(esp);
        }

        // DELETE api/especialidades/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var esp = await _context.Especialidades.FindAsync(id);
            if (esp == null) return NotFound("No existe la especialidad.");

            // validar si hay doctores usando esta especialidad y bloquear el delete.

            _context.Especialidades.Remove(esp);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Especialidad eliminada." });
        }
    }
}
