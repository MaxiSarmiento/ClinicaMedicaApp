using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class MisObrasSocialesPage : ContentPage
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    public MisObrasSocialesPage()
    {
        InitializeComponent();

        var services = App.Current.Handler.MauiContext.Services;
        _api = services.GetService<ApiService>();
        _auth = services.GetService<AuthService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarObras();
    }

    private async Task CargarObras()
    {
        var token = await SecureStorage.GetAsync("jwt");
        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var doctorId = await SecureStorage.GetAsync("userId");

        var resp = await _api.GetAsync($"api/DoctoresObrasSociales/{doctorId}");

        if (!resp.IsSuccessStatusCode)
        {
            await DisplayAlert("Error", "No se pudieron cargar las obras.", "OK");
            return;
        }

        var obras = await resp.Content.ReadFromJsonAsync<List<ObraSocial>>();
        ListaDoctorObras.ItemsSource = obras;
    }

    private async void OnAgregar(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SeleccionarObraSocialPage());
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("PerfilDoctor");
    }
    private async void OnEliminar(object sender, EventArgs e)
    {
        var obra = (ObraSocial)((SwipeItem)sender).CommandParameter;

        var confirm = await DisplayAlert(
            "Confirmar",
            $"¿Eliminar '{obra.Nombre}'?",
            "Sí", "No");

        if (!confirm) return;

        var token = await SecureStorage.GetAsync("jwt");
        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var doctorId = await SecureStorage.GetAsync("userId");

        var resp = await _api.DeleteAsync($"api/DoctoresObrasSociales/{doctorId}/{obra.OSID}");

        if (resp.IsSuccessStatusCode)
            await CargarObras();
        else
            await DisplayAlert("Error", "No se pudo eliminar.", "OK");
    }
}
