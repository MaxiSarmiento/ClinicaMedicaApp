using System.Net;
using System.Net.Http.Json;
using ClinicaMedica.Cliente.Models;

namespace ClinicaMedica.Cliente.Services
{
    public class UsuariosService
    {
        private readonly HttpClient _http;

        public UsuariosService(HttpClient http)
        {
            _http = http;
        }

        // GET api/usuarios/pacientes
        public async Task<List<Usuario>> GetPacientes()
        {
            var resp = await _http.GetAsync("api/usuarios/pacientes");

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para ver pacientes.");

            resp.EnsureSuccessStatusCode();

            var pacientes = await resp.Content.ReadFromJsonAsync<List<Usuario>>();
            return pacientes ?? new List<Usuario>();
        }

        // GET api/usuarios
        public async Task<List<UsuarioAdminDto>> GetUsuarios()
        {
            var resp = await _http.GetAsync("api/usuarios");

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para ver usuarios.");

            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<List<UsuarioAdminDto>>()) ?? new();
        }

        // PUT api/usuarios/{id}/rol
        public async Task CambiarRol(int id, string rol)
        {
            var dto = new { Rol = rol };
            var resp = await _http.PutAsJsonAsync($"api/usuarios/{id}/rol", dto);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para cambiar roles.");

            resp.EnsureSuccessStatusCode();
        }

        // GET api/usuarios/mi-perfil
        public async Task<PacientePerfilDto> GetMiPerfilPaciente()
        {
            var resp = await _http.GetAsync("api/usuarios/mi-perfil");

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            resp.EnsureSuccessStatusCode();

            var dto = await resp.Content.ReadFromJsonAsync<PacientePerfilDto>();
            return dto!;
        }

        // ✅ PUT api/usuarios/{id}/estado  { bloqueado: true/false }
        public async Task SetBloqueo(int id, bool bloquear)
        {
            var dto = new { Bloqueado = bloquear };

            var resp = await _http.PutAsJsonAsync($"api/usuarios/{id}/estado", dto);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para bloquear/desbloquear usuarios.");

            resp.EnsureSuccessStatusCode();
        }

        // ✅ DELETE api/usuarios/{id}
        public async Task EliminarUsuario(int id)
        {
            var resp = await _http.DeleteAsync($"api/usuarios/{id}");

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para eliminar usuarios.");

            resp.EnsureSuccessStatusCode();
        }
    }
}
