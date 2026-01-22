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
    private readonly GoogleDriveOAuthService _drive;

    public EstudiosController(
        AppDbContext context,
        IConfiguration config,
        GoogleDriveOAuthService drive)
    {
        _context = context;
        _config = config;
        _drive = drive;
    }

   
    // SUBIR ESTUDIO (ADMIN)
 
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

            Console.WriteLine(">> Antes de subir a Drive");

            await using var stream = dto.Archivo.OpenReadStream();

            var (fileId, webViewLink, downloadLink) = await _drive.UploadAsync(
                stream,
                nombreDrive,
                dto.Archivo.ContentType ?? "application/octet-stream",
                folderId
            );
            Console.WriteLine(">> SUBIR: empezó");
            Console.WriteLine($">> Archivo: {dto.Archivo.FileName} - {dto.Archivo.Length} bytes");
            Console.WriteLine(">> Antes de SaveChanges");

          

            Console.WriteLine(">> Después de SaveChanges");
            Console.WriteLine(">> Antes de subir a Drive");

           

           

            estudio.DriveFileId = fileId;
            estudio.DriveWebViewLink = webViewLink;
            estudio.DriveDownloadLink = downloadLink;


            await _context.SaveChangesAsync();
            Console.WriteLine(">> Después de subir a Drive");

            Console.WriteLine(">> Después de subir a Drive");
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


    
    // LISTAR ESTUDIOS POR PACIENTE
 
    [HttpGet("paciente/{pacienteId}")]
    [Authorize(Roles = "Admin,Doctor,Paciente")]
    public async Task<IActionResult> GetPorPaciente(int pacienteId)
    {
        var authHeader = Request.Headers["Authorization"].ToString();

        var userId = int.Parse(User.FindFirstValue("Id") ?? "0");
        var rol = User.FindFirstValue("Rol") ?? "";
        if (string.IsNullOrWhiteSpace(authHeader))
            return Unauthorized("NO LLEGÓ AUTHORIZATION HEADER");

        if (rol == "Paciente" && userId != pacienteId)
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

   
    // DESCARGAR ESTUDIO

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
            var userId = int.Parse(User.FindFirstValue("Id") ?? "0");
            if (estudio.IdPaciente != userId)
                return Forbid();
        }

        var result = await _drive.DownloadAsync(estudio.DriveFileId);

        var bytes = result.bytes;
        var contentType = result.contentType;
        var fileName = result.fileName;

        return File(
            bytes,
            contentType,
            estudio.NombreArchivo ?? fileName
        );
    }

    
    // ELIMINAR ESTUDIO (ADMIN)
  
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var estudio = await _context.Estudios.FindAsync(id);
        if (estudio == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(estudio.DriveFileId))
            await _drive.DeleteAsync(estudio.DriveFileId);

        _context.Estudios.Remove(estudio);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Estudio eliminado" });
    }
}
