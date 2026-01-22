using ClinicaMedica.Cliente.Services;
using Microsoft.Maui.Controls;
using System.Net;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class CrearTurnoSingularPage : ContentPage
{
    private readonly ApiService _api;

    public CrearTurnoSingularPage(ApiService api)
    {
        InitializeComponent();
        _api = api;

        FechaPicker.Date = DateTime.Today;
        HoraPicker.Time = TimeSpan.FromHours(9);
    }

    private async Task<(string? token, int userId)> GetSesionAsync()
    {
        var token = await SecureStorage.GetAsync("token");
        var idStr = await SecureStorage.GetAsync("userId");

      
        if (string.IsNullOrWhiteSpace(token))
            token = await SecureStorage.GetAsync("jwt");

        if (string.IsNullOrWhiteSpace(idStr))
            idStr = await SecureStorage.GetAsync("id_usuario");

        if (!int.TryParse(idStr, out var id))
            id = 0;

        return (token, id);
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ConfigurarAgenda");
    }
    private async void OnCrearTurno(object sender, EventArgs e)
    {
        try
        {
            if (!int.TryParse(DuracionEntry.Text, out var dur) || dur <= 0)
            {
                await DisplayAlert("Error", "Duración inválida", "OK");
                return;
            }

            var (token, doctorId) = await GetSesionAsync();
            if (string.IsNullOrWhiteSpace(token) || doctorId <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var fechaHora = FechaPicker.Date.Date + HoraPicker.Time;

            if (fechaHora <= DateTime.Now)
            {
                await DisplayAlert("Error", "No podés crear un turno en el pasado.", "OK");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                IdDoctor = doctorId,
                IdPaciente = (int?)null,
                FechaHora = fechaHora,
                DuracionMinutos = dur
            };

            var resp = await _api.PostAsync("api/turnos/crear", payload);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await DisplayAlert("Sesión", "Sesión expirada o token inválido.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            if (resp.StatusCode == HttpStatusCode.Forbidden)
            {
                await DisplayAlert("Permisos", "No tenés permisos para crear turnos.", "OK");
                return;
            }

            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                await DisplayAlert("Error",
                    string.IsNullOrWhiteSpace(msg) ? "No se pudo crear el turno." : msg,
                    "Cerrar");
                return;
            }

            await DisplayAlert("OK", "Turno creado correctamente.", "Cerrar");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}
