using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class VerTurnosPacientePage : ContentPage
{
    private readonly ApiService _api;

    public VerTurnosPacientePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarTurnos();
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomePaciente");
    }
    private async Task CargarTurnos()
    {
        try
        {
           
            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrWhiteSpace(token))
                token = await SecureStorage.GetAsync("jwt");

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert("Sesión", "No hay token guardado (token/jwt). Volvé a loguearte.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var endpoint = "api/turnos/mis-turnos";

           
            var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.ParseAdd("application/json");

            var res = await _api.Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("HTTP ERROR",
                    $"Status: {(int)res.StatusCode} {res.ReasonPhrase}\n\n{body}",
                    "OK");

                TurnosList.ItemsSource = new List<TurnoPacienteDto>();
                return;
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var turnos = JsonSerializer.Deserialize<List<TurnoPacienteDto>>(body, opts)
                        ?? new List<TurnoPacienteDto>();

            TurnosList.ItemsSource = turnos;

         
        }
        catch (Exception ex)
        {
            await DisplayAlert("EXCEPCIÓN", ex.ToString(), "OK");
            TurnosList.ItemsSource = new List<TurnoPacienteDto>();
        }
    }
}
