using ClinicaMedica.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DoctoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET api/Doctores?especialidadId=1
        [HttpGet]
        [Authorize(Roles = "Paciente,Doctor,Admin")]
        public async Task<IActionResult> Listar([FromQuery] int? especialidadId = null)
        {
            var q = _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Rol == "Doctor");

            if (especialidadId.HasValue && especialidadId.Value > 0)
            {
                q = q.Where(d => d.Especialidades.Any(e => e.EspID == especialidadId.Value));
            }

            var list = await q
                .OrderBy(d => d.Apellido).ThenBy(d => d.Nombre)
                .Select(d => new
                {
                    d.Id,
                    d.Nombre,
                    d.Apellido,
                    Especialidades = d.Especialidades.Select(e => e.Especialidad.Nombre).ToList()
                })
                .ToListAsync();

            return Ok(list);
        }
        // GET api/doctores/disponibles?pacienteId=1
        [HttpGet("disponibles")]
        public async Task<IActionResult> Disponibles([FromQuery] int pacienteId)
        {
            var paciente = await _context.Usuarios.FindAsync(pacienteId);
            if (paciente == null || paciente.Rol != "Paciente")
                return BadRequest("Paciente inválido.");

            // Caso 1: sin obra social
            if (paciente.OSID == null)
            {
                var docs = await _context.Usuarios
                    .Where(u => u.Rol == "Doctor")
                    .Select(d => new
                    {
                        d.Id,
                        d.Nombre,
                        d.Apellido,
                        AtiendeObraSocial = false,
                        Advertencia = "Este turno deberá ser abonado al atender."
                    })
                    .OrderBy(d => d.Apellido)
                    .ToListAsync();

                return Ok(docs);
            }

            // Caso 2: con obra social → filtrar doctores que atienden esa OS
            var compatibles =
                await (from d in _context.Usuarios
                       join rel in _context.DoctoresObrasSociales
                            on d.Id equals rel.DoctorID
                       where d.Rol == "Doctor"
                             && rel.OSID == paciente.OSID
                       select new
                       {
                           d.Id,
                           d.Nombre,
                           d.Apellido,
                           AtiendeObraSocial = true,
                           Advertencia = ""
                       })
                .OrderBy(d => d.Apellido)
                .ToListAsync();

            return Ok(compatibles);
        }

    }

}
