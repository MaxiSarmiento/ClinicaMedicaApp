using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Services;

public class DoctorAgendaApiService
{
    private readonly ApiService _api;

    public DoctorAgendaApiService(ApiService api) => _api = api;

    private void SetAuth(string token)
        => _api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // REGLAS
    public async Task<List<DoctorAgendaReglaDto>> GetReglasAsync(int doctorId, string token)
    {
        SetAuth(token);
        return await _api.Client.GetFromJsonAsync<List<DoctorAgendaReglaDto>>($"api/DoctorAgendas/doctor/{doctorId}")
               ?? new();
    }

    public async Task CrearReglaAsync(DoctorAgendaCreateDto dto, string token)
    {
        SetAuth(token);
        var resp = await _api.Client.PostAsJsonAsync("api/DoctorAgendas", dto);
        resp.EnsureSuccessStatusCode();
    }

    public async Task EliminarReglaAsync(int idAgenda, string token)
    {
        SetAuth(token);
        var resp = await _api.Client.DeleteAsync($"api/DoctorAgendas/{idAgenda}");
        resp.EnsureSuccessStatusCode();
    }

    // TURNOS
    public async Task<List<TurnoAgendaDto>> GetTurnosAsync(string token, DateTime desde, DateTime hasta)
    {
        SetAuth(token);
        var url = $"api/DoctorAgendas/agenda?desde={Uri.EscapeDataString(desde.ToString("O"))}&hasta={Uri.EscapeDataString(hasta.ToString("O"))}";
        return await _api.Client.GetFromJsonAsync<List<TurnoAgendaDto>>(url) ?? new();
    }

    public async Task<int> GenerarTurnosAsync(GenerarTurnosRequest dto, string token)
    {
        SetAuth(token);
        var resp = await _api.Client.PostAsJsonAsync("api/DoctorAgendas/generar", dto);
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<GenerarTurnosResponse>();
        return result?.creados ?? 0;
    }
}

// ===== DTOs (ponelos acá para NO duplicarlos en Models) =====
public class DoctorAgendaCreateDto
{
    public int DoctorID { get; set; }
    public int DiaSemana { get; set; } // 1..7
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int DuracionMinutos { get; set; }
}

public class DoctorAgendaReglaDto
{
    public int Id { get; set; }
    public int DoctorID { get; set; }
    public int DiaSemana { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int DuracionMinutos { get; set; }
}

public class GenerarTurnosRequest
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
}

public class GenerarTurnosResponse
{
    public string? mensaje { get; set; }
    public int creados { get; set; }
}

public class TurnoAgendaDto
{
    public int Id { get; set; }
    public DateTime FechaHora { get; set; }
    public int DuracionMinutos { get; set; }
    public string? Estado { get; set; }
    public string? PacienteNombre { get; set; } // lo llenamos “plano” desde el backend (recomendado)
}
