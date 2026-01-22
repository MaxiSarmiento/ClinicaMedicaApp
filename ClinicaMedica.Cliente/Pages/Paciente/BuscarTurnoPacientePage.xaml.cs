using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class BuscarTurnoPacientePage : ContentPage
{
    private readonly ApiService _api;

    public BuscarTurnoPacientePage(ApiService api)
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
        var token = await SecureStorage.GetAsync("jwt");
        var idPaciente = await SecureStorage.GetAsync("id_usuario");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var docs = await _api.GetJsonAsync<List<DoctorDisponibleDto>>(
            $"api/doctores/disponibles?pacienteId={idPaciente}");

      ListaDocs.ItemsSource = docs;
    }

    private async void OnVerTurnos(object sender, EventArgs e)
    {
        var idDoctor = (int)((Button)sender).CommandParameter;
        await Shell.Current.GoToAsync($"//TurnosDisponiblesPage?idDoctor={idDoctor}");
    }
}
