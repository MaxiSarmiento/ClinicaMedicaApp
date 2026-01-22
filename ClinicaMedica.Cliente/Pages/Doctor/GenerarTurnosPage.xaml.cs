using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class GenerarTurnosPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    private List<DoctorAgendaDtos> _agendas = new();

    public GenerarTurnosPage(ApiService api, AuthService auth)
    {
        InitializeComponent();
        _api = api;
        _auth = auth;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAgendas();
    }

    private async Task CargarAgendas()
    {
        var idDoctor = await SecureStorage.GetAsync("id_usuario");
        var token = await SecureStorage.GetAsync("jwt");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        _agendas = await _api.GetJsonAsync<List<DoctorAgendaDtos>>(
            $"api/doctoragendas/{idDoctor}");

        AgendaPicker.ItemsSource = _agendas;
        AgendaPicker.ItemDisplayBinding = new Binding("Descripcion");
    }

    private async void OnGenerarTurnos(object sender, EventArgs e)
    {
        if (AgendaPicker.SelectedItem is not DoctorAgendaDtos ag)
        {
            await DisplayAlert("Error", "Debe seleccionar una agenda.", "OK");
            return;
        }

        var fecha = FechaPicker.Date;

        var dto = new
        {
            AgendaID = ag.ID,
            Fecha = fecha
        };

        var token = await SecureStorage.GetAsync("jwt");
        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var resp = await _api.PostAsync("api/doctoragendas/generar/singular", dto);

        if (resp.IsSuccessStatusCode)
        {
            await DisplayAlert("OK", "Turnos generados correctamente.", "Cerrar");
        }
        else
        {
            await DisplayAlert("Error", "No se pudieron generar los turnos", "Cerrar");
        }
    }
}
