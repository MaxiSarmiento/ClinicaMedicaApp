using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class MiObraSocialPage : ContentPage
{
    private readonly ApiService _api;

    public MiObraSocialPage()
    {
        InitializeComponent();

        var services = App.Current.Handler.MauiContext.Services;
        _api = services.GetService<ApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatos();
    }

    private async Task CargarDatos()
    {
        var userId = await SecureStorage.GetAsync("userId");
        var token = await SecureStorage.GetAsync("jwt");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var resp = await _api.GetAsync($"api/Usuarios/{userId}");

        if (!resp.IsSuccessStatusCode)
        {
            await DisplayAlert("Error", "No se pudo cargar el perfil.", "OK");
            return;
        }

        var perfil = await resp.Content.ReadFromJsonAsync<UsuarioPacienteDto>();

        if (perfil.OSID == null)
            LblOS.Text = "Sin obra social";
        else
            LblOS.Text = perfil.ObraSocial;

        EntryNroSocio.Text = perfil.NroSocio;
    }

    private async void OnGuardar(object sender, EventArgs e)
    {
        var userId = await SecureStorage.GetAsync("userId");
        var token = await SecureStorage.GetAsync("jwt");

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var dto = new { NroSocio = EntryNroSocio.Text };

        var resp = await _api.PutAsync($"api/Usuarios/guardar-nrosocio/{userId}", dto);

        if (resp.IsSuccessStatusCode)
            await DisplayAlert("OK", "Datos guardados.", "Cerrar");
        else
            await DisplayAlert("Error", "No se pudo guardar.", "Cerrar");
    }

    private async void OnCambiar(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SeleccionarObraSocialPacientePage");

    }
}
