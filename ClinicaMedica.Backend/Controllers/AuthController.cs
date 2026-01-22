using ClinicaMedica.Backend.Models;
using ClinicaMedica.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using ClinicaMedica.Backend.Models.Dto;
using ClinicaMedica.Backend.Models.Dto.ClinicaMedica.Backend.Models.Dto;
namespace ClinicaMedica.Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    //  LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var (ok, token, user, error) = await _auth.LoginAsync(dto.Email, dto.Password);

        if (!ok || user == null || token == null)
            return Unauthorized(new { error });

        return Ok(new
        {
            token,
            user.Id,
            user.Nombre,
            user.Apellido,
            user.Email,
            user.DNI,
            user.Rol,
            user.FechaNacimiento
        });
    }
    [HttpPut("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
    {
        var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");

        var (ok, error) = await _auth.CambiarPasswordAsync(userId, dto.PasswordActual, dto.PasswordNueva);

        if (!ok)
            return BadRequest(new { error });

        return Ok(new { message = "Contraseña actualizada correctamente" });
    }

    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        
        string rolFinal = string.IsNullOrWhiteSpace(dto.Rol) ? "Paciente" : dto.Rol;

        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            DNI = dto.DNI,
            Email = dto.Email,
            Rol = rolFinal,
            FechaNacimiento = dto.FechaNacimiento
        };

        var (ok, error) = await _auth.RegisterAsync(usuario, dto.Password);

        if (!ok)
            return BadRequest(new { error });

        return Ok(new { message = "Usuario creado correctamente" });
    }

   
}

public record LoginDto(string Email, string Password);

public record RegisterDto(
    string Email,
    string Password,
    string Nombre,
    string Apellido,
    DateOnly FechaNacimiento,
    string DNI,
    string Rol = "Paciente"
    );
