using Microsoft.AspNetCore.Mvc;
using ClinicaMedica.Backend.Services;

namespace ClinicaMedica.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly GoogleDriveOAuthService _drive;

    public HealthController(GoogleDriveOAuthService drive)
    {
        _drive = drive;
    }

    [HttpGet("drive")]
    public async Task<IActionResult> Drive()
    {
        var ok = await _drive.PingAsync();

        if (!ok)
            return StatusCode(503, new { ok = false, mensaje = "Google Drive NO autorizado / token inválido" });

        return Ok(new { ok = true, mensaje = "Google Drive OK" });
    }
}
