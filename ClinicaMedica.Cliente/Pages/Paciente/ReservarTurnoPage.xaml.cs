using ClinicaMedica.Cliente.Models;
using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClinicaMedica.Cliente.Pages.Paciente;

public partial class ReservarTurnoPage : ContentPage
{
    private readonly ApiService _api;

    private static class ApiPaths
    {
        public const string Especialidades = "api/Especialidades";
        public const string Doctores = "api/Doctores"; 
        public const string TurnosDisponibles = "api/Turnos/disponibles";
        public const string ReservarTurno = "api/Turnos/reservar";
    }

    private List<EspecialidadItemDto> _especialidades = new();
    private List<DoctorItemDto> _doctores = new();

    public ReservarTurnoPage(ApiService api)
    {
        InitializeComponent();
        _api = api;

        FechaFiltro.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarInicialAsync();
    }
    private async void OnVolver(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("HomePaciente");
    }
    private async Task<(string token, int userId)?> GetSesionPacienteAsync()
    {
        var token = await SecureStorage.GetAsync("token");
        var idStr = await SecureStorage.GetAsync("userId");

        if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idStr, out var id) || id <= 0)
        {
            await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//Login");
            return null;
        }
        return (token, id);
    }

    private void SetLoading(bool on)
    {
        Loading.IsVisible = Loading.IsRunning = on;
    }

    private void Info(string text)
    {
        LblInfo.Text = text;
        LblInfo.IsVisible = !string.IsNullOrWhiteSpace(text);
    }

    private async Task CargarInicialAsync()
    {
        var sesion = await GetSesionPacienteAsync();
        if (sesion == null) return;

        SetLoading(true);
        Info("");

        try
        {
          
            var req = new HttpRequestMessage(HttpMethod.Get, ApiPaths.Especialidades);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);

            var res = await _api.Client.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                Info($"No pude cargar especialidades. HTTP {(int)res.StatusCode}\n{body}");
                return;
            }

            _especialidades = (await res.Content.ReadFromJsonAsync<List<EspecialidadItemDto>>())
                              ?? new List<EspecialidadItemDto>();

           
            _especialidades.Insert(0, new EspecialidadItemDto { Id = 0, Nombre = "Todas" });

            EspecialidadPicker.ItemsSource = _especialidades;
            EspecialidadPicker.ItemDisplayBinding = new Binding(nameof(EspecialidadItemDto.Nombre));
            EspecialidadPicker.SelectedIndex = 0;

            await CargarDoctoresAsync(especialidadId: 0);
        }
        catch (Exception ex)
        {
            Info(ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnEspecialidadChanged(object sender, EventArgs e)
    {
        if (EspecialidadPicker.SelectedItem is not EspecialidadItemDto esp) return;
        await CargarDoctoresAsync(esp.Id);
    }

    private async Task CargarDoctoresAsync(int especialidadId)
    {
        var sesion = await GetSesionPacienteAsync();
        if (sesion == null) return;

        SetLoading(true);
        Info("");

        try
        {
            var url = especialidadId > 0
                ? $"{ApiPaths.Doctores}?especialidadId={especialidadId}"
                : ApiPaths.Doctores;

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);

            var res = await _api.Client.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                Info($"No pude cargar doctores. HTTP {(int)res.StatusCode}\n{body}");
                DoctorPicker.ItemsSource = null;
                return;
            }

            _doctores = (await res.Content.ReadFromJsonAsync<List<DoctorItemDto>>())
                        ?? new List<DoctorItemDto>();

           
            _doctores.Insert(0, new DoctorItemDto { Id = 0, Nombre = "Todos", Apellido = "" });

            DoctorPicker.ItemsSource = _doctores;
            DoctorPicker.ItemDisplayBinding = new Binding(nameof(DoctorItemDto.NombreCompleto));
            DoctorPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            Info(ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnBuscar(object sender, EventArgs e)
    {
        var sesion = await GetSesionPacienteAsync();
        if (sesion == null) return;

        SetLoading(true);
        Info("");

        try
        {
            var fecha = FechaFiltro.Date.ToString("yyyy-MM-dd");

            var doctorId = 0;
            if (DoctorPicker.SelectedItem is DoctorItemDto doc)
                doctorId = doc.Id;

            
            var url = doctorId > 0
                ? $"{ApiPaths.TurnosDisponibles}?fecha={fecha}&idDoctor={doctorId}"
                : $"{ApiPaths.TurnosDisponibles}?fecha={fecha}";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);

            var res = await _api.Client.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                Info($"Error buscando turnos. HTTP {(int)res.StatusCode}\n{body}");
                TurnosList.ItemsSource = null;
                return;
            }

            var turnos = (await res.Content.ReadFromJsonAsync<List<TurnoDisponibleDto>>())
                         ?? new List<TurnoDisponibleDto>();

            TurnosList.ItemsSource = turnos;
        }
        catch (Exception ex)
        {
            Info(ex.Message);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async void OnReservar(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.BindingContext is not TurnoDisponibleDto turno)
            return;

        var ok = await DisplayAlert("Confirmar", $"¿Reservar el turno {turno.FechaHora:dd/MM HH:mm}?", "Sí", "No");
        if (!ok) return;

        var sesion = await GetSesionPacienteAsync();
        if (sesion == null) return;

        SetLoading(true);
        Info("");

        try
        {
            var dto = new { idTurno = turno.Id };

            var req = new HttpRequestMessage(HttpMethod.Post, ApiPaths.ReservarTurno);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Value.token);
            req.Content = JsonContent.Create(dto);

            var res = await _api.Client.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"No se pudo reservar.\nHTTP {(int)res.StatusCode}\n{body}", "OK");
                return;
            }

            await DisplayAlert("OK", "Turno reservado", "Cerrar");
          
            OnBuscar(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }
}
