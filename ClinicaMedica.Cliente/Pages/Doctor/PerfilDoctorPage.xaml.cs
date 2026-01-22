using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using Microsoft.Maui.Controls;
using System.Net;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class PerfilDoctorPage : ContentPage
{
    private readonly ApiService _api;
    private DoctorPerfilDto? _perfil;

    public PerfilDoctorPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPerfil();
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

    private async Task CargarPerfil()
    {
        try
        {
            var (token, id) = await GetSesionAsync();

            if (string.IsNullOrWhiteSpace(token) || id <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            _perfil = await _api.GetJsonAsync<DoctorPerfilDto>($"api/Usuarios/doctor/{id}");

            if (_perfil == null)
            {
                await DisplayAlert("Error", "No se pudo cargar el perfil.", "OK");
                return;
            }

            EntryNombre.Text = _perfil.Nombre ?? "";
            EntryApellido.Text = _perfil.Apellido ?? "";
            EntryDNI.Text = _perfil.DNI ?? "";
            EntryEmail.Text = _perfil.Email ?? "";

            ListaOS.ItemsSource = _perfil.ObrasSociales ?? new();
            ListaEsp.ItemsSource = _perfil.Especialidades ?? new();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el perfil: {ex.Message}", "OK");
        }
    }

    private async void OnGuardar(object sender, EventArgs e)
    {
        try
        {
            var (token, id) = await GetSesionAsync();

            if (string.IsNullOrWhiteSpace(token) || id <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            var dto = new
            {
                Nombre = EntryNombre.Text?.Trim(),
                Apellido = EntryApellido.Text?.Trim(),
                DNI = EntryDNI.Text?.Trim(),
                Email = EntryEmail.Text?.Trim()
            };

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var resp = await _api.PutAsync($"api/Usuarios/actualizar-perfil/{id}", dto);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await DisplayAlert("Sesión", "Sesión expirada o token inválido.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            if (resp.StatusCode == HttpStatusCode.Forbidden)
            {
                await DisplayAlert("Permisos", "No tenés permisos para actualizar el perfil.", "OK");
                return;
            }

            resp.EnsureSuccessStatusCode();

            await DisplayAlert("OK", "Datos actualizados", "Cerrar");
            await CargarPerfil();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "Cerrar");
        }
    }

    private async void OnAgregarOS(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("SeleccionarObraSocial");

    private async void OnAgregarEsp(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("SeleccionarEspecialidad");

    private async void OnEliminarOS(object sender, EventArgs e)
    {
        try
        {
            if (((SwipeItem)sender).CommandParameter is not ObraSocialDto item)
                return;

            if (!await DisplayAlert("Confirmar", $"¿Eliminar {item.Nombre}?", "Sí", "No"))
                return;

            var (token, id) = await GetSesionAsync();
            if (string.IsNullOrWhiteSpace(token) || id <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var resp = await _api.DeleteAsync($"api/DoctoresObrasSociales/{id}/{item.OSID}");
            resp.EnsureSuccessStatusCode();

            await CargarPerfil();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomeDoctor");
    }
    private async void OnEliminarEsp(object sender, EventArgs e)
    {
        try
        {
            if (((SwipeItem)sender).CommandParameter is not EspecialidadDto item)
                return;

            if (!await DisplayAlert("Confirmar", $"¿Eliminar {item.Nombre}?", "Sí", "No"))
                return;

            var (token, id) = await GetSesionAsync();
            if (string.IsNullOrWhiteSpace(token) || id <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var resp = await _api.DeleteAsync($"api/DoctoresEspecialidades/{id}/{item.EspID}");
            resp.EnsureSuccessStatusCode();

            await CargarPerfil();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }

    }
}
