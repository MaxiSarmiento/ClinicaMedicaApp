using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Doctor;

public partial class HomeDoctorPage : ContentPage
{
    private readonly ApiService _api;

    public HomeDoctorPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarPerfilDoctorAsync();
    }

    private async Task CargarPerfilDoctorAsync()
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            var idStr = await SecureStorage.GetAsync("userId");

            if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idStr, out var id) || id <= 0)
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var perfil = await _api.GetJsonAsync<DoctorPerfilDto>($"api/Usuarios/doctor/{id}");

            if (perfil == null)
            {
                await DisplayAlert("Error", "No se pudo cargar el perfil del doctor.", "OK");
                return;
            }

            var iniN = !string.IsNullOrWhiteSpace(perfil.Nombre) ? perfil.Nombre[0].ToString() : "D";
            var iniA = !string.IsNullOrWhiteSpace(perfil.Apellido) ? perfil.Apellido[0].ToString() : "R";
            LblIniciales.Text = (iniN + iniA).ToUpper();

            LblNombre.Text = $"{perfil.Nombre} {perfil.Apellido}".Trim();

            var esp = (perfil.Especialidades ?? new())
                .Select(e => e.Nombre)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();

            LblEspecialidades.Text = esp.Count > 0 ? string.Join(" • ", esp) : "Sin especialidades";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            Loading.IsVisible = Loading.IsRunning = false;
        }
    }

    private async void OnDoctorAgenda(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("AgendaDoctor");

    private async void OnConfigurarAgenda(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ConfigurarAgenda");


    private async void OnBuscarPaciente(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("BuscarPaciente");

    private async void OnBuscarEstudios(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("VerEstudiosDoctor");

    private async void OnPerfilDoctor(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("PerfilDoctor");

    private async void OnLogout(object sender, EventArgs e)
    {
        SecureStorage.Remove("token");
        SecureStorage.Remove("userId");
        SecureStorage.Remove("rol");

        // opcional compat
        SecureStorage.Remove("jwt");
        SecureStorage.Remove("id_usuario");

        await Shell.Current.GoToAsync("Login");
    }
}
