using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class MisTurnosPage : ContentPage
{
    private readonly ApiService _api;

    public MisTurnosPage(ApiService api)
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
            {
                await DisplayAlert("Sesión", "No hay token. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

          
            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

         
            var turnos = await _api.Client.GetFromJsonAsync<List<TurnoPacienteDto>>("api/turnos/mis-turnos");

            TurnosList.ItemsSource = turnos ?? new();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar los turnos: {ex.Message}", "OK");
            TurnosList.ItemsSource = new List<TurnoPacienteDto>();
        }
    }
}
