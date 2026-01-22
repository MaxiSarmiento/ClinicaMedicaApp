using System.Net.Http.Json;
using System.Net.Http.Headers;
using ClinicaMedica.Cliente.Models;
namespace ClinicaMedica.Cliente.Services
{
    public class EstudiosService
    {
        private readonly HttpClient _http;

        public EstudiosService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<EstudioListDto>> GetEstudiosPaciente(int pacienteId)
        {
            // Este endpoint debe existir en el backend: GET api/Estudios/paciente/{pacienteId}
            var resp = await _http.GetAsync($"api/Estudios/paciente/{pacienteId}");

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para ver los estudios.");

            resp.EnsureSuccessStatusCode();

            var lista = await resp.Content.ReadFromJsonAsync<List<EstudioListDto>>();
            return lista ?? new List<EstudioListDto>();
        }

        public async Task SubirEstudio(UploadEstudioDto dto)
        {
            if (dto.Archivo == null)
                throw new ArgumentException("Debes seleccionar un archivo.");

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(dto.PacienteId.ToString()), "PacienteId");
            content.Add(new StringContent(dto.Descripcion ?? ""), "Descripcion");

            var fechaStr = (dto.Fecha ?? DateTime.UtcNow).ToString("o"); // ✅ ISO 8601
            content.Add(new StringContent(fechaStr), "Fecha");

            await using var stream = await dto.Archivo.OpenReadAsync();
            var fileContent = new StreamContent(stream);

            var contentType = string.IsNullOrWhiteSpace(dto.Archivo.ContentType)
                ? "application/octet-stream"
                : dto.Archivo.ContentType;

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            content.Add(fileContent, "Archivo", dto.Archivo.FileName);

            var resp = await _http.PostAsync("api/Estudios/subir", content);

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para subir estudios (solo Admin).");

            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                throw new Exception(string.IsNullOrWhiteSpace(error) ? "Error al subir el estudio." : error);
            }
        }

        public async Task EliminarEstudio(int id)
        {
            var resp = await _http.DeleteAsync($"api/Estudios/{id}");

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para eliminar estudios (solo Admin).");

            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                throw new Exception(string.IsNullOrWhiteSpace(error) ? "Error al eliminar el estudio." : error);
            }
        }

        public async Task<byte[]> Descargar(int id)
        {
            var resp = await _http.GetAsync($"api/Estudios/archivo/{id}");

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Sesión expirada o token inválido.");

            if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("No tenés permisos para descargar este estudio.");

            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                throw new Exception(string.IsNullOrWhiteSpace(error) ? "Error al descargar el estudio." : error);
            }

            return await resp.Content.ReadAsByteArrayAsync();
        }
    }
}
