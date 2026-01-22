using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctoresEspecialidadesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DoctoresEspecialidadesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/DoctoresEspecialidades/{doctorId}
        [HttpGet("{doctorId}")]
        public async Task<IActionResult> GetEspecialidadesDelDoctor(int doctorId)
        {
            var list = await _context.DoctoresEspecialidades
                .Include(x => x.Especialidad)
                .Where(x => x.DoctorID == doctorId)
                .Select(x => x.Especialidad)
                .OrderBy(e => e.Nombre)
                .ToListAsync();

            return Ok(list);
        }

        public class AddEspecialidadDto
        {
            public int DoctorID { get; set; }
            public int EspID { get; set; }
        }

        // POST api/DoctoresEspecialidades/agregar
        [HttpPost("agregar")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Agregar([FromBody] AddEspecialidadDto dto)
        {
            var doctor = await _context.Usuarios.FindAsync(dto.DoctorID);
            if (doctor == null || doctor.Rol != "Doctor")
                return BadRequest("Doctor inválido.");

            var existe = await _context.DoctoresEspecialidades
                .AnyAsync(x => x.DoctorID == dto.DoctorID && x.EspID == dto.EspID);

            if (existe)
                return BadRequest("El doctor ya tiene esta especialidad.");

            _context.DoctoresEspecialidades.Add(new DoctorEspecialidad
            {
                DoctorID = dto.DoctorID,
                EspID = dto.EspID
            });

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Especialidad agregada al doctor." });
        }

        // DELETE api/DoctoresEspecialidades/{doctorId}/{espId}
        [HttpDelete("{doctorId}/{espId}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Eliminar(int doctorId, int espId)
        {
            var rel = await _context.DoctoresEspecialidades
                .FirstOrDefaultAsync(x => x.DoctorID == doctorId && x.EspID == espId);

            if (rel == null)
                return NotFound();

            _context.DoctoresEspecialidades.Remove(rel);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Especialidad eliminada del doctor." });
        }
    }
}
