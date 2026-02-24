using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using ClinicaMedica.Backend.Models.Dto;
using ClinicaMedica.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicaMedica.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IGoogleDriveService _driveSA;
    private readonly GoogleDriveOAuthService _driveOAuth;

    public EstudiosController(
        AppDbContext context,
        IConfiguration config,
        IGoogleDriveService driveSA,
        GoogleDriveOAuthService driveOAuth)
    {
        _context = context;
        _config = config;
        _driveSA = driveSA;
        _driveOAuth = driveOAuth;
    }

    [HttpPost("subir")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Subir([FromForm] UploadEstudioDto dto)
    {
        if (dto.Archivo == null || dto.Archivo.Length == 0)
            return BadRequest("Archivo inválido.");

        if (dto.Archivo.Length > 10 * 1024 * 1024)
            return BadRequest("Archivo demasiado grande (máx 10MB).");

        var paciente = await _context.Usuarios.FindAsync(dto.PacienteId);
        if (paciente == null || paciente.Rol != "Paciente")
            return BadRequest("Paciente inválido.");

        var ext = Path.GetExtension(dto.Archivo.FileName).ToLowerInvariant();
        var permitidos = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        if (!permitidos.Contains(ext))
            return BadRequest("Solo PDF o imágenes JPG/PNG.");

        var fecha = dto.Fecha ?? DateTime.UtcNow;

        var estudio = new Estudio
        {
            IdPaciente = paciente.Id,
            NombreArchivo = dto.Archivo.FileName,
            Fecha = fecha,
            Descripcion = dto.Descripcion ?? "",
            Dni = paciente.DNI,
            NombrePaciente = paciente.Nombre,
            ApellidoPaciente = paciente.Apellido
        };

        _context.Estudios.Add(estudio);
        await _context.SaveChangesAsync();

        try
        {
            var folderId = _config["GoogleDrive:FolderId"];
            if (string.IsNullOrWhiteSpace(folderId))
                throw new Exception("Falta GoogleDrive:FolderId");

            var nombreDrive = $"{paciente.Id}_{estudio.Id}_{fecha:yyyyMMdd}{ext}";

            await using var stream = dto.Archivo.OpenReadStream();

            var (fileId, webViewLink, downloadLink) = await _driveOAuth.UploadAsync(
                stream,
                nombreDrive,
                dto.Archivo.ContentType ?? "application/octet-stream",
                folderId
            );

            estudio.DriveFileId = fileId;
            estudio.DriveWebViewLink = webViewLink;
            estudio.DriveDownloadLink = downloadLink;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Estudio subido correctamente",
                estudio.Id,
                driveFileId = fileId,
                webViewLink,
                downloadLink
            });
        }
        catch (Exception ex)
        {
            _context.Estudios.Remove(estudio);
            await _context.SaveChangesAsync();

            return StatusCode(500, new
            {
                mensaje = "Falló la subida a Drive",
                error = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("paciente/{pacienteId}")]
    [Authorize(Roles = "Admin,Doctor,Paciente")]
    public async Task<IActionResult> GetPorPaciente(int pacienteId)
    {
        var userIdStr =
            User.FindFirstValue("Id") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            "0";

        int.TryParse(userIdStr, out var userId);

        var rol =
            User.FindFirstValue(ClaimTypes.Role) ??
            User.FindFirstValue("role") ??
            User.FindFirstValue("Rol") ??
            "";

        if (string.Equals(rol, "Paciente", StringComparison.OrdinalIgnoreCase) && userId != pacienteId)
            return Forbid();

        var estudios = await _context.Estudios
            .Where(e => e.IdPaciente == pacienteId)
            .OrderByDescending(e => e.Fecha)
            .Select(e => new EstudioListDto
            {
                Id = e.Id,
                NombreArchivo = e.NombreArchivo,
                Fecha = e.Fecha,
                Descripcion = e.Descripcion
            })
            .ToListAsync();

        return Ok(estudios);
    }

    [HttpGet("archivo/{id}")]
    [Authorize(Roles = "Paciente,Doctor")]
    public async Task<IActionResult> Descargar(int id)
    {
        var estudio = await _context.Estudios.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (estudio == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(estudio.DriveFileId))
            return NotFound("Archivo no disponible.");

        if (User.IsInRole("Paciente"))
        {
            var userIdStr =
                User.FindFirstValue("Id") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                "0";

            int.TryParse(userIdStr, out var userId);

            if (estudio.IdPaciente != userId)
                return Forbid();
        }

        var result = await _driveSA.DownloadAsync(estudio.DriveFileId);

        return File(
            result.bytes,
            result.contentType,
            estudio.NombreArchivo ?? result.fileName
        );
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var estudio = await _context.Estudios.FindAsync(id);
        if (estudio == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(estudio.DriveFileId))
            await _driveOAuth.DeleteAsync(estudio.DriveFileId);

        _context.Estudios.Remove(estudio);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Estudio eliminado" });
    }
}