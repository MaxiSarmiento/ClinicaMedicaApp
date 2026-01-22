using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using ClinicaMedica.Backend.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TurnosController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TurnosController(AppDbContext context) => _context = context;

     
        // Helpers JWT

        private int? GetUserId()
        {
            var idStr =
                User.FindFirst("Id")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            return int.TryParse(idStr, out var id) ? id : null;
        }

        
        // DTOs (no postear entidades EF)
     
        public class CrearTurnoDto
        {
            public DateTime FechaHora { get; set; }
            public int DuracionMinutos { get; set; }

     
            public int? IdDoctor { get; set; }
        }

        public class ReservarTurnoDto
        {
            public int IdTurno { get; set; }

          
            public int? IdPaciente { get; set; }
        }

        public class TurnoDoctorItemDto
        {
            public int Id { get; set; }
            public DateTime FechaHora { get; set; }
            public int DuracionMinutos { get; set; }
            public string Estado { get; set; } = "";
            public bool Reservado { get; set; }
            public string? PacienteNombre { get; set; }
            public string? PacienteApellido { get; set; }
        }

       
        public class GenerarMasivoDto
        {
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }

            public TimeSpan HoraInicio { get; set; }
            public TimeSpan HoraFin { get; set; }

            public int DuracionMinutos { get; set; }

         
            public int? MaxTurnosPorDia { get; set; }

           
            public List<int>? ExcluirDiasSemana { get; set; }
        }

       
        // POST api/turnos/crear
        // Doctor crea disponibilidad (turno Libre)
        // Admin también puede
     
        [HttpPost("crear")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Crear([FromBody] CrearTurnoDto dto)
        {
            if (dto.FechaHora == default) return BadRequest("FechaHora inválida.");
            if (dto.DuracionMinutos <= 0) return BadRequest("DuracionMinutos inválida.");

            int doctorId;

            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                doctorId = me.Value;
            }
            else
            {
                if (!dto.IdDoctor.HasValue || dto.IdDoctor.Value <= 0)
                    return BadRequest("IdDoctor requerido para Admin.");

                doctorId = dto.IdDoctor.Value;
            }

       
            var fechaHora = new DateTime(dto.FechaHora.Year, dto.FechaHora.Month, dto.FechaHora.Day,
                                         dto.FechaHora.Hour, dto.FechaHora.Minute, 0);

         
            var existe = await _context.Turnos.AnyAsync(t =>
                t.IdDoctor == doctorId && t.FechaHora == fechaHora);

            if (existe)
                return BadRequest("Ya existe un turno para ese doctor en esa fecha/hora.");

            var turno = new Turno
            {
                IdDoctor = doctorId,
                IdPaciente = null,
                FechaHora = fechaHora,
                DuracionMinutos = dto.DuracionMinutos,
                Estado = "Libre"
            };

            _context.Turnos.Add(turno);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest("No se pudo crear (posible duplicado).");
            }

            return Ok(new
            {
                turno.Id,
                turno.IdDoctor,
                turno.FechaHora,
                turno.DuracionMinutos,
                turno.Estado
            });
        }

      
        // GET api/turnos/disponibles?fecha=...&idDoctor=...
        
        [HttpGet("disponibles")]
        public async Task<IActionResult> Disponibles([FromQuery] DateTime? fecha = null, [FromQuery] int? idDoctor = null)
        {
            var q = _context.Turnos
                .AsNoTracking()
                .Include(t => t.Doctor)
                    .ThenInclude(d => d.Especialidades)
                        .ThenInclude(de => de.Especialidad)
                .AsQueryable();

            if (fecha.HasValue)
            {
                var day = fecha.Value.Date;
                var nextDay = day.AddDays(1);
                q = q.Where(t => t.FechaHora >= day && t.FechaHora < nextDay);
            }

            if (idDoctor.HasValue && idDoctor.Value > 0)
                q = q.Where(t => t.IdDoctor == idDoctor.Value);

            var list = await q
                .Where(t => t.IdPaciente == null && t.Estado == "Libre" && t.FechaHora > DateTime.Now)
                .OrderBy(t => t.FechaHora)
                .Select(t => new
                {
                    t.Id,
                    t.IdDoctor,
                    t.FechaHora,
                    t.DuracionMinutos,
                    t.Estado,

                    DoctorNombre = t.Doctor != null ? t.Doctor.Nombre : "",
                    DoctorApellido = t.Doctor != null ? t.Doctor.Apellido : "",

                  
                    Especialidades = t.Doctor != null
                        ? t.Doctor.Especialidades.Select(e => e.Especialidad.Nombre).ToList()
                        : new List<string>()
                })
                .ToListAsync();

            return Ok(list);
        }


      
        // POST api/turnos/reservar
        // Paciente: IdPaciente desde JWT
        // Admin: puede pasar IdPaciente
  
        [HttpPost("reservar")]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> Reservar([FromBody] ReservarTurnoDto dto)
        {
            var turno = await _context.Turnos.FindAsync(dto.IdTurno);

            if (turno == null)
                return NotFound("Turno no encontrado.");

            if (turno.IdPaciente != null || turno.Estado != "Libre")
                return BadRequest("Turno inválido o no disponible.");

            if (turno.FechaHora <= DateTime.Now)
                return BadRequest("No se puede reservar un turno pasado.");

            int pacienteId;

            if (User.IsInRole("Paciente"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                pacienteId = me.Value;
            }
            else
            {
                if (!dto.IdPaciente.HasValue || dto.IdPaciente.Value <= 0)
                    return BadRequest("IdPaciente requerido para Admin.");

                pacienteId = dto.IdPaciente.Value;
            }

            turno.IdPaciente = pacienteId;
            turno.Estado = "Reservado";

            await _context.SaveChangesAsync();

            return Ok(new { turno.Id, turno.Estado });
        }

        // GET api/turnos/mis-turnos
        // Paciente logueado
       
        [HttpGet("mis-turnos")]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> MisTurnos()
        {
            var me = GetUserId();
            if (me == null) return Forbid();

            var idPaciente = me.Value;

            var turnos = await _context.Turnos
                .AsNoTracking()
                .Include(t => t.Doctor)
                .Where(t => t.IdPaciente == idPaciente)
                .OrderBy(t => t.FechaHora)
                .Select(t => new
                {
                    t.Id,
                    t.FechaHora,
                    t.DuracionMinutos,
                    t.Estado,
                    Doctor = t.Doctor != null ? (t.Doctor.Nombre + " " + t.Doctor.Apellido) : ""
                })
                .ToListAsync();

            return Ok(turnos);
        }


     
        // GET api/turnos/agenda?desde=...&hasta=...
        // Agenda del doctor logueado
       
        [HttpGet("agenda")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> AgendaDoctorLogueado([FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var me = GetUserId();
            if (me == null) return Forbid();
            var idDoctor = me.Value;

            var d = (desde ?? DateTime.Today).Date;
            var hExclusive = (hasta ?? DateTime.Today.AddDays(30)).Date.AddDays(1);

            var turnos = await _context.Turnos
                .AsNoTracking()
                .Include(t => t.Paciente)
                .Where(t => t.IdDoctor == idDoctor && t.FechaHora >= d && t.FechaHora < hExclusive)
                .OrderBy(t => t.FechaHora)
                .Select(t => new TurnoDoctorItemDto
                {
                    Id = t.Id,
                    FechaHora = t.FechaHora,
                    DuracionMinutos = t.DuracionMinutos,
                    Estado = t.Estado,
                    Reservado = t.IdPaciente != null,
                    PacienteNombre = t.IdPaciente != null ? t.Paciente!.Nombre : null,
                    PacienteApellido = t.IdPaciente != null ? t.Paciente!.Apellido : null
                })
                .ToListAsync();

            return Ok(turnos);
        }

       
        // GET api/turnos/doctor/{idDoctor}/agenda?desde=...&hasta=...
        // Admin o Doctor (solo si es él mismo)

        [HttpGet("doctor/{idDoctor:int}/agenda")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> AgendaDoctor(int idDoctor, [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                if (me.Value != idDoctor) return Forbid();
            }

            var desdeDate = desde.Date;
            var hastaExclusive = hasta.Date.AddDays(1);

            var turnos = await _context.Turnos
                .AsNoTracking()
                .Include(t => t.Paciente)
                .Where(t => t.IdDoctor == idDoctor && t.FechaHora >= desdeDate && t.FechaHora < hastaExclusive)
                .OrderBy(t => t.FechaHora)
                .Select(t => new TurnoDoctorItemDto
                {
                    Id = t.Id,
                    FechaHora = t.FechaHora,
                    DuracionMinutos = t.DuracionMinutos,
                    Estado = t.Estado,
                    Reservado = t.IdPaciente != null,
                    PacienteNombre = t.IdPaciente != null ? t.Paciente!.Nombre : null,
                    PacienteApellido = t.IdPaciente != null ? t.Paciente!.Apellido : null
                })
                .ToListAsync();

            return Ok(turnos);
        }

       
        // POST api/turnos/generar
        // Genera turnos desde DoctorAgenda
        // Doctor: fuerza DoctorID desde JWT
        // Admin: puede generar para cualquier doctor
      
        [HttpPost("generar")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> Generar([FromBody] GenerarTurnosDto dto)
        {
            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                dto.DoctorID = me.Value;
            }

            var desde = dto.FechaInicio.Date;
            var hasta = dto.FechaFin.Date;

            if (dto.DoctorID <= 0) return BadRequest("DoctorID inválido.");
            if (hasta < desde) return BadRequest("FechaFin no puede ser menor que FechaInicio.");

            var reglas = await _context.DoctorAgenda
                .AsNoTracking()
                .Where(a => a.DoctorID == dto.DoctorID)
                .ToListAsync();

            if (reglas.Count == 0)
                return BadRequest("El doctor no tiene agenda cargada.");

            var bloqueados = await _context.DoctorFechasBloqueadas
                .AsNoTracking()
                .Where(b => b.DoctorID == dto.DoctorID && b.Fecha >= desde && b.Fecha <= hasta)
                .Select(b => b.Fecha.Date)
                .ToListAsync();

            var bloqueadosSet = bloqueados.ToHashSet();

            var existentes = await _context.Turnos
                .AsNoTracking()
                .Where(t => t.IdDoctor == dto.DoctorID && t.FechaHora >= desde && t.FechaHora < hasta.AddDays(1))
                .Select(t => t.FechaHora)
                .ToListAsync();

            var existentesSet = existentes.ToHashSet();
            int creados = 0;

            for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
            {
                if (bloqueadosSet.Contains(dia))
                    continue;

                int diaSemana = ((int)dia.DayOfWeek + 6) % 7 + 1; // Lun=1..Dom=7

                foreach (var r in reglas.Where(r => r.DiaSemana == diaSemana))
                {
                    if (r.HoraFin <= r.HoraInicio) continue;
                    if (r.DuracionMinutos <= 0) continue;

                    var start = dia + r.HoraInicio;
                    var end = dia + r.HoraFin;
                    var dur = TimeSpan.FromMinutes(r.DuracionMinutos);

                    while (start + dur <= end)
                    {
                        if (!existentesSet.Contains(start))
                        {
                            _context.Turnos.Add(new Turno
                            {
                                IdDoctor = dto.DoctorID,
                                IdPaciente = null,
                                FechaHora = start,
                                DuracionMinutos = r.DuracionMinutos,
                                Estado = "Libre"
                            });

                            existentesSet.Add(start);
                            creados++;
                        }
                        start = start.Add(dur);
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Ok(new { creados, nota = "Algunos turnos pudieron omitirse por duplicados." });
            }

            return Ok(new { creados });
        }

       
        // POST api/turnos/generar-masivo
        // Doctor/Admin genera turnos por rango horario y exclusiones

        [HttpPost("generar-masivo")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GenerarMasivo([FromBody] GenerarMasivoDto dto)
        {
            if (dto.FechaFin.Date < dto.FechaInicio.Date)
                return BadRequest("FechaFin no puede ser menor que FechaInicio.");

            if (dto.DuracionMinutos <= 0)
                return BadRequest("Duración inválida.");

            if (dto.HoraFin <= dto.HoraInicio)
                return BadRequest("HoraFin debe ser mayor que HoraInicio.");

            if (dto.HoraInicio.Add(TimeSpan.FromMinutes(dto.DuracionMinutos)) > dto.HoraFin)
                return BadRequest("Con esa duración no entra ningún turno dentro del horario.");

            int doctorId;

            if (User.IsInRole("Doctor"))
            {
                var me = GetUserId();
                if (me == null) return Forbid();
                doctorId = me.Value;
            }
            else
            {
                // Admin: requiere doctorId por query
                if (!Request.Query.TryGetValue("doctorId", out var v) || !int.TryParse(v, out doctorId) || doctorId <= 0)
                    return BadRequest("Admin debe enviar doctorId por query: ?doctorId=1009");
            }

            var desde = dto.FechaInicio.Date;
            var hasta = dto.FechaFin.Date;

            var bloqueados = await _context.DoctorFechasBloqueadas
                .AsNoTracking()
                .Where(b => b.DoctorID == doctorId && b.Fecha >= desde && b.Fecha <= hasta)
                .Select(b => b.Fecha.Date)
                .ToListAsync();
            var bloqueadosSet = bloqueados.ToHashSet();

            var hastaExclusive = hasta.AddDays(1);
            var existentes = await _context.Turnos
                .AsNoTracking()
                .Where(t => t.IdDoctor == doctorId && t.FechaHora >= desde && t.FechaHora < hastaExclusive)
                .Select(t => t.FechaHora)
                .ToListAsync();
            var existentesSet = existentes.ToHashSet();

            var excluirSet = (dto.ExcluirDiasSemana ?? new()).ToHashSet();
            var dur = TimeSpan.FromMinutes(dto.DuracionMinutos);

            var nuevos = new List<Turno>();
            int creados = 0;

            for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
            {
                if (bloqueadosSet.Contains(dia))
                    continue;

                int diaSemana = ((int)dia.DayOfWeek + 6) % 7 + 1;
                if (excluirSet.Contains(diaSemana))
                    continue;

                var inicio = dia.Add(dto.HoraInicio);
                var fin = dia.Add(dto.HoraFin);

                int creadosHoy = 0;
                var slot = inicio;

                while (slot.Add(dur) <= fin)
                {
                    if (!existentesSet.Contains(slot))
                    {
                        nuevos.Add(new Turno
                        {
                            IdDoctor = doctorId,
                            IdPaciente = null,
                            FechaHora = slot,
                            DuracionMinutos = dto.DuracionMinutos,
                            Estado = "Libre"
                        });

                        existentesSet.Add(slot);
                        creados++;
                        creadosHoy++;

                        if (dto.MaxTurnosPorDia.HasValue && creadosHoy >= dto.MaxTurnosPorDia.Value)
                            break;
                    }

                    slot = slot.Add(dur);
                }
            }

            if (nuevos.Count == 0)
                return Ok(new { creados = 0, nota = "No había turnos para crear (ya existían o fueron excluidos/bloqueados)." });

            _context.Turnos.AddRange(nuevos);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Ok(new { creados, nota = "Algunos turnos pudieron omitirse por duplicados." });
            }

            return Ok(new { creados });
        }
    }
}
