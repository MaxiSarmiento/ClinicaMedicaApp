using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorAgendasController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DoctorAgendasController(AppDbContext context) => _context = context;

      
        private int? GetUserId()
        {
            var idStr =
                User.FindFirst("Id")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            return int.TryParse(idStr, out var id) ? id : null;
        }

      
        // DTO que consume el FRONT para la agenda del doctor

        public class TurnoDoctorDto
        {
            public int Id { get; set; }
            public DateTime FechaHora { get; set; }
            public int DuracionMinutos { get; set; }
            public string Estado { get; set; } = "Libre";

            public string? PacienteNombre { get; set; }
            public string? PacienteApellido { get; set; }
        }

      
        // REGLAS DE AGENDA (DoctorAgenda): por DoctorId (Admin/Doctor)
      

        // GET api/DoctorAgendas/doctor/1009
        [HttpGet("doctor/{doctorId:int}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GetByDoctor(int doctorId)
        {
            // Si es Doctor, solo puede consultar su propio doctorId
            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                if (me.Value != doctorId) return Forbid();
            }

            var list = await _context.DoctorAgenda
                .AsNoTracking()
                .Where(a => a.DoctorID == doctorId)
                .OrderBy(a => a.DiaSemana)
                .ThenBy(a => a.HoraInicio)
                .Select(a => new
                {
                    a.Id,
                    a.DoctorID,
                    a.DiaSemana,
                    a.HoraInicio,
                    a.HoraFin,
                    a.DuracionMinutos
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET api/DoctorAgendas/1009 (compat con front viejo)
        [HttpGet("{doctorId:int}")]
        [Authorize(Roles = "Doctor,Admin")]
        public Task<IActionResult> GetByDoctor_Compat(int doctorId)
            => GetByDoctor(doctorId);

       
        // TURNOS (Turnos): agenda del doctor logueado
   

       
        [HttpGet("agenda")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetAgendaDoctor(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var me = GetUserId();
            if (me == null) return Forbid();

            var doctorId = me.Value;

            var q = _context.Turnos
                .AsNoTracking()
                .Where(t => t.IdDoctor == doctorId);

            if (desde.HasValue) q = q.Where(t => t.FechaHora >= desde.Value);
            if (hasta.HasValue) q = q.Where(t => t.FechaHora <= hasta.Value);

            var turnos = await q
                .OrderBy(t => t.FechaHora)
                .Select(t => new TurnoDoctorDto
                {
                    Id = t.Id,
                    FechaHora = t.FechaHora,
                    DuracionMinutos = t.DuracionMinutos,
                    Estado = t.Estado ?? "Libre",

                    PacienteNombre = t.IdPaciente == null ? null : t.Paciente!.Nombre,
                    PacienteApellido = t.IdPaciente == null ? null : t.Paciente!.Apellido
                })
                .ToListAsync();

            return Ok(turnos);
        }

      
        // GENERAR TURNOS "VACÍOS" (LIBRES) A PARTIR DE DoctorAgenda


        public class GenerarTurnosRequest
        {
            public DateTime Desde { get; set; }
            public DateTime Hasta { get; set; }
        }

        //  POST api/DoctorAgendas/generar
        [HttpPost("generar")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GenerarTurnos([FromBody] GenerarTurnosRequest req)
        {
            if (req.Hasta.Date < req.Desde.Date)
                return BadRequest("Rango inválido.");

            var me = GetUserId();
            if (me == null) return Forbid();
            var doctorId = me.Value;

            var reglas = await _context.DoctorAgenda
                .AsNoTracking()
                .Where(a => a.DoctorID == doctorId)
                .ToListAsync();

            if (reglas.Count == 0)
                return BadRequest("El doctor no tiene agenda configurada.");

            var desde = req.Desde.Date;
            var hasta = req.Hasta.Date.AddDays(1).AddTicks(-1);

            // Turnos ya existentes (evitar duplicados)
            var existentes = await _context.Turnos
                .AsNoTracking()
                .Where(t => t.IdDoctor == doctorId && t.FechaHora >= desde && t.FechaHora <= hasta)
                .Select(t => t.FechaHora)
                .ToListAsync();

            var existentesSet = existentes.ToHashSet();
            var nuevos = new List<Turno>();

            for (var dia = desde; dia <= req.Hasta.Date; dia = dia.AddDays(1))
            {
                // Si DoctorAgenda usa 1..7 (Lunes..Domingo)
                int diaSemana = ((int)dia.DayOfWeek + 6) % 7 + 1;

                var reglasDelDia = reglas.Where(r => r.DiaSemana == diaSemana);
                foreach (var r in reglasDelDia)
                {
                    var inicio = dia.Add(r.HoraInicio);
                    var fin = dia.Add(r.HoraFin);

                    var slot = inicio;
                    while (slot.AddMinutes(r.DuracionMinutos) <= fin)
                    {
                        if (!existentesSet.Contains(slot))
                        {
                            nuevos.Add(new Turno
                            {
                                IdDoctor = doctorId,
                                IdPaciente = null,
                                FechaHora = slot,
                                DuracionMinutos = r.DuracionMinutos,
                                Estado = "Libre"
                            });

                            existentesSet.Add(slot);
                        }

                        slot = slot.AddMinutes(r.DuracionMinutos);
                    }
                }
            }

            if (nuevos.Count == 0)
                return Ok(new { mensaje = "No había turnos para crear (ya existían).", creados = 0 });

            _context.Turnos.AddRange(nuevos);
            var creados = await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Turnos generados OK", creados = nuevos.Count });
        }

     
        // POST: crear regla de agenda

        [HttpPost]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Crear([FromBody] DoctorAgenda agenda)
        {
          
            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                agenda.DoctorID = me.Value;
            }

            // ===== Validaciones básicas =====
            if (agenda.DoctorID <= 0)
                return BadRequest("DoctorID inválido.");

            if (agenda.DiaSemana < 1 || agenda.DiaSemana > 7)
                return BadRequest("DiaSemana debe ser 1..7 (Lunes..Domingo).");

            if (agenda.DuracionMinutos <= 0)
                return BadRequest("DuracionMinutos inválido.");

            if (agenda.HoraFin <= agenda.HoraInicio)
                return BadRequest("HoraFin debe ser mayor que HoraInicio.");

            if (agenda.HoraInicio.Add(TimeSpan.FromMinutes(agenda.DuracionMinutos)) > agenda.HoraFin)
                return BadRequest("Con esa duración no entra ningún turno dentro del horario.");

            // ===== Evitar duplicado exacto =====
            var existeIgual = await _context.DoctorAgenda.AnyAsync(a =>
                a.DoctorID == agenda.DoctorID &&
                a.DiaSemana == agenda.DiaSemana &&
                a.HoraInicio == agenda.HoraInicio &&
                a.HoraFin == agenda.HoraFin &&
                a.DuracionMinutos == agenda.DuracionMinutos);

            if (existeIgual)
                return BadRequest("Ya existe una regla idéntica para ese día.");

            // ===== Evitar solapamiento =====
            var solapa = await _context.DoctorAgenda.AnyAsync(a =>
                a.DoctorID == agenda.DoctorID &&
                a.DiaSemana == agenda.DiaSemana &&
                agenda.HoraInicio < a.HoraFin &&
                agenda.HoraFin > a.HoraInicio);

            if (solapa)
                return BadRequest("El horario se solapa con otra regla del mismo día.");

            _context.DoctorAgenda.Add(agenda);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                agenda.Id,
                agenda.DoctorID,
                agenda.DiaSemana,
                agenda.HoraInicio,
                agenda.HoraFin,
                agenda.DuracionMinutos
            });
        }

      
        // DELETE
    
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var item = await _context.DoctorAgenda.FindAsync(id);
            if (item == null) return NotFound();

            // Si es Doctor, solo puede borrar sus reglas
            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                if (item.DoctorID != me.Value) return Forbid();
            }

            _context.DoctorAgenda.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
