using System.Net.Http.Json;
using ClinicaMedica.Cliente.Config;

namespace ClinicaMedica.Cliente.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public string? Token { get; private set; }
        public int UserId { get; private set; }
        public string Rol { get; private set; } = "";

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LoginResult?> LoginAsync(string email, string password)
        {
            var dto = new { Email = email, Password = password };

            var resp = await _httpClient.PostAsJsonAsync("api/usuarios/login", dto);
            if (!resp.IsSuccessStatusCode)
                return null;

            var result = await resp.Content.ReadFromJsonAsync<LoginResult>();
            if (result != null)
            {
                Token = result.token;
                UserId = result.id;
                Rol = result.rol;

                // ✅ Persistir para que lo use el Handler
                await SecureStorage.SetAsync("token", Token);
                await SecureStorage.SetAsync("userId", UserId.ToString());
                await SecureStorage.SetAsync("rol", Rol);
            }

            return result;
        }

        public async Task<bool> RegisterAsync(string email, string password, string nombre, string apellido, DateOnly FechadeNacimiento, string dni, string rol)
        {
            var dto = new
            {
                Nombre = nombre,
                Apellido = apellido,
                Email = email,
                Password = password,
                DNI = dni,
                Rol = rol,
                FechaNacimiento = FechadeNacimiento
            };

            var resp = await _httpClient.PostAsJsonAsync("api/usuarios/register", dto);
            return resp.IsSuccessStatusCode;
        }
    }

    public class LoginResult
    {
        public string token { get; set; }
        public int id { get; set; }
        public string rol { get; set; }
        public string nombre { get; set; }
        public string apellido { get; set; }
        public string email { get; set; }
    }
}
