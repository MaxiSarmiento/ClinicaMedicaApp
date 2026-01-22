using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Admin;

public partial class ObrasSocialesPage : ContentPage
{
    private readonly ApiService _api;

    public ObrasSocialesPage()
    {
        InitializeComponent();

        var services = App.Current!.Handler!.MauiContext!.Services;
        _api = services.GetService<ApiService>()!;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await CargarObras();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task<string?> GetToken()
        => await SecureStorage.GetAsync("token") ?? await SecureStorage.GetAsync("jwt");

    private async Task<bool> PrepararAuth()
    {
        var token = await GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            await DisplayAlert("Error", "No hay token. Volvé a iniciar sesión.", "OK");
            return false;
        }

        _api.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return true;
    }

    private async Task CargarObras()
    {
        if (!await PrepararAuth()) return;

  
        var resp = await _api.Client.GetAsync("api/ObrasSociales");

        if (!resp.IsSuccessStatusCode)
        {
            var detalle = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error",
                $"No se pudieron obtener las obras sociales. ({(int)resp.StatusCode})\n{detalle}",
                "OK");
            return;
        }

        var obras = await resp.Content.ReadFromJsonAsync<List<ObraSocial>>() ?? new();
        ListaObras.ItemsSource = obras;
    }

    private async void OnAgregar(object sender, EventArgs e)
    {
        string nombre = await DisplayPromptAsync("Nueva Obra Social", "Nombre:");
        if (string.IsNullOrWhiteSpace(nombre)) return;

        if (!await PrepararAuth()) return;

        var dto = new { Nombre = nombre.Trim() };

        var resp = await _api.Client.PostAsJsonAsync("api/ObrasSociales", dto);

        if (resp.IsSuccessStatusCode)
        {
            await CargarObras();
            return;
        }

        //  Si backend devuelve 409 por duplicado
        if ((int)resp.StatusCode == 409)
        {
            var msg = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Atención",
                string.IsNullOrWhiteSpace(msg) ? "Esa obra social ya existe." : msg,
                "OK");
            return;
        }

        var detalle = await resp.Content.ReadAsStringAsync();
        await DisplayAlert("Error", $"No se pudo crear. ({(int)resp.StatusCode})\n{detalle}", "OK");
    }

    private async void OnEliminar(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not ObraSocial obra)
            return;

        bool confirm = await DisplayAlert("Confirmar", $"¿Eliminar '{obra.Nombre}'?", "Sí", "No");
        if (!confirm) return;

        if (!await PrepararAuth()) return;

        var resp = await _api.Client.DeleteAsync($"api/ObrasSociales/{obra.OSID}");

        if (resp.IsSuccessStatusCode)
        {
            await CargarObras();
            return;
        }

        var detalle = await resp.Content.ReadAsStringAsync();
        await DisplayAlert("Error", $"No se pudo eliminar. ({(int)resp.StatusCode})\n{detalle}", "OK");
    }

    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeAdmin");
    }
}
