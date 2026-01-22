using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctoresObrasSocialesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DoctoresObrasSocialesController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/doctores-obras/{doctorId}
        [HttpGet("{doctorId}")]
        public async Task<IActionResult> GetObrasDelDoctor(int doctorId)
        {
            var obras = await _context.DoctoresObrasSociales
                .Include(d => d.ObraSocial)
                .Where(d => d.DoctorID == doctorId)
                .Select(d => d.ObraSocial)
                .OrderBy(o => o.Nombre)
                .ToListAsync();

            return Ok(obras);
        }

        public class AddOSDto
        {
            public int DoctorID { get; set; }
            public int OSID { get; set; }
        }

        // POST api/doctores-obras/agregar
        [HttpPost("agregar")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Agregar([FromBody] AddOSDto dto)
        {
            var doctor = await _context.Usuarios.FindAsync(dto.DoctorID);
            if (doctor == null || doctor.Rol != "Doctor")
                return BadRequest("El usuario no es un doctor.");

            var existe = await _context.DoctoresObrasSociales
                .AnyAsync(x => x.DoctorID == dto.DoctorID && x.OSID == dto.OSID);

            if (existe)
                return BadRequest("El doctor ya atiende esta obra social.");

            _context.DoctoresObrasSociales.Add(new DoctorObraSocial
            {
                DoctorID = dto.DoctorID,
                OSID = dto.OSID
            });

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Obra social agregada al doctor." });
        }

        // DELETE api/doctores-obras/{doctorId}/{osid}
        [HttpDelete("{doctorId}/{osid}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Eliminar(int doctorId, int osid)
        {
            var rel = await _context.DoctoresObrasSociales
                .FirstOrDefaultAsync(x => x.DoctorID == doctorId && x.OSID == osid);

            if (rel == null) return NotFound();

            _context.DoctoresObrasSociales.Remove(rel);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Obra social eliminada del doctor." });
        }

    }
}
