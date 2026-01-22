using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class BuscarDoctorTurnoPage : ContentPage
{
    private readonly ApiService _api;

    public BuscarDoctorTurnoPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDoctores();
    }

    private async Task CargarDoctores()
    {
        var token = await SecureStorage.GetAsync("token");
        var idPaciente = await SecureStorage.GetAsync("userId");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(idPaciente))
        {
            await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//Login");
            return;
        }

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

       
        var docs = await _api.GetJsonAsync<List<DoctorDisponibleDto>>(
            $"api/doctores/disponibles?pacienteId={idPaciente}");

        DoctoresList.ItemsSource = docs ?? new List<DoctorDisponibleDto>();
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomePaciente");
    }
    private async void OnVerTurnos(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not int idDoctor)
            return;

        
        await Shell.Current.GoToAsync($"TurnosDisponibles?idDoctor={idDoctor}");
    }
}
