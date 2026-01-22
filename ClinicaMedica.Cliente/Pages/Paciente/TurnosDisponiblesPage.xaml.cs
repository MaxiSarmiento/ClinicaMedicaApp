using ClinicaMedica.Cliente.Services;
using System.Net.Http.Headers;

namespace ClinicaMedica.Cliente.Pages.Paciente;

[QueryProperty(nameof(IdDoctor), "idDoctor")]
public partial class TurnosDisponiblesPage : ContentPage
{
    private readonly ApiService _api;

    public int IdDoctor { get; set; }

    public TurnosDisponiblesPage(ApiService api)
    {
        InitializeComponent();
        _api = api;
        FechaFiltro.Date = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await BuscarAsync();
    }

    private async void OnBuscar(object sender, EventArgs e)
        => await BuscarAsync();

    private async Task BuscarAsync()
    {
        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var fecha = FechaFiltro.Date.ToString("yyyy-MM-dd");

           
            var url = $"api/Turnos/disponibles?fecha={fecha}&idDoctor={IdDoctor}";
            var turnos = await _api.GetJsonAsync<List<TurnoDisponibleDto>>(url);

            TurnosList.ItemsSource = turnos ?? new List<TurnoDisponibleDto>();
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

    private async void OnReservar(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not int idTurno)
            return;

        try
        {
            Loading.IsVisible = Loading.IsRunning = true;

            var token = await SecureStorage.GetAsync("token");
            var idPacienteStr = await SecureStorage.GetAsync("userId");

            if (string.IsNullOrWhiteSpace(token) || !int.TryParse(idPacienteStr, out var idPaciente))
            {
                await DisplayAlert("Sesión", "Sesión inválida. Volvé a iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//Login");
                return;
            }

            _api.Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

           
            var dto = new { IdTurno = idTurno };
            var resp = await _api.PostAsync("api/Turnos/reservar", dto);

          

            if (resp.IsSuccessStatusCode)
            {
                await DisplayAlert("OK", "Turno reservado ✅", "Cerrar");
                await BuscarAsync();
            }
            else
            {
                var msg = await resp.Content.ReadAsStringAsync();
                await DisplayAlert("Error", msg, "Cerrar");
            }
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

    // DTO que matchea lo que devuelve GET /Turnos/disponibles
    public class TurnoDisponibleDto
    {
        public int Id { get; set; }
        public int IdDoctor { get; set; }
        public int? IdPaciente { get; set; }
        public DateTime FechaHora { get; set; }
        public int DuracionMinutos { get; set; }
        public string? Estado { get; set; }
    }
}
