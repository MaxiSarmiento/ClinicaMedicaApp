using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using ClinicaMedica.Backend.Models.Dto;
using ClinicaMedica.Backend.Models.Dto.ClinicaMedica.Backend.Models.Dto;
using ClinicaMedica.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicaMedica.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly AppDbContext _context;

        public UsuariosController(IAuthService auth, AppDbContext context)
        {
            _auth = auth;
            _context = context;
        }

    
        // PACIENTES
       
        [HttpGet("pacientes")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> GetPacientes()
        {
            var lista = await _context.Usuarios
                .Where(u => u.Rol == "Paciente")
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.Email,
                    u.DNI,
                    u.Rol
                })
                .ToListAsync();

            return Ok(lista);
        }

        // PERFIL PACIENTE (JWT)
      
        [HttpGet("mi-perfil")]
        [Authorize(Roles = "Paciente")]
        public async Task<IActionResult> MiPerfil()
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idStr, out var id))
                return Unauthorized("Token inválido: no contiene NameIdentifier.");

            var paciente = await _context.Usuarios
                .Where(u => u.Id == id && u.Rol == "Paciente")
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.DNI,
                    u.Email,

                    FechaNacimiento = u.FechaNacimiento,

                    ObraSocial = u.OSID.HasValue
                        ? _context.ObraSocial
                            .Where(o => o.OSID == u.OSID)
                            .Select(o => o.Nombre)
                            .FirstOrDefault()
                        : null,
                    u.OSID,
                    u.NroSocio
                })
                .FirstOrDefaultAsync();

            if (paciente == null)
                return NotFound("Paciente no encontrado.");

            return Ok(paciente);
        }

      
        // LOGIN
        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _auth.LoginAsync(dto.Email, dto.Password);
            if (!result.ok)
                return Unauthorized(result.error);

            var user = result.user!;

            //  Bloqueo
            if (user.Bloqueado)
                return StatusCode(403, "Usuario bloqueado. Contacte al administrador.");

            return Ok(new
            {
                token = result.token,
                id = user.Id,
                rol = user.Rol,
                nombre = user.Nombre,
                apellido = user.Apellido,
                email = user.Email
                
            });
        }

       
        // REGISTRO
       
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var usuario = new Usuario
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Email = dto.Email,
                Rol = dto.Rol,
                DNI = dto.DNI,
                FechaRegistro = DateTime.UtcNow,
                Bloqueado = false
            };

            var result = await _auth.RegisterAsync(usuario, dto.Password);
            if (!result.ok)
                return BadRequest(result.error);

            return Ok(new { message = "Usuario registrado" });
        }

        // CAMBIAR PASSWORD
      
        [HttpPut("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            // id desde el JWT
            var idStr =
                User.FindFirst("Id")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            if (!int.TryParse(idStr, out var id) || id <= 0)
                return Unauthorized("Token inválido.");

            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.PasswordActual) ||
                string.IsNullOrWhiteSpace(dto.PasswordNueva))
                return BadRequest("Completá la contraseña actual y la nueva.");

            if (dto.PasswordNueva.Trim().Length < 6)
                return BadRequest("La nueva contraseña debe tener al menos 6 caracteres.");

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound(new { error = "Usuario no encontrado" });

    
            if (usuario.Bloqueado)
                return StatusCode(403, "Usuario bloqueado. Contacte al administrador.");

          
            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return BadRequest(new { error = "La contraseña actual es incorrecta" });

           
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva.Trim());
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Contraseña cambiada correctamente" });
        }


        // ADMIN - LISTAR USUARIOS
       
        // GET api/usuarios
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _context.Usuarios
                .Select(u => new UsuarioAdminDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol,
                    Bloqueado = u.Bloqueado
                })
                .ToListAsync();

            return Ok(usuarios);
        }

       
        // ADMIN - CAMBIAR ROL
        
        [HttpPut("{id}/rol")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            usuario.Rol = dto.Rol;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Rol actualizado" });
        }

        // ADMIN - BLOQUEAR / DESBLOQUEAR
        
        public class CambiarEstadoUsuarioDto
        {
            public bool Bloqueado { get; set; }
        }

        [HttpPut("{id}/estado")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoUsuarioDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            // Evitar que admin se bloquee a sí mismo
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idStr, out var adminId) && adminId == id)
                return BadRequest("No podés bloquearte/desbloquearte a vos mismo.");

            usuario.Bloqueado = dto.Bloqueado;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = usuario.Bloqueado ? "Usuario bloqueado" : "Usuario desbloqueado"
            });
        }

      
        // ADMIN - ELIMINAR USUARIO (DELETE REAL)
   
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            // Evitar que admin se elimine a sí mismo
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idStr, out var adminId) && adminId == id)
                return BadRequest("No podés eliminarte a vos mismo.");

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario eliminado" });
        }

        
        // PERFIL DOCTOR
      
        [HttpGet("doctor/{id}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GetPerfilDoctor(int id)
        {
            var doctor = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == id && u.Rol == "Doctor")
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.DNI,
                    u.Email,

                    ObrasSociales = _context.DoctoresObrasSociales
                        .AsNoTracking()
                        .Where(x => x.DoctorID == id)
                        .Select(x => new
                        {
                            OSID = x.OSID,
                            Nombre = x.ObraSocial.Nombre
                        })
                        .ToList(),

                    Especialidades = _context.DoctoresEspecialidades
                        .AsNoTracking()
                        .Where(x => x.DoctorID == id)
                        .Select(x => new
                        {
                            EspID = x.EspID,
                            Nombre = x.Especialidad.Nombre
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (doctor == null) return NotFound();

            return Ok(doctor);
        }

       
        // PERFIL PACIENTE POR ID
    
        [HttpGet("paciente/{id}")]
        [Authorize]
        public async Task<IActionResult> GetPerfilPaciente(int id)
        {
            var u = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound($"No existe usuario con Id={id}");
            if (u.Rol != "Paciente") return NotFound($"El usuario existe (Id={id}) pero Rol='{u.Rol}'");

            var obra = u.OSID.HasValue
                ? await _context.ObraSocial
                    .Where(o => o.OSID == u.OSID)
                    .Select(o => o.Nombre)
                    .FirstOrDefaultAsync()
                : null;

            return Ok(new
            {
                u.Id,
                u.Nombre,
                u.Apellido,
                u.DNI,
                u.Email,

                FechaNacimiento = u.FechaNacimiento,

                ObraSocial = obra,
                u.OSID,
                u.NroSocio
            });
        }

        // BUSCAR PACIENTES
      
        [HttpGet("buscar")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> BuscarPacientes([FromQuery] string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return Ok(new List<Usuario>());

            texto = texto.Trim().ToLower();

            var pacientes = await _context.Usuarios
                .Where(u => u.Rol == "Paciente" &&
                    (
                        u.Nombre.ToLower().Contains(texto) ||
                        u.Apellido.ToLower().Contains(texto) ||
                        u.DNI.ToLower().Contains(texto)
                    ))
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.DNI,
                    ObraSocial = _context.ObraSocial
                        .Where(os => os.OSID == u.OSID)
                        .Select(os => os.Nombre)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(pacientes);
        }

       
        // EDITAR PERFIL (Doctor + Paciente)
     
        [Authorize]
        [HttpPut("actualizar-perfil")]
        public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarPerfilDto dto)
        {
            var idStr =
                User.FindFirst("Id")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            if (!int.TryParse(idStr, out var userId) || userId <= 0)
                return Forbid();

            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
                return BadRequest("Nombre y Apellido son obligatorios.");

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Apellido = dto.Apellido.Trim();

            usuario.FechaNacimiento = dto.FechaNacimiento.HasValue
                ? DateOnly.FromDateTime(dto.FechaNacimiento.Value)
                : null;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Perfil actualizado" });
        }

      
        // SET OBRA SOCIAL DEL PACIENTE
      
        [HttpPut("paciente/obra-social/{id}")]
        [Authorize(Roles = "Paciente,Admin")]
        public async Task<IActionResult> ActualizarObraSocial(int id, [FromBody] CambiarOSDto dto)
        {
            if (dto == null)
                return BadRequest("Body inválido.");

            var idStr =
                User.FindFirst("Id")?.Value ??
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value;

            if (!int.TryParse(idStr, out var jwtId) || jwtId <= 0)
                return Unauthorized("Token inválido.");

            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;

   
            if (string.Equals(rol, "Paciente", StringComparison.OrdinalIgnoreCase))
                id = jwtId;

           
            var existeOS = await _context.ObraSocial.AnyAsync(o => o.OSID == dto.OSID);
            if (!existeOS)
                return BadRequest("La obra social seleccionada no existe.");

            var paciente = await _context.Usuarios.FindAsync(id);
            if (paciente == null) return NotFound("Paciente no encontrado.");

           
            if (!string.Equals(paciente.Rol, "Paciente", StringComparison.OrdinalIgnoreCase))
                return BadRequest("El usuario no es Paciente.");

            paciente.OSID = dto.OSID;
            paciente.NroSocio = dto.NroSocio?.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Obra social actualizada" });
        }


        public class CambiarOSDto
        {
            public int OSID { get; set; }
            public string? NroSocio { get; set; }
        }
    }
}
