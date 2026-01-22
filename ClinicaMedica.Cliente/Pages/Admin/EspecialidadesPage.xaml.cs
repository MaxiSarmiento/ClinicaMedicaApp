using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Admin;

public partial class EspecialidadesPage : ContentPage
{
    private readonly ApiService _api;

    public EspecialidadesPage()
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
            await CargarEspecialidades();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task<string?> GetToken()
    {
        return await SecureStorage.GetAsync("token") ?? await SecureStorage.GetAsync("jwt");
    }

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

    private async Task CargarEspecialidades()
    {
        if (!await PrepararAuth()) return;

        
        var resp = await _api.Client.GetAsync("api/Especialidades");

        if (!resp.IsSuccessStatusCode)
        {
            var detalle = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error",
                $"No se pudieron obtener las especialidades. ({(int)resp.StatusCode})\n{detalle}",
                "OK");
            return;
        }

        var lista = await resp.Content.ReadFromJsonAsync<List<EspecialidadDto>>() ?? new();
        ListaEspecialidades.ItemsSource = lista;
    }

    private async void OnAgregar(object sender, EventArgs e)
    {
        string nombre = await DisplayPromptAsync("Nueva Especialidad", "Nombre:");
        if (string.IsNullOrWhiteSpace(nombre)) return;

        if (!await PrepararAuth()) return;

        var dto = new { Nombre = nombre.Trim() };

        var resp = await _api.Client.PostAsJsonAsync("api/Especialidades", dto);

        if (resp.IsSuccessStatusCode)
            await CargarEspecialidades();
        else
        {
            var detalle = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error",
                $"No se pudo crear. ({(int)resp.StatusCode})\n{detalle}",
                "OK");
        }
    }

    private async void OnEditar(object sender, EventArgs e)
    {
        if (sender is not SwipeItem item || item.CommandParameter is not EspecialidadDto esp)
            return;

        string nuevo = await DisplayPromptAsync(
            "Editar Especialidad",
            "Nuevo nombre:",
            initialValue: esp.Nombre);

        if (string.IsNullOrWhiteSpace(nuevo)) return;

        if (!await PrepararAuth()) return;

        var dto = new { Nombre = nuevo.Trim() };

        var resp = await _api.Client.PutAsJsonAsync($"api/Especialidades/{esp.EspID}", dto);

        if (resp.IsSuccessStatusCode)
            await CargarEspecialidades();
        else
        {
            var detalle = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Error",
                $"No se pudo editar. ({(int)resp.StatusCode})\n{detalle}",
                "OK");
        }
    }

    private async void OnEliminar(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not EspecialidadDto esp)
            return;

        bool confirm = await DisplayAlert(
            "Confirmar",
            $"¿Eliminar '{esp.Nombre}'?",
            "Sí", "No");

        if (!confirm) return;

        if (!await PrepararAuth()) return; 
        var resp = await _api.Client.DeleteAsync($"api/Especialidades/{esp.EspID}");

        if (resp.IsSuccessStatusCode)
            await CargarEspecialidades();
        else
            await DisplayAlert("Error", "No se pudo eliminar.", "OK");
    }


    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeAdmin");
    }
}
