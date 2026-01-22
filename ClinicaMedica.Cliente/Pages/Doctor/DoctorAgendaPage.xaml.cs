using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class DoctorAgendaPage : ContentPage
{
    private readonly ApiService _api;

    public DoctorAgendaPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAgendas();
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeDoctor");
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

    private async Task CargarAgendas()
    {
        try
        {
            var (token, idDoctor) = await GetSesionAsync();
            if (string.IsNullOrWhiteSpace(token) || idDoctor <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var agendas = await _api.GetJsonAsync<List<DoctorAgendaDtos>>($"api/doctoragendas/{idDoctor}");
            AgendaList.ItemsSource = agendas ?? new List<DoctorAgendaDtos>();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudieron cargar las agendas: {ex.Message}", "OK");
        }
    }

    
    private async void OnConfigurarSingular(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("CrearTurnoSingular");

    private async void OnGenerarMasivo(object sender, EventArgs e)
      => await Shell.Current.GoToAsync("GenerarTurnosMasivos");

    private async void OnEliminar(object sender, EventArgs e)
    {
        try
        {
            var item = (DoctorAgendaDtos)((Button)sender).CommandParameter;

            if (!await DisplayAlert("Eliminar", "¿Eliminar esta agenda?", "Sí", "No"))
                return;

            var (token, _) = await GetSesionAsync();
            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            await _api.DeleteAsync($"api/doctoragendas/eliminar/{item.ID}");

            await CargarAgendas();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void OnAgregarAgenda(object sender, EventArgs e)
    => await Shell.Current.GoToAsync("CrearAgenda");

    private async void OnEditar(object sender, EventArgs e)
    {
        var item = (DoctorAgendaDtos)((Button)sender).CommandParameter;
        await Shell.Current.GoToAsync($"CrearAgenda?idAgenda={item.ID}");
    }
}
