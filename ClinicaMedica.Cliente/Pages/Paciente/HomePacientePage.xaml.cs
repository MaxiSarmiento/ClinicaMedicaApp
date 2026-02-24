using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class HomePacientePage : ContentPage
{
    private readonly ApiService _api;
    private string _nroSocioReal = "";

    // Notificaciones
    private List<TurnoHoyDto> _turnosHoy = new();

    public HomePacientePage(ApiService api)
    {
        InitializeComponent();
        _api = api;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosPaciente();
        await CargarNotificacionesTurnosHoy(); 
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


    private async Task CargarNotificacionesTurnosHoy()
    {
        try
        {
            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                SetBadge(false);
                _turnosHoy.Clear();
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Trae turnos del paciente (mis-turnos)
            var turnos = await _api.GetJsonAsync<List<TurnoHoyDto>>("api/turnos/mis-turnos") ?? new();

            var ahora = DateTime.Now;
            var limite = ahora.AddHours(24);

            _turnosHoy = turnos
                .Where(t =>
                    t.FechaHora >= ahora &&
                    t.FechaHora <= limite &&
                    string.Equals(t.Estado, "Reservado", StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.FechaHora)
                .ToList();

            if (_turnosHoy.Count == 0)
            {
                SetBadge(false);
                return;
            }

            // Colorear badge según el turno más cercano
            var prox = _turnosHoy[0];
            var mins = (prox.FechaHora - ahora).TotalMinutes;

            // < 2h rojo, < 12h naranja, < 24h azul
            if (mins <= 120)
                SetBadge(true, Colors.Red);
            else if (mins <= 720)
                SetBadge(true, Colors.Orange);
            else
                SetBadge(true, Colors.DodgerBlue);
        }
        catch
        {
            SetBadge(false);
            _turnosHoy.Clear();
        }
    }

    private void SetBadge(bool visible, Color? color = null)
    {
        BadgeNoti.IsVisible = visible;

        if (color != null)
            BadgeNoti.BackgroundColor = color; 
    }


    private async void OnNotificacionesClicked(object sender, EventArgs e)
    {
        if (_turnosHoy.Count == 0)
        {
            await DisplayAlert("Notificaciones", "No tenés turnos en las próximas 24 horas.", "OK");
            SetBadge(false);
            return;
        }

        var ahora = DateTime.Now;

        var lineas = _turnosHoy.Select(t =>
        {
            var cuando =
                t.FechaHora.Date == DateTime.Today ? $"Hoy {t.FechaHora:HH:mm}" :
                t.FechaHora.Date == DateTime.Today.AddDays(1) ? $"Mañana {t.FechaHora:HH:mm}" :
                $"{t.FechaHora:dd/MM HH:mm}";

            var faltan = t.FechaHora - ahora;
            string en;

            if (faltan.TotalMinutes < 60)
                en = $"(en {Math.Max(0, (int)Math.Round(faltan.TotalMinutes))} min)";
            else
                en = $"(en {Math.Max(0, (int)Math.Round(faltan.TotalHours))} h)";

            return $"• {cuando} {en}\n  Dr/a: {t.Doctor}";
        });

        var mensaje = string.Join("\n\n", lineas);

        await DisplayAlert("Recordatorio de turnos", mensaje, "OK");

        // opcional: al abrir notificaciones, oculto el badge
        SetBadge(false);
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

    // Menú overlay
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
        Application.Current.UserAppTheme =
            Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
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

  
        private class TurnoHoyDto
        {
        public DateTime FechaHora { get; set; }
        public string Doctor { get; set; } = "";
        public string Estado { get; set; } = ""; // "Libre" / "Reservado"
    }
}
