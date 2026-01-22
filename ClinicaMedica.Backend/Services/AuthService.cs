using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicaMedica.Backend.Data;
using ClinicaMedica.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicaMedica.Backend.Services
{
    public interface IAuthService
    {
        Task<(bool ok, string? token, Usuario? user, string? error)> LoginAsync(string email, string password);
        Task<(bool ok, string? error)> RegisterAsync(Usuario usuario, string password);
        Task<(bool ok, string? error)> CambiarPasswordAsync(int userId, string passwordActual, string passwordNueva);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<(bool ok, string? token, Usuario? user, string? error)>
     LoginAsync(string email, string password)
        {
            Console.WriteLine("====== LOGIN INTENTO ======");
            Console.WriteLine($"Email recibido: {email}");

            try
            {
                var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    Console.WriteLine("❌ Usuario NO encontrado");
                    return (false, null, null, "Usuario no encontrado");
                }

                Console.WriteLine($"✔ Usuario encontrado: ID={user.Id}, Rol={user.Rol}");

                // 🔐 Verificar password
                bool passOk = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                Console.WriteLine($"Password OK?: {passOk}");

                if (!passOk)
                {
                    Console.WriteLine("❌ Password incorrecta");
                    return (false, null, null, "Password incorrecta");
                }

                // 🔎 Rol
                if (string.IsNullOrWhiteSpace(user.Rol))
                {
                    Console.WriteLine("❌ Rol NULL o vacío");
                    return (false, null, null, "Rol inválido");
                }

                Console.WriteLine($"✔ Rol válido: '{user.Rol}'");

                // 🔑 Generar token
                var token = GenerateJwtToken(user);

                Console.WriteLine("✔ Token generado correctamente");
                Console.WriteLine("====== LOGIN OK ======\n");

                return (true, token, user, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔥 EXCEPCIÓN EN LOGIN");
                Console.WriteLine(ex.ToString());
                return (false, null, null, "Error interno en login");
            }
        }


        public async Task<(bool ok, string? error)> RegisterAsync(Usuario usuario, string password)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email))
                return (false, "Email ya registrado");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ok, string? error)> CambiarPasswordAsync(int userId, string passwordActual, string passwordNueva)
        {
            var usuario = await _context.Usuarios.FindAsync(userId);
            if (usuario == null)
                return (false, "Usuario no encontrado");

            if (!BCrypt.Net.BCrypt.Verify(passwordActual, usuario.PasswordHash))
                return (false, "La contraseña actual es incorrecta");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordNueva);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        private string GenerateJwtToken(Usuario usuario)
        {
            Console.WriteLine("→ Generando JWT...");
            Console.WriteLine($"UsuarioId: {usuario.Id}");
            Console.WriteLine($"Rol: {usuario.Rol}");

            var key = _config["Jwt:Key"];
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];

            Console.WriteLine($"Issuer: {issuer}");
            Console.WriteLine($"Audience: {audience}");

            if (string.IsNullOrWhiteSpace(key))
                throw new Exception("Jwt:Key no configurado");

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256
            );

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new Claim(ClaimTypes.Email, usuario.Email),
        new Claim(ClaimTypes.Role, usuario.Rol)
    };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            Console.WriteLine("← JWT generado OK");

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
