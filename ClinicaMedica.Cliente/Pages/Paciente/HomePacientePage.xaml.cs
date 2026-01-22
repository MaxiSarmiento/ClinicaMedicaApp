using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class HomePacientePage : ContentPage
{
    private readonly ApiService _api;
    private string _nroSocioReal = "";

    public HomePacientePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosPaciente();
    }

    private async Task CargarDatosPaciente()
    {
        try
        {
            var token = await SecureStorage.GetAsync("token");
            var idStr = await SecureStorage.GetAsync("userId");

            if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idStr, out var id))
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var datos = await _api.GetJsonAsync<PerfilPacienteDto>($"api/usuarios/paciente/{id}");
            if (datos == null)
            {
                await DisplayAlert("Error", "No se pudo cargar el perfil.", "OK");
                return;
            }

            var iniN = !string.IsNullOrWhiteSpace(datos.Nombre) ? datos.Nombre[0].ToString() : "P";
            var iniA = !string.IsNullOrWhiteSpace(datos.Apellido) ? datos.Apellido[0].ToString() : "X";
            LblIniciales.Text = (iniN + iniA).ToUpper();

            LblNombre.Text = $"{datos.Nombre} {datos.Apellido}".Trim();

            //  EDAD
            if (!string.IsNullOrWhiteSpace(datos.FechaNacimiento) &&
      DateTime.TryParse(datos.FechaNacimiento, out var fn))
            {
                LblEdad.Text = $"{CalcularEdad(fn)} años";
            }
            else
            {
                LblEdad.Text = "Edad: —";
            }

            LblObraSocial.Text = datos.ObraSocial ?? "Sin obra social";

            _nroSocioReal = datos.NroSocio ?? "";
            LblNroSocio.Text = OcultarNro(_nroSocioReal);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private static int CalcularEdad(DateTime fechaNacimiento)
    {
        var hoy = DateTime.Today;
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
        return Math.Max(0, edad);
    }

    private string OcultarNro(string nro)
    {
        if (string.IsNullOrWhiteSpace(nro)) return "";
        if (nro.Length <= 3) return "***" + nro;
        return $"{new string('*', nro.Length - 3)}{nro[^3..]}";
    }

    private void OnToggleNroSocio(object sender, ToggledEventArgs e)
        => LblNroSocio.Text = e.Value ? _nroSocioReal : OcultarNro(_nroSocioReal);

    //Menú overlay 
    private void OnToggleMenu(object sender, EventArgs e)
        => MenuOverlay.IsVisible = !MenuOverlay.IsVisible;

    private void OnOverlayTapped(object sender, TappedEventArgs e)
        => MenuOverlay.IsVisible = false;

    private async void OnEditarPerfil(object sender, EventArgs e)
    {
        MenuOverlay.IsVisible = false;
        await Shell.Current.GoToAsync("EditarPerfilPaciente");
    }

    private void OnCambiarTema(object sender, EventArgs e)
    {
        MenuOverlay.IsVisible = false;
        if (Application.Current.UserAppTheme == AppTheme.Light)
        {
            Application.Current.UserAppTheme =
            AppTheme.Dark;
        }
        else
        {
            Application.Current.UserAppTheme =
            AppTheme.Light;
        }
    }

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

    // ===== Botones =====
    private async void OnBuscarProfesional(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("BuscarDoctorTurno"); 

    private async void OnReservarTurno(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("ReservarTurno"); 

    private async void OnMisTurnos(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("MisTurnosPaciente"); 

    private async void OnEstudios(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("VerEstudiosPaciente"); 
 
}
